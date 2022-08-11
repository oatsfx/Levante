using Levante.Configs;
using System;
using System.Threading.Tasks;
using Discord.Interactions;
using Discord;

namespace Levante.Util
{
    public sealed class RequireBotStaff : PreconditionAttribute
    {
        public override Task<PreconditionResult> CheckRequirementsAsync(IInteractionContext context, ICommandInfo command, IServiceProvider services)
        {
            if (BotConfig.BotStaffDiscordIDs.Contains(context.User.Id))
                return Task.FromResult(PreconditionResult.FromSuccess());

            // If not a Bot Staff, say they cannot run command.
            return Task.FromResult(PreconditionResult.FromError("You are not permitted to run this command."));
        }

        public static bool IsBotStaff(ulong DiscordID) => BotConfig.BotStaffDiscordIDs.Contains(DiscordID);
    }
}
