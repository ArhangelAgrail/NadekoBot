﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection.Metadata.Ecma335;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using NadekoBot.Common;
using NadekoBot.Common.Attributes;
using NadekoBot.Common.Collections;
using NadekoBot.Core.Services;
using NadekoBot.Core.Services.Database.Models;
using NadekoBot.Extensions;
using static NadekoBot.Modules.Xp.Xp;

namespace NadekoBot.Modules.Gambling
{
    public partial class Gambling
    {
        [Group]
        public class FlowerShopCommands : NadekoSubmodule
        {
            private readonly DbService _db;
            private readonly ICurrencyService _cs;
            private readonly DiscordSocketClient _client;
            private readonly IHttpClientFactory _httpFactory;

            public enum Role
            {
                Role
            }

            public enum List
            {
                List
            }

            public FlowerShopCommands(DbService db, ICurrencyService cs, DiscordSocketClient client)
            {
                _db = db;
                _cs = cs;
                _client = client;
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task Shop(int page = 1)
            {
                if (--page < 0)
                    return;
                List<ShopEntry> entries;
                using (var uow = _db.UnitOfWork)
                {
                    entries = new IndexedCollection<ShopEntry>(uow.GuildConfigs.ForId(Context.Guild.Id,
                        set => set.Include(x => x.ShopEntries)
                                  .ThenInclude(x => x.Items)).ShopEntries);
                }

                await Context.SendPaginatedConfirmAsync(page, (curPage) =>
                {
                    var theseEntries = entries.Skip(curPage * 12).Take(12).ToArray();

                    if (!theseEntries.Any())
                        return new EmbedBuilder().WithErrorColor()
                            .WithDescription(GetText("shop_none"));
                    var embed = new EmbedBuilder().WithOkColor()
                        .WithTitle(GetText("shop"))
                        .WithDescription(GetText("shop_desc"));

                    for (int i = 0; i < theseEntries.Length; i++)
                    {
                        var entry = theseEntries[i];
                        embed.AddField(efb => efb.WithName(GetText("shop_item_title", curPage * 12 + i + 1, entry.Name, entry.ItemName)).WithValue(GetText("shop_item_desc", EntryToString(entry), entry.Price, Bc.BotConfig.CurrencySign)).WithIsInline(true));
                    }
                    return embed;
                }, entries.Count, 12, true).ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task ShopDesc(int page = 1)
            {
                if (--page < 0)
                    return;
                List<ShopEntry> entries;
                using (var uow = _db.UnitOfWork)
                {
                    entries = new IndexedCollection<ShopEntry>(uow.GuildConfigs.ForId(Context.Guild.Id,
                        set => set.Include(x => x.ShopEntries)
                                  .ThenInclude(x => x.Items)).ShopEntries);
                }

                await Context.SendPaginatedConfirmAsync(page, (curPage) =>
                {
                    var theseEntries = entries.Skip(curPage).Take(1).ToArray();
                    var entry = theseEntries[0];

                    if (!theseEntries.Any())
                        return new EmbedBuilder().WithErrorColor()
                            .WithDescription(GetText("shop_none"));

                    var itemDescription = entry.Description.Split(';');

                    var embed = new EmbedBuilder()
                            .WithTitle(GetText("shop_purchase_title", entry.Name))
                            .WithAuthor(GetText("shop_purchase_author", curPage+1, entry.ItemName))
                            .WithDescription(GetText("shop_purchase_desc", string.Join("\n", itemDescription.Select(x => $"- {x}"))))
                            .AddField(GetText("shop_purchase_field_title"), GetText("shop_purchase_field_desc", entry.Price, Bc.BotConfig.CurrencySign, curPage+1, EntryToString(entry)), true);

                    if (Uri.IsWellFormedUriString(entry.ItemLogoUrl, UriKind.Absolute))
                        embed.WithThumbnailUrl(entry.ItemLogoUrl);
                    if (Uri.IsWellFormedUriString(entry.ItemImageUrl, UriKind.Absolute))
                        embed.WithImageUrl(entry.ItemImageUrl);

                    return embed;
                }, entries.Count, 1, true).ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task Buy(int index)
            {
                index -= 1;
                if (index < 0)
                    return;
                ShopEntry entry;
                using (var uow = _db.UnitOfWork)
                {
                    var config = uow.GuildConfigs.ForId(Context.Guild.Id, set => set
                        .Include(x => x.ShopEntries)
                        .ThenInclude(x => x.Items));
                    var entries = new IndexedCollection<ShopEntry>(config.ShopEntries);
                    entry = entries.ElementAtOrDefault(index);
                    uow.Complete();
                }

                if (entry == null)
                {
                    await ReplyErrorLocalized("shop_item_not_found").ConfigureAwait(false);
                    return;
                }

                var itemDescription = entry.Description.Split(';');

                var embed = new EmbedBuilder()
                        .WithTitle(GetText("shop_purchase_title", entry.Name))
                        .WithAuthor(GetText("shop_purchase_author", index+1, entry.ItemName))
                        .WithDescription(GetText("shop_purchase_desc", string.Join("\n", itemDescription.Select(x => $"- {x}"))))
                        .AddField(GetText("shop_purchase_confirm_title", entry.ItemName), GetText("shop_purchase_confirm_desc"), true);
                 
                if (Uri.IsWellFormedUriString(entry.ItemLogoUrl, UriKind.Absolute))
                    embed.WithThumbnailUrl(entry.ItemLogoUrl);
                if (Uri.IsWellFormedUriString(entry.ItemImageUrl, UriKind.Absolute))
                    embed.WithImageUrl(entry.ItemImageUrl);

                if (!await PromptUserConfirmAsync(embed).ConfigureAwait(false))
                {
                    return;
                }

                if (entry.Type == ShopEntryType.Role)
                {
                    var guser = (IGuildUser)Context.User;
                    var role = Context.Guild.GetRole(entry.RoleId);

                    if (role == null)
                    {
                        await ReplyErrorLocalized("shop_role_not_found").ConfigureAwait(false);
                        return;
                    }

                    if (await _cs.RemoveAsync(Context.User.Id, $"Shop purchase - {entry.Type}", entry.Price).ConfigureAwait(false))
                    {
                        try
                        {
                            await guser.AddRoleAsync(role).ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            _log.Warn(ex);
                            await _cs.AddAsync(Context.User.Id, $"Shop error refund", entry.Price).ConfigureAwait(false);
                            await ReplyErrorLocalized("shop_role_purchase_error").ConfigureAwait(false);
                            return;
                        }
                        var profit = GetProfitAmount(entry.Price);
                        await _cs.AddAsync(entry.AuthorId, $"Shop sell item - {entry.Type}", profit).ConfigureAwait(false);
                        await _cs.AddAsync(Context.Client.CurrentUser.Id, $"Shop sell item - cut", entry.Price - profit).ConfigureAwait(false);
                        await ReplyConfirmLocalized("shop_role_purchase", Format.Bold(role.Name)).ConfigureAwait(false);
                        return;
                    }
                    else
                    {
                        await ReplyErrorLocalized("not_enough", Bc.BotConfig.CurrencySign).ConfigureAwait(false);
                        return;
                    }
                }
                else if (entry.Type == ShopEntryType.List)
                {
                    if (entry.Items.Count == 0)
                    {
                        await ReplyErrorLocalized("shop_out_of_stock").ConfigureAwait(false);
                        return;
                    }

                    var item = entry.Items.ToArray()[new NadekoRandom().Next(0, entry.Items.Count)];

                    if (await _cs.RemoveAsync(Context.User.Id, $"Shop purchase - {entry.Type} : {entry.Name} : {entry.ItemName}", entry.Price).ConfigureAwait(false))
                    {
                        using (var uow = _db.UnitOfWork)
                        {
                            var x = uow._context.Set<ShopEntryItem>().Remove(item);
                            uow.Complete();
                        }
                        try
                        {
                            var itemDesc = entry.Description.Split(';');
                            await (await Context.User.GetOrCreateDMChannelAsync().ConfigureAwait(false))
                                .EmbedAsync(new EmbedBuilder().WithOkColor()
                                .WithTitle(GetText("shop_purchase", Context.Guild.Name))
                                .AddField(efb => efb.WithName(GetText("shop_item", entry.ItemName)).WithValue(item.Text).WithIsInline(false))
                                .AddField(efb => efb.WithName(entry.Name).WithValue(string.Join("\n", itemDescription.Select(x => $"- {x}"))).WithIsInline(true)))
                                .ConfigureAwait(false);

                            await (await Context.User.GetOrCreateDMChannelAsync().ConfigureAwait(false)).SendMessageAsync(item.Text);

                            await _cs.AddAsync(entry.AuthorId,
                                    $"Shop sell item - {entry.Name} : {entry.ItemName}",
                                    GetProfitAmount(entry.Price)).ConfigureAwait(false);
                        }
                        catch
                        {
                            await _cs.AddAsync(Context.User.Id,
                                $"Shop error refund - {entry.Name}",
                                entry.Price).ConfigureAwait(false);
                            using (var uow = _db.UnitOfWork)
                            {
                                var entries = new IndexedCollection<ShopEntry>(uow.GuildConfigs.ForId(Context.Guild.Id,
                                    set => set.Include(x => x.ShopEntries)
                                              .ThenInclude(x => x.Items)).ShopEntries);
                                entry = entries.ElementAtOrDefault(index);
                                if (entry != null)
                                {
                                    if (entry.Items.Add(item))
                                    {
                                        uow.Complete();
                                    }
                                }
                            }
                            await ReplyErrorLocalized("shop_buy_error").ConfigureAwait(false);
                            return;
                        }
                        await ReplyConfirmLocalized("shop_item_purchase", entry.Name, entry.ItemName).ConfigureAwait(false);
                    }
                    else
                    {
                        await ReplyErrorLocalized("not_enough", Bc.BotConfig.CurrencySign).ConfigureAwait(false);
                        return;
                    }
                }

            }

            private static long GetProfitAmount(int price) =>
                (int)(Math.Ceiling(0.01 * price));

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.Administrator)]
            [RequireBotPermission(GuildPermission.ManageRoles)]
            public async Task ShopAdd(Role _, int price, [Remainder] IRole role)
            {
                var entry = new ShopEntry()
                {
                    Name = "-",
                    Price = price,
                    Type = ShopEntryType.Role,
                    AuthorId = Context.User.Id,
                    RoleId = role.Id,
                    RoleName = role.Name
                };
                using (var uow = _db.UnitOfWork)
                {
                    var entries = new IndexedCollection<ShopEntry>(uow.GuildConfigs.ForId(Context.Guild.Id,
                        set => set.Include(x => x.ShopEntries)
                                  .ThenInclude(x => x.Items)).ShopEntries)
                    {
                        entry
                    };
                    uow.GuildConfigs.ForId(Context.Guild.Id, set => set).ShopEntries = entries;
                    uow.Complete();
                }
                await Context.Channel.EmbedAsync(EntryToEmbed(entry)
                    .WithTitle(GetText("shop_item_add"))).ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.Administrator)]
            public async Task ShopAdd(List _, int price, [Remainder]string name)
            {
                var entry = new ShopEntry()
                {
                    Name = name.TrimTo(100),
                    Price = price,
                    Type = ShopEntryType.List,
                    AuthorId = Context.User.Id,
                    Items = new HashSet<ShopEntryItem>(),
                };
                using (var uow = _db.UnitOfWork)
                {
                    var entries = new IndexedCollection<ShopEntry>(uow.GuildConfigs.ForId(Context.Guild.Id,
                        set => set.Include(x => x.ShopEntries)
                                  .ThenInclude(x => x.Items)).ShopEntries)
                    {
                        entry
                    };
                    uow.GuildConfigs.ForId(Context.Guild.Id, set => set).ShopEntries = entries;
                    uow.Complete();
                }
                await Context.Channel.EmbedAsync(EntryToEmbed(entry)
                    .WithTitle(GetText("shop_item_add"))).ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.Administrator)]
            public async Task ShopListAdd(int index, [Remainder] string itemText)
            {
                index -= 1;
                if (index < 0)
                    return;
                var item = new ShopEntryItem()
                {
                    Text = itemText
                };
                ShopEntry entry;
                bool rightType = false;
                bool added = false;
                using (var uow = _db.UnitOfWork)
                {
                    var entries = new IndexedCollection<ShopEntry>(uow.GuildConfigs.ForId(Context.Guild.Id,
                        set => set.Include(x => x.ShopEntries)
                                  .ThenInclude(x => x.Items)).ShopEntries);
                    entry = entries.ElementAtOrDefault(index);
                    if (entry != null && (rightType = (entry.Type == ShopEntryType.List)))
                    {
                        if (added = entry.Items.Add(item))
                        {
                            uow.Complete();
                        }
                    }
                }
                if (entry == null)
                    await ReplyErrorLocalized("shop_item_not_found").ConfigureAwait(false);
                else if (!rightType)
                    await ReplyErrorLocalized("shop_item_wrong_type").ConfigureAwait(false);
                else if (added == false)
                    await ReplyErrorLocalized("shop_list_item_not_unique").ConfigureAwait(false);
                else
                    await ReplyConfirmLocalized("shop_list_item_added").ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.Administrator)]
            public async Task ShopItemDesc(int index, [Remainder] string desc)
            {
                index -= 1;
                if (index < 0)
                    return;

                using (var uow = _db.UnitOfWork)
                {
                    var entry = uow.GuildConfigs.ForId(Context.Guild.Id, set => set
                        .Include(x => x.ShopEntries));
                    if (entry == null)
                        return;

                    entry.ShopEntries[index].Description = desc;
                    uow.Complete();
                }

                await ReplyConfirmLocalized("shop_info_updated", desc).ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.Administrator)]
            public async Task ShopItemName(int index, [Remainder] string name)
            {
                index -= 1;
                if (index < 0)
                    return;

                using (var uow = _db.UnitOfWork)
                {
                    var entry = uow.GuildConfigs.ForId(Context.Guild.Id, set => set
                        .Include(x => x.ShopEntries));
                    if (entry == null)
                        return;

                    entry.ShopEntries[index].ItemName = name;
                    uow.Complete();
                }

                await ReplyConfirmLocalized("shop_info_updated", name).ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.Administrator)]
            public async Task ShopItemLogo(int index, [Remainder] string url = null)
            {
                index -= 1;
                if (index < 0)
                    return;

                if (!Uri.IsWellFormedUriString(url, UriKind.Absolute) && url != null)
                { return; }

                using (var uow = _db.UnitOfWork)
                {
                    var entry = uow.GuildConfigs.ForId(Context.Guild.Id, set => set
                        .Include(x => x.ShopEntries));
                    if (entry == null)
                        return;

                    entry.ShopEntries[index].ItemLogoUrl = url;
                    uow.Complete();
                }

                await ReplyConfirmLocalized("shop_info_updated", url).ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.Administrator)]
            public async Task ShopItemImage(int index, [Remainder] string url = null)
            {
                index -= 1;
                if (index < 0)
                    return;

                if (!Uri.IsWellFormedUriString(url, UriKind.Absolute) && url != null)
                { return; }

                using (var uow = _db.UnitOfWork)
                {
                    var entry = uow.GuildConfigs.ForId(Context.Guild.Id, set => set
                        .Include(x => x.ShopEntries));
                    if (entry == null)
                        return;

                    entry.ShopEntries[index].ItemImageUrl = url;
                    uow.Complete();
                }

                await ReplyConfirmLocalized("shop_info_updated", url).ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.Administrator)]
            public async Task ShopItemPrice(int index, int price)
            {
                index -= 1;
                if (index < 0)
                    return;

                using (var uow = _db.UnitOfWork)
                {
                    var entry = uow.GuildConfigs.ForId(Context.Guild.Id, set => set
                        .Include(x => x.ShopEntries));
                    if (entry == null)
                        return;

                    entry.ShopEntries[index].Price = price;
                    uow.Complete();
                }

                await ReplyConfirmLocalized("shop_info_updated", price).ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.Administrator)]
            public async Task ShopRemove(int index)
            {
                index -= 1;
                if (index < 0)
                    return;
                ShopEntry removed;
                using (var uow = _db.UnitOfWork)
                {
                    var config = uow.GuildConfigs.ForId(Context.Guild.Id, set => set
                        .Include(x => x.ShopEntries)
                        .ThenInclude(x => x.Items));

                    var entries = new IndexedCollection<ShopEntry>(config.ShopEntries);
                    removed = entries.ElementAtOrDefault(index);
                    if (removed != null)
                    {
                        uow._context.RemoveRange(removed.Items);
                        uow._context.Remove(removed);
                        uow.Complete();
                    }
                }

                if (removed == null)
                    await ReplyErrorLocalized("shop_item_not_found").ConfigureAwait(false);
                else
                    await Context.Channel.EmbedAsync(EntryToEmbed(removed)
                        .WithTitle(GetText("shop_item_rm"))).ConfigureAwait(false);
            }

            public EmbedBuilder EntryToEmbed(ShopEntry entry)
            {
                var embed = new EmbedBuilder().WithOkColor();

                if (entry.Type == ShopEntryType.Role)
                    return embed.AddField(efb => efb.WithName(GetText("name")).WithValue(GetText("shop_role", Format.Bold(Context.Guild.GetRole(entry.RoleId)?.Name ?? "MISSING_ROLE"))).WithIsInline(true))
                            .AddField(efb => efb.WithName(GetText("price")).WithValue(entry.Price.ToString()).WithIsInline(true))
                            .AddField(efb => efb.WithName(GetText("type")).WithValue(entry.Type.ToString()).WithIsInline(true));
                else if (entry.Type == ShopEntryType.List)
                    return embed.AddField(efb => efb.WithName(GetText("name")).WithValue(entry.Name).WithIsInline(true))
                            .AddField(efb => efb.WithName(GetText("price")).WithValue(entry.Price.ToString()).WithIsInline(true))
                            .AddField(efb => efb.WithName(GetText("type")).WithValue(GetText("random_unique_item")).WithIsInline(true));
                //else if (entry.Type == ShopEntryType.Infinite_List)
                //    return embed.AddField(efb => efb.WithName(GetText("name")).WithValue(GetText("shop_role", Format.Bold(entry.RoleName))).WithIsInline(true))
                //            .AddField(efb => efb.WithName(GetText("price")).WithValue(entry.Price.ToString()).WithIsInline(true))
                //            .AddField(efb => efb.WithName(GetText("type")).WithValue(entry.Type.ToString()).WithIsInline(true));
                else return null;
            }

            public string EntryToString(ShopEntry entry)
            {
                if (entry.Type == ShopEntryType.Role)
                {
                    return GetText("shop_role", Format.Bold(Context.Guild.GetRole(entry.RoleId)?.Name ?? "MISSING_ROLE"));
                }
                else if (entry.Type == ShopEntryType.List)
                {
                    return GetText("unique_items_left", entry.Items.Count);
                }
                //else if (entry.Type == ShopEntryType.Infinite_List)
                //{

                //}
                return "";
            }
        }
    }
}
