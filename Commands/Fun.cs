using Discord;
using System.Threading.Tasks;
using Discord.Interactions;
using System;
using System.Collections.Generic;
using System.Net.Http;
using Newtonsoft.Json;
using System.Linq;
using BungieSharper.Entities.Destiny.Definitions;
using BungieSharper.Entities.Destiny;
using Levante.Util;
using Levante.Services;
using Levante.Configs;
using Levante.Helpers;

namespace Levante.Commands
{
    public class Fun : InteractionModuleBase<ShardedInteractionContext>
    {
        private readonly UserService Users;

        [SlashCommand("ratio", "Get ratioed.")]
        public async Task Ratio()
        {
            await DeferAsync();
            string[] ratioMsgs =
            {
                "Ratio",
                "Counter",
                "L",
                "You fell off",
                "Skill issue",
                "GG",
                "EZ",
                "Ice cold",
                "Bozo",
                "Ok and?",
                "Who asked?",
                "Didn't ask",
                "💀 x7",
                "Cope",
                "Caught in 4K",
                "Rolled",
                "I own you",
                "You're mad",
                "Ran",
                "RRRRRAAAAAAAAHHHHHHH",
                $"Who is {Context.User.GlobalName}?",
                "67",
                "brainrot",
                "dead game",
                "yes, and?",
                "goon"
            };

            var random = new Random();
            int numOfMsgs = random.Next(1, 5);
            List<string> msgs = new List<string>();
            
            for (int i = 0; i < numOfMsgs; i++)
            {
                int msgNum = random.Next(ratioMsgs.Length);
                if (msgs.Contains(ratioMsgs[msgNum]))
                    i--;
                else
                    msgs.Add(ratioMsgs[msgNum]);
            }

            string result = "";
            foreach (var msg in msgs)
                result += $"{msg} + ";

            if (!Users.IsExistingUser(Context.User.Id))
                result += $"You aren't linked";
            else
            {
                var dil = Users.GetUser(Context.User.Id);
                if (dil == null)
                {
                    var embed = Embeds.GetErrorEmbed();
                    embed.Description = $"There was an error with your linked account. Try relinking with the `/link` command.";
                    await Context.Interaction.ModifyOriginalResponseAsync(message => { message.Embed = embed.Build(); });
                    return;
                }

                var content = await BungieApi.Query($"/Platform/Destiny2/{dil.BungieCredentials.MembershipType}/Profile/{dil.BungieCredentials.MembershipId}/?components=800", dil.BungieCredentials.AccessToken);
                dynamic item = JsonConvert.DeserializeObject(content);

                bool HasEmblem = true;
                while (HasEmblem)
                {
                    try
                    {
                        var emblem = ManifestHelper.Emblems.ElementAt(random.Next(0, ManifestHelper.EmblemsCollectible.Count));
                        var emblemCollectible = ManifestHelper.EmblemsCollectible[emblem.Key];
                        if (item.Response.profileCollectibles.data.collectibles[$"{emblemCollectible}"].state == null)
                        {
                            continue;
                        }

                        if (((DestinyCollectibleState)item.Response.profileCollectibles.data.collectibles[$"{emblemCollectible}"].state).HasFlag(DestinyCollectibleState.NotAcquired))
                        {
                            HasEmblem = false;
                            result += $"You don't have {emblem.Value}";
                            break;
                        }
                    }
                    catch
                    {
                        continue;
                    }
                }
            }
            Emoji thumbsUp = new("👍");
            await Context.Interaction.ModifyOriginalResponseAsync(message => { message.Content = result; }).Result.AddReactionAsync(thumbsUp);
        }
    }
}
