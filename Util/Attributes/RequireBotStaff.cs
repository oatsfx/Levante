using Levante.Configs;
using System;
using System.Threading.Tasks;
using Discord.Interactions;
using Discord;
using Levante.Services;

namespace Levante.Util.Attributes
{
    public sealed class RequireBotStaff : PreconditionAttribute
    {
        private readonly UserService Users = new();

        public override Task<PreconditionResult> CheckRequirementsAsync(IInteractionContext context, ICommandInfo command, IServiceProvider services)
        {
            if (Users.GetUser(context.User.Id).IsStaff())
                return Task.FromResult(PreconditionResult.FromSuccess());

            // If not a Bot Staff, say they cannot run command.
            return Task.FromResult(PreconditionResult.FromError("You are not permitted to run this command."));
        }

        public static bool IsBotStaff(ulong DiscordID) => AppConfig.BotStaffDiscordIDs.Contains(DiscordID);
    }
}
