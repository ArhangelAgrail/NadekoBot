﻿using NadekoBot.Core.Services.Database.Models;
using System;
using System.Threading.Tasks;

namespace NadekoBot.Core.Services.Database.Repositories
{
    public interface IModLogRepository : IRepository<ModLog>
    {
        ModLog[] ForId(ulong guildId, ulong userId);
        ModLog[] ByDateTime(ulong guildId, ulong moderator);
    }
}
