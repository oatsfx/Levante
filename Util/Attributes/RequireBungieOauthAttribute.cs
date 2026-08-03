using Discord;
using Discord.Interactions;
using Levante.Services;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace Levante.Util.Attributes
{
    public class RequireBungieOauthAttribute : PreconditionAttribute
    {
        public override Task<PreconditionResult> CheckRequirementsAsync(IInteractionContext context, ICommandInfo command, IServiceProvider services)
        {
            var users = services.GetRequiredService<UserService>();

            if (!users.IsExistingUser(context.User.Id))
                return Task.FromResult(PreconditionResult.FromError("You don't have a Destiny 2 account linked, I need this information to get your data from Bungie's API. Get started with the `/link` command."));

            if (users.GetUser(context.User.Id).IsRefreshAccessExpired())
                return Task.FromResult(PreconditionResult.FromError("I have lost privileges to your Destiny 2 data. This is common if you've been using my services for awhile. You'll have to link again with the `/link` command."));

            return Task.FromResult(PreconditionResult.FromSuccess());
        }
    }
}
