﻿using Discord;
using Discord.Commands;
using NadekoBot.Extensions;
using NadekoBot.Core.Services;
using NLog;
using System.Globalization;
using System.Threading.Tasks;
using Discord.WebSocket;
using NadekoBot.Core.Services.Impl;

namespace NadekoBot.Modules
{
    public abstract class NadekoTopLevelModule : ModuleBase
    {
        protected Logger _log { get; }
        protected CultureInfo _cultureInfo { get; set; }

        public string ModuleTypeName { get; }
        public string LowerModuleTypeName { get; }

        public NadekoStrings Strings { get; set; }
        public IBotConfigProvider Bc { get; set; }
        public CommandHandler CmdHandler { get; set; }
        public ILocalization Localization { get; set; }

        public string Prefix => CmdHandler.GetPrefix(Context.Guild);

        protected NadekoTopLevelModule(bool isTopLevelModule = true)
        {
            //if it's top level module
            ModuleTypeName = isTopLevelModule ? this.GetType().Name : this.GetType().DeclaringType.Name;
            LowerModuleTypeName = ModuleTypeName.ToLowerInvariant();
            _log = LogManager.GetCurrentClassLogger();
        }

        protected override void BeforeExecute(CommandInfo cmd)
        {
            _cultureInfo = Localization.GetCultureInfo(Context.Guild?.Id);
        }

        //public Task<IUserMessage> ReplyConfirmLocalized(string titleKey, string textKey, string url = null, string footer = null)
        //{
        //    var title = NadekoBot.ResponsesResourceManager.GetString(titleKey, cultureInfo);
        //    var text = NadekoBot.ResponsesResourceManager.GetString(textKey, cultureInfo);
        //    return Context.Channel.SendConfirmAsync(title, text, url, footer);
        //}

        //public Task<IUserMessage> ReplyConfirmLocalized(string textKey)
        //{
        //    var text = NadekoBot.ResponsesResourceManager.GetString(textKey, cultureInfo);
        //    return Context.Channel.SendConfirmAsync(Context.User.Mention + " " + textKey);
        //}

        //public Task<IUserMessage> ReplyErrorLocalized(string titleKey, string textKey, string url = null, string footer = null)
        //{
        //    var title = NadekoBot.ResponsesResourceManager.GetString(titleKey, cultureInfo);
        //    var text = NadekoBot.ResponsesResourceManager.GetString(textKey, cultureInfo);
        //    return Context.Channel.SendErrorAsync(title, text, url, footer);
        //}

        protected string GetText(string key) =>
            Strings.GetText(key, _cultureInfo, LowerModuleTypeName);

        protected string GetText(string key, params object[] replacements) =>
            Strings.GetText(key, _cultureInfo, LowerModuleTypeName, replacements);

        public Task<IUserMessage> ErrorLocalized(string textKey, params object[] replacements)
        {
            var text = GetText(textKey, replacements);
            return Context.Channel.SendErrorAsync(text);
        }

        public Task<IUserMessage> ReplyErrorLocalized(string textKey, params object[] replacements)
        {
            var text = GetText(textKey, replacements);
            return Context.Channel.SendErrorAsync(Format.Bold(Context.User.Mention) + " " + text);
        }

        public Task<IUserMessage> ConfirmLocalized(string textKey, params object[] replacements)
        {
            var text = GetText(textKey, replacements);
            return Context.Channel.SendConfirmAsync(text);
        }

        public Task<IUserMessage> ReplyConfirmLocalized(string textKey, params object[] replacements)
        {
            var text = GetText(textKey, replacements);
            return Context.Channel.SendConfirmAsync(Format.Bold(Context.User.Mention) + " " + text);
        }

        public async Task<bool> PromptUserConfirmAsync(EmbedBuilder embed)
        {
            embed.WithOkColor();

            var msg = await Context.Channel.EmbedAsync(embed).ConfigureAwait(false);
            try
            {
                var input = await GetUserInputAsync(Context.User.Id, Context.Channel.Id).ConfigureAwait(false);
                input = input?.ToUpperInvariant();

                if (input != "YES" && input != "Y" && input != "ДА")
                {
                    return false;
                }

                return true;
            }
            finally
            {
                var _ = Task.Run(() => msg.DeleteAsync());
            }
        }
        
        // TypeConverter typeConverter = TypeDescriptor.GetConverter(propType); ?
        public async Task<string> GetUserInputAsync(ulong userId, ulong channelId)
        {
            var userInputTask = new TaskCompletionSource<string>();
            var dsc = (DiscordSocketClient)Context.Client;
            try
            {
                dsc.MessageReceived += MessageReceived;

                if ((await Task.WhenAny(userInputTask.Task, Task.Delay(50000)).ConfigureAwait(false)) != userInputTask.Task)
                {
                    return null;
                }

                return await userInputTask.Task.ConfigureAwait(false);
            }
            finally
            {
                dsc.MessageReceived -= MessageReceived;
            }

            Task MessageReceived(SocketMessage arg)
            {
                var _ = Task.Run(() =>
                {
                    if (!(arg is SocketUserMessage userMsg) ||
                        !(userMsg.Channel is ITextChannel chan) ||
                        userMsg.Author.Id != userId ||
                        userMsg.Channel.Id != channelId)
                    {
                        return Task.CompletedTask;
                    }

                    if (userInputTask.TrySetResult(arg.Content))
                    {
                        userMsg.DeleteAfter(1);
                    }
                    return Task.CompletedTask;
                });
                return Task.CompletedTask;
            }
        }
    }
    
    public abstract class NadekoTopLevelModule<TService> : NadekoTopLevelModule where TService : INService
    {
        public TService _service { get; set; }

        protected NadekoTopLevelModule(bool isTopLevel = true) : base(isTopLevel)
        {
        }
    }

    public abstract class NadekoSubmodule : NadekoTopLevelModule
    {
        protected NadekoSubmodule() : base(false) { }
    }

    public abstract class NadekoSubmodule<TService> : NadekoTopLevelModule<TService> where TService : INService
    {
        protected NadekoSubmodule() : base(false)
        {
        }
    }
}
