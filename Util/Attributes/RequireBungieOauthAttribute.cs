using Discord;
using Discord.Interactions;
using Levante.Configs;
using System;
using System.Threading.Tasks;

namespace Levante.Util.Attributes
{
    public class RequireBungieOauthAttribute : PreconditionAttribute
    {
        public override Task<PreconditionResult> CheckRequirementsAsync(IInteractionContext context, ICommandInfo command, IServiceProvider services)
        {
            if (!DataConfig.IsExistingLinkedUser(context.User.Id))
                return Task.FromResult(PreconditionResult.FromError("You don't have a Destiny 2 account linked, I need this information to get your data from Bungie's API. Get started with the `/link` command."));

            if (DataConfig.IsRefreshTokenExpired(context.User.Id))
                return Task.FromResult(PreconditionResult.FromError("I have lost privileges to your Destiny 2 data. This is common if you've been using my services for awhile. You'll have to link again with the `/link` command."));

            return Task.FromResult(PreconditionResult.FromSuccess());
        }
    }
}
