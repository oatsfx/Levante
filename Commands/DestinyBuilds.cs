using System;
using System.Net.Http;
using System.Threading.Tasks;
using Discord.Interactions;
using Levante.Configs;
using Newtonsoft.Json;
// ReSharper disable UnusedMember.Global
// ReSharper disable InvertIf

namespace Levante.Commands
{
    public class DestinyBuilds : InteractionModuleBase<SocketInteractionContext>
    {

        [SlashCommand("show-build", "Show characters equipped mods.")]
        public async Task ShowBuild()
        {
            var LinkedUser = DataConfig.GetLinkedUser(Context.User.Id);
            if (LinkedUser == null || !DataConfig.IsExistingLinkedUser(LinkedUser.DiscordID))
            {
                await RespondAsync("No user linked.");
                return;
            }

            await DeferAsync();

            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("X-API-Key", BotConfig.BungieApiKey);

            var response = client.GetAsync("https://www.bungie.net/platform/Destiny2/" +
                                           LinkedUser.BungieMembershipType + "/Profile/" +
                                           LinkedUser.BungieMembershipID + "?components=205,305").Result;
            var content = response.Content.ReadAsStringAsync().Result;
            dynamic item = JsonConvert.DeserializeObject(content);

            if (DataConfig.IsBungieAPIDown(content))
            {
                await Context.Interaction.ModifyOriginalResponseAsync(message =>
                {
                    message.Content = "Bungie API is temporarily down, try again later.";
                });
                return;
            }

            if (item == null || item.ErrorCode != 1)
            {
                await Context.Interaction.ModifyOriginalResponseAsync(message =>
                {
                    message.Content =
                        "An error occured with that account. Is there a connected Destiny 2 account?";
                });
                return;
            }

            try
            {
                // TODO: parse hunter/titan/warlock to arguments and find character id accordingly
                // hunter: 2305843009842534029
                // helmet: 3448274439
                // gauntlets: 3551918588 
                // chest: 14239492 
                // legs: 20886954 
                // class: 1585787867 

                var equippedItems = item.Response.characterEquipment.data["2305843009842534029"].items;

                var helmet = "";
                var arms = "";
                var chest = "";
                var legs = "";
                var classItem = "";

                foreach (var equippedItem in equippedItems)
                {
                    if (equippedItem.bucketHash == 3448274439)
                    {
                        helmet = equippedItem.itemInstanceId;
                        continue;
                    }

                    if (equippedItem.bucketHash == 3551918588)
                    {
                        arms = equippedItem.itemInstanceId;
                        continue;
                    }

                    if (equippedItem.bucketHash == 14239492)
                    {
                        chest = equippedItem.itemInstanceId;
                        continue;
                    }

                    if (equippedItem.bucketHash == 20886954)
                    {
                        legs = equippedItem.itemInstanceId;
                        continue;
                    }

                    if (equippedItem.bucketHash == 1585787867)
                    {
                        classItem = equippedItem.itemInstanceId;
                    }
                }

                var msg = "**Helmet:**\n";
                msg += GetMods(helmet, item, client);
                await Context.Interaction.ModifyOriginalResponseAsync(message => message.Content = msg + " <a:loading:872886173075378197>");
                msg += "\n**Arms:**\n";
                msg += GetMods(arms, item, client);
                await Context.Interaction.ModifyOriginalResponseAsync(message => message.Content = msg + " <a:loading:872886173075378197>");
                msg += "\n**Chest:**\n";
                msg += GetMods(chest, item, client);
                await Context.Interaction.ModifyOriginalResponseAsync(message => message.Content = msg + " <a:loading:872886173075378197>");
                msg += "\n**Legs:**\n";
                msg += GetMods(legs, item, client);
                await Context.Interaction.ModifyOriginalResponseAsync(message => message.Content = msg + " <a:loading:872886173075378197>");
                msg += "\n**Class:**\n";
                msg += GetMods(classItem, item, client);
                await Context.Interaction.ModifyOriginalResponseAsync(message => message.Content = msg);
            }
            catch (Exception e)
            {
                await Context.Interaction.ModifyOriginalResponseAsync(message =>
                    message.Content = $"{e.GetType()}: {e.Message}");
            }
        }

        private string GetMods(string I, dynamic item, HttpClient client)
        {
            var returnMsg = "";

            foreach (var mod in item.Response.itemComponents.sockets.data[I].sockets)
            {
                if (mod.plugHash == null)
                    continue;

                var resp = client.GetAsync("https://www.bungie.net/platform/Destiny2/Manifest/DestinyInventoryItemDefinition/" +
                                           mod.plugHash).Result;
                string cont = resp.Content.ReadAsStringAsync().Result;
                dynamic modItem = JsonConvert.DeserializeObject(cont);

                if (modItem != null && modItem.Response.tooltipStyle == "build")
                {
                    returnMsg += $"{modItem.Response.displayProperties.name} (cost: {modItem.Response.plug.energyCost.energyCost} [{GetEnergyType((int) modItem.Response.plug.energyCost.energyType)}])\n";
                }
            }

            return returnMsg;
        }

        public string GetEnergyType(int energyType)
        {
            return energyType switch
            {
                0 => "Any",
                1 => "Arc",
                2 => "Solar",
                3 => "Void",
                4 => "Ghost",
                5 => "Subclass",
                6 => "Stasis",
                _ => "Unknown"
            };
        }
    }
}
