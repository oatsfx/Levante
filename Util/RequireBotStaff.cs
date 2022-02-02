using Levante.Configs;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Levante.Util
{
    public sealed class RequireBotStaff : PreconditionAttribute
    {
        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            if (BotConfig.BotStaffDiscordIDs.Contains(context.User.Id))
                return Task.FromResult(PreconditionResult.FromSuccess());

            // If not a Bot Staff, say they cannot run command.
            return Task.FromResult(PreconditionResult.FromError("You are not permitted to run this command."));
        }

        public static bool IsBotStaff(ulong DiscordID) => BotConfig.BotStaffDiscordIDs.Contains(DiscordID);
    }
}
