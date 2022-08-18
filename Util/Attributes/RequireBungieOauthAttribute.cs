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
            {

                return Task.FromResult(PreconditionResult.FromError("You don't have a Destiny 2 account linked, get started with the /link command."));
            }
            else
            {
                DataConfig.RefreshCode(DataConfig.GetLinkedUser(context.User.Id));
            }
            return Task.FromResult(PreconditionResult.FromSuccess());
        }
    }
}
