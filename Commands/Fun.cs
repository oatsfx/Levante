using Discord;
using System.Threading.Tasks;
using Levante.Configs;
using Discord.Interactions;
using System;
using System.Collections.Generic;
using Levante.Helpers;
using System.Net.Http;
using Newtonsoft.Json;
using System.Linq;
using BungieSharper.Entities.Destiny.Definitions;
using BungieSharper.Entities.Destiny;

namespace Levante.Commands
{
    public class Fun : InteractionModuleBase<SocketInteractionContext>
    {
        [SlashCommand("ratio", "Get ratioed.")]
        public async Task Ratio()
        {
            await DeferAsync();
            string[] ratioMsgs =
            {
                "Counter",
                "L",
                "You fell off",
                "Skill issue",
                "GG",
                "Ice cold",
                "Bozo",
                "Ok and?",
                "Who asked?",
                "💀 x7",
                "Cope",
            };

            var random = new Random();
            int numOfMsgs = random.Next(1, 4);
            List<string> msgs = new List<string>();
            
            for (int i = 0; i < numOfMsgs; i++)
            {
                int msgNum = random.Next(ratioMsgs.Length);
                if (msgs.Contains(ratioMsgs[msgNum]))
                {
                    i--;
                }
                else
                {
                    msgs.Add(ratioMsgs[msgNum]);
                }
            }

            string result = "";
            foreach (var msg in msgs)
                result += $"{msg} + ";

            if (!DataConfig.IsExistingLinkedUser(Context.User.Id))
                result += $"You aren't linked";
            else
            {
                var dil = DataConfig.GetLinkedUser(Context.User.Id);
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("X-API-Key", BotConfig.BungieApiKey);
                    client.DefaultRequestHeaders.Add("Authorization", $"Bearer {dil.AccessToken}");

                    var response = client.GetAsync($"https://www.bungie.net/Platform/Destiny2/" + dil.BungieMembershipType + "/Profile/" + dil.BungieMembershipID + "/?components=800").Result;
                    var content = response.Content.ReadAsStringAsync().Result;
                    dynamic item = JsonConvert.DeserializeObject(content);

                    bool HasEmblem = true;
                    while (HasEmblem)
                    {
                        try
                        {
                            var emblem = ManifestHelper.Emblems.ElementAt(random.Next(0, ManifestHelper.EmblemsCollectible.Count));
                            var emblemCollectible = ManifestHelper.EmblemsCollectible[emblem.Key];
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
            }
            Emoji thumbsUp = new Emoji("👍");
            await Context.Interaction.ModifyOriginalResponseAsync(message => { message.Content = result; }).Result.AddReactionAsync(thumbsUp);
        }
    }
}
