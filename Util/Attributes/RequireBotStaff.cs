using Levante.Configs;
using System;
using System.Threading.Tasks;
using Discord.Interactions;
using Discord;
using Levante.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Levante.Util.Attributes
{
    public sealed class RequireBotStaff : PreconditionAttribute
    {
        public override Task<PreconditionResult> CheckRequirementsAsync(IInteractionContext context, ICommandInfo command, IServiceProvider services)
        {
            var userService = services.GetService<UserService>();

            if (!userService.IsExistingUser(context.User.Id))
                return Task.FromResult(PreconditionResult.FromError("You are not permitted to run this command."));

            if (userService.GetUser(context.User.Id).IsStaff())
                return Task.FromResult(PreconditionResult.FromSuccess());

            // If not a Bot Staff, say they cannot run command.
            return Task.FromResult(PreconditionResult.FromError("You are not permitted to run this command."));
        }
    }
}
