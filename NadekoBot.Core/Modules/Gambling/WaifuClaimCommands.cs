﻿using Discord;
using Discord.Commands;
using NadekoBot.Extensions;
using NadekoBot.Core.Services.Database.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using NadekoBot.Common.Attributes;
using NadekoBot.Modules.Gambling.Services;
using NadekoBot.Core.Modules.Gambling.Common.Waifu;
using System.Diagnostics;

namespace NadekoBot.Modules.Gambling
{
    public partial class Gambling
    {
        [Group]
        public class WaifuClaimCommands : NadekoSubmodule<WaifuService>
        {
            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [OwnerOnly]
            public async Task Immune([Remainder]IGuildUser u = null)
            {
                var success = await _service.SetImmune(u);
                if (success)
                {
                    await ReplyConfirmLocalized("waifu_success");
                    return;
                }
                else
                    await ReplyConfirmLocalized("waifu_not_success");
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task About([Remainder] string info = null)
            {
                if (info.Length > 256)
                    await ReplyErrorLocalized("info_not_success");
                else
                {
                    var success = await _service.SetInfo(Context.User, info);
                    if (success)
                    {
                        await ReplyConfirmLocalized("info_success");
                        return;
                    }
                    else
                        await ReplyErrorLocalized("info_not_success");
                }
            }

            [NadekoCommand, Usage, Description, Aliases]
            public async Task WaifuReset()
            {
                var price = _service.GetResetPrice(Context.User);
                var embed = new EmbedBuilder()
                        .WithTitle(GetText("waifu_reset_confirm"))
                        .WithDescription(GetText("cost", Format.Bold(price + Bc.BotConfig.CurrencySign)));

                if (!await PromptUserConfirmAsync(embed))
                    return;

                if (await _service.TryReset(Context.User))
                {
                    await ReplyConfirmLocalized("waifu_reset");
                    return;
                }
                await ReplyErrorLocalized("waifu_reset_fail");
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task WaifuClaim(int amount, [Remainder]IUser target)
            {
                var wi = _service.GetCountInfoAsync(Context.User);

                if (amount < Bc.BotConfig.MinWaifuPrice)
                {
                    await ReplyErrorLocalized("waifu_isnt_cheap", Bc.BotConfig.MinWaifuPrice + Bc.BotConfig.CurrencySign);
                    return;
                }

                if (target.Id == Context.User.Id)
                {
                    await ReplyErrorLocalized("waifu_not_yourself");
                    return;
                }

                var (w, isAffinity, result) = await _service.ClaimWaifuAsync(Context.User, target, amount, wi.ClaimCount);

                if (result == WaifuClaimResult.InsufficientAmount)
                {
                    await ReplyErrorLocalized("waifu_not_enough", Math.Ceiling(w.Price * (isAffinity ? 0.88f : 1.1f)));
                    return;
                }
                if (result == WaifuClaimResult.NotEnoughFunds)
                {
                    await ReplyErrorLocalized("not_enough", Bc.BotConfig.CurrencySign);
                    return;
                }
                if (result == WaifuClaimResult.Immune)
                {
                    await ReplyErrorLocalized("waifu_immune");
                    return;
                }
                if (result == WaifuClaimResult.MaxCount)
                {
                    await ReplyErrorLocalized("waifu_max_count");
                    return;
                }
                var msg = GetText("waifu_claimed",
                    Format.Bold(target.ToString()),
                    amount + Bc.BotConfig.CurrencySign);
                if (w.Affinity?.UserId == Context.User.Id)
                    msg += "\n" + GetText("waifu_fulfilled", target, w.Price + Bc.BotConfig.CurrencySign);
                else
                    msg = " " + msg;
                await Context.Channel.SendConfirmAsync(Context.User.Mention + msg);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task WaifuTransfer(IUser waifu, IUser newOwner)
            {
                if (!await _service.WaifuTransfer(Context.User, waifu.Id, newOwner)
                    )
                {
                    await ReplyErrorLocalized("waifu_transfer_fail");
                    return;
                }

                await ReplyConfirmLocalized("waifu_transfer_success",
                    Format.Bold(waifu.ToString()),
                    Format.Bold(Context.User.ToString()),
                    Format.Bold(newOwner.ToString()));
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [Priority(0)]
            public Task Divorce([Remainder]IGuildUser target) => Divorce(target.Id);

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [Priority(1)]
            public async Task Divorce([Remainder]ulong targetId)
            {
                if (targetId == Context.User.Id)
                    return;

                var (w, result, amount, remaining) = await _service.DivorceWaifuAsync(Context.User, targetId);

                if (result == DivorceResult.SucessWithPenalty)
                {
                    await ReplyConfirmLocalized("waifu_divorced_like", Format.Bold(w.Waifu.ToString()), amount + Bc.BotConfig.CurrencySign);
                }
                else if (result == DivorceResult.Success)
                {
                    await ReplyConfirmLocalized("waifu_divorced_notlike", amount + Bc.BotConfig.CurrencySign);
                }
                else if (result == DivorceResult.NotYourWife)
                {
                    await ReplyErrorLocalized("waifu_not_yours");
                }
                else
                {
                    await ReplyErrorLocalized("waifu_recent_divorce",
                        Format.Bold(((int)remaining?.TotalHours).ToString()),
                        Format.Bold(remaining?.Minutes.ToString()));
                }
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task WaifuClaimerAffinity([Remainder]IGuildUser u = null)
            {
                if (u?.Id == Context.User.Id)
                {
                    await ReplyErrorLocalized("waifu_egomaniac");
                    return;
                }
                var (oldAff, sucess, remaining) = await _service.ChangeAffinityAsync(Context.User, u);
                if (!sucess)
                {
                    if (remaining != null)
                    {
                        await ReplyErrorLocalized("waifu_affinity_cooldown",
                            Format.Bold(((int)remaining?.TotalHours).ToString()),
                            Format.Bold(remaining?.Minutes.ToString()));
                    }
                    else
                    {
                        await ReplyErrorLocalized("waifu_affinity_already");
                    }
                    return;
                }
                if (u == null)
                {
                    await ReplyConfirmLocalized("waifu_affinity_reset");
                }
                else if (oldAff == null)
                {
                    await ReplyConfirmLocalized("waifu_affinity_set", Format.Bold(u.ToString()));
                }
                else
                {
                    await ReplyConfirmLocalized("waifu_affinity_changed", Format.Bold(oldAff.ToString()), Format.Bold(u.ToString()));
                }
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public Task WaifuLeaderboard(int page = 1)
            {
                if (--page < 0 || page > 100)
                    return Task.CompletedTask;

                return Context.SendPaginatedConfirmAsync(page, (curPage) =>
                {
                    var users = _service.GetTopWaifusAtPage(curPage);

                    var embed = new EmbedBuilder()
                        .WithTitle(GetText("waifus_top_waifus"))
                        .WithFooter(GetText("page", curPage + 1))
                        .WithOkColor();

                    if (!users.Any())
                        return embed.WithDescription("-");
                    else
                    {
                        var i = curPage * 9;
                        foreach (var w in users)
                        {
                            embed.AddField(efb => efb.WithName($"#{++i} - " + w.ToString()).WithValue(Format.Bold(w.Price + Bc.BotConfig.CurrencySign)).WithIsInline(false));
                        }
                        return embed;
                    }
                }, 1000, 10, addPaginatedFooter: false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public Task RepLb(int page = 1)
            {
                if (--page < 0 || page > 100)
                    return Task.CompletedTask;

                return Context.SendPaginatedConfirmAsync(page, (curPage) =>
                {
                    var users = _service.GetTopRepAtPage(curPage);

                    var embed = new EmbedBuilder()
                        .WithTitle(GetText("rep_top"))
                        .WithFooter(GetText("page", curPage + 1))
                        .WithOkColor();

                    if (!users.Any())
                        return embed.WithDescription("-");
                    else
                    {
                        var i = curPage * 10;
                        foreach (var w in users)
                        {
                            embed.AddField(efb => efb.WithName($"#{++i} " + w.Username + "#" + w.Discrim + " - \"" + GetText(_service.GetRepTitle(w.Reputation)) + "\"").WithValue(GetText("reputation") + " ☆ +" + w.Reputation.ToString()).WithIsInline(false));
                        }
                        return embed;
                    }
                }, 1000, 10, addPaginatedFooter: false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public Task RepLog(IUser user, int page = 1)
            {
                if (--page < 0 || page > 100)
                    return Task.CompletedTask;

                return Context.SendPaginatedConfirmAsync(page, (curPage) =>
                {
                    var users = _service.GetRepLogByUser(user.Id, curPage);

                    var embed = new EmbedBuilder()
                        .WithTitle(GetText("rep_top"))
                        .WithFooter(GetText("page", curPage + 1))
                        .WithOkColor();

                    if (!users.Any())
                        return embed.WithDescription("-");
                    else
                    {
                        var i = curPage * 9;
                        foreach (var w in users)
                        {
                            var j = i++;
                            embed.AddField(efb => efb.WithName($"#{++i} <@{w.UserId}>").WithValue(GetText("reputation") + " ☆ +").WithIsInline(false));
                        }
                        return embed;
                    }
                }, 1000, 10, addPaginatedFooter: false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task WaifuInfo([Remainder]IGuildUser target = null)
            {
                if (target == null)
                    target = (IGuildUser)Context.User;
                var wi = _service.GetFullWaifuInfoAsync(target);
                var affInfo = _service.GetAffinityTitle(wi.AffinityCount);
                var club = _service.GetClubName(target);

                var time = DateTime.UtcNow - target.JoinedAt;

                string clubName = "-";
                if (club != null)
                    clubName = club.Name;

                string info = wi.Info;
                if (info == null)
                    info = GetText("about_me");

                var nobody = GetText("nobody");
                var i = 0;

                var items = wi.Items.OrderBy(x => x.Price).GroupBy(x => x.ItemEmoji);
                var count = !wi.Items.Any() ? 0 : items.Count();

                var itemsStr1 = !wi.Items.Any()
                    ? "-"
                    : string.Join("\n", wi.Items
                        .OrderBy(x => x.Price)
                        .GroupBy(x => x.ItemEmoji)
                        .Take(14)
                        .Select(x => $"{x.Key}x{x.Count(),-3}")
                        .GroupBy(x => i++ / 2)
                        .Select(x => string.Join(" ", x)));

                var itemsStr2 = count < 14 ? "-"
                    : string.Join("\n", wi.Items
                        .OrderBy(x => x.Price)
                        .GroupBy(x => x.ItemEmoji)
                        .TakeLast(count - 14)
                        .Select(x => $"{x.Key}x{x.Count(),-3}")
                        .GroupBy(x => i++ / 2)
                        .Select(x => string.Join(" ", x)));

                var itemsStr = itemsStr1 + itemsStr2;

                /*var itemsStr = !wi.Items.Any()
                    ? "-"
                    : string.Join("\n", wi.Items
                        .OrderBy(x => x.Price)
                        .GroupBy(x => x.ItemEmoji)
                        .Select(x => $"{x.Key}x{x.Count(),-3}")
                        .GroupBy(x => i++ / 2)
                        .Select(x => string.Join(" ", x)));*/

                var hero = "Герой";
                if (itemsStr.Contains("🐲"))
                    hero = "🐲 Повелитель драконов";
                else if (itemsStr.Contains("🏰"))
                    hero = "🏰 Повелитель";
                else if (itemsStr.Contains("👑"))
                    hero = "👑 Наследник";
                else if (itemsStr.Contains("💎"))
                    hero = "💎 Аристократ";
                else if (itemsStr.Contains("🦄"))
                    hero = "🦄 Наездник";
                else if (itemsStr.Contains("👾"))
                    hero = "👾 Пришелец";
                else if (itemsStr.Contains("⚔️"))
                    hero = "⚔️ Мечник";
                else if (itemsStr.Contains("🛡️"))
                    hero = "🛡️ Рыцарь";
                else if (itemsStr.Contains("🗡️"))
                    hero = "🗡️ Воин";

                var embed = new EmbedBuilder()
                    .WithColor(16738816)
                    .WithAuthor(name: hero + " " + wi.FullName + " - \"" + GetText(_service.GetRepTitle(wi.Reputation)) + "\"", iconUrl: target.GetAvatarUrl())
                    .AddField(efb => efb.WithName(GetText("price")).WithValue(wi.Price.ToString() + Bc.BotConfig.CurrencySign).WithIsInline(true))
                    //.AddField(efb => efb.WithName(GetText("claimed_by")).WithValue(wi.ClaimerName ?? nobody).WithIsInline(true))
                    //.AddField(efb => efb.WithName(GetText("likes")).WithValue(wi.AffinityName ?? nobody).WithIsInline(true))
                    //.AddField(efb => efb.WithName(GetText("changes_of_heart")).WithValue($"{wi.AffinityCount} - \"{GetText(affInfo)}\"").WithIsInline(true))
                    //.AddField(efb => efb.WithName(GetText("club")).WithValue(clubName).WithIsInline(true))
                    .AddField(efb => efb.WithName(GetText("reputation")).WithValue("**+" + wi.Reputation.ToString() + " ☆**").WithIsInline(true))
                    .AddField(efb => efb.WithName(GetText("on_server")).WithValue(time.Value.Days + GetText("days")).WithIsInline(true))
                    .AddField(efb => efb.WithName(GetText("inventory", count > 14 ? 14 : count, 14)).WithValue(itemsStr1).WithIsInline(true))
                    .AddField(efb => efb.WithName(GetText("backpack", count > 14 ? count - 14 : 0, 13)).WithValue(itemsStr2).WithIsInline(true))
                    .AddField(efb => efb.WithName(GetText("roles", target.RoleIds.Count - 1)).WithValue($"{string.Join("\n", target.GetRoles().Take(10).Where(r => r.Id != r.Guild.EveryoneRole.Id).Select(r => { var id = r.Id; return $"<@&{id}>"; })).SanitizeMentions()}").WithIsInline(true))
                    //.AddField(efb => efb.WithName(GetText("Waifus", wi.ClaimCount)).WithValue(wi.ClaimCount == 0 ? nobody + "\n_______" : string.Join("\n", wi.Claims30) + "\n_______").WithIsInline(false))
                    .WithTitle(info);
                    //.WithFooter(text: GetText("info") + " " + info, iconUrl: "http://www.picshare.ru/uploads/181213/sK6tzE73ub.png");

                await Context.Channel.EmbedAsync(embed).ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [Priority(1)]
            public async Task WaifuGift(int page = 1)
            {
                if (--page < 0 || page > 3)
                    return;

                await Context.SendPaginatedConfirmAsync(page, (cur) =>
                {
                    var embed = new EmbedBuilder()
                        .WithTitle(GetText("waifu_gift_shop"))
                        .WithOkColor();

                    Enum.GetValues(typeof(WaifuItem.ItemName))
                                        .Cast<WaifuItem.ItemName>()
                                        .Select(x => WaifuItem.GetItemObject(x, Bc.BotConfig.WaifuGiftMultiplier))
                                        .OrderBy(x => x.Price)
                                        .Skip(9 * cur)
                                        .Take(9)
                                        .ForEach(x => embed.AddField(f => f.WithName(x.ItemEmoji + " " + x.Item.ToString()).WithValue(x.Price).WithIsInline(true)));

                    return embed;
                }, Enum.GetValues(typeof(WaifuItem.ItemName)).Length, 9);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [Priority(0)]
            public async Task WaifuGift(WaifuItem.ItemName item, [Remainder] IUser waifu)
            {
                if (waifu.Id == Context.User.Id)
                    return;
                var itemObj = WaifuItem.GetItemObject(item, Bc.BotConfig.WaifuGiftMultiplier);
                int sucess = await _service.GiftWaifuAsync(Context.User.Id, waifu, itemObj);

                if (sucess != 0)
                {
                    await ReplyConfirmLocalized("waifu_gift", Format.Bold(GetText(item.ToString()) + " " + itemObj.ItemEmoji), waifu.Mention, Format.Bold(String.Format("{0:#,0}", sucess) + Bc.BotConfig.CurrencySign));
                }
                else
                {
                    await ReplyErrorLocalized("not_enough", Bc.BotConfig.CurrencySign);
                }
            }

            /*[NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [Priority(2)]
            public async Task WaifuGift(int count, WaifuItem.ItemName item, [Remainder] IUser waifu)
            {
                if (waifu.Id == Context.User.Id)
                    return;

                var itemObj = WaifuItem.GetItemObject(item, Bc.BotConfig.WaifuGiftMultiplier);
                bool sucess = await _service.GiftWaifuAsync(Context.User.Id, count, waifu, itemObj);

                if (sucess)
                {
                    await ReplyConfirmLocalized("waifu_gift_count", Format.Bold(count.ToString()), Format.Bold(item.ToString() + " " + itemObj.ItemEmoji), Format.Bold(waifu.ToString()));
                }
                else
                {
                    await ReplyErrorLocalized("not_enough", Bc.BotConfig.CurrencySign);
                }
            }*/
        }
    }
}