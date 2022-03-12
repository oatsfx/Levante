using System.IO;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Levante.Manifest;

// ReSharper disable UnusedMember.Global

namespace Levante.Commands
{
    public class ManifestQuery : ModuleBase<SocketCommandContext>
    {
        [Command("queryManifestById")]
        public async Task QueryManifestById(uint hashId)
        {
            var manifestId = unchecked((int) hashId);
            var item = ManifestConnection.ManifestRepository.GetInventoryItem(manifestId);

            await File.WriteAllTextAsync("tmpManifest.json", Encoding.Default.GetString(item.json));

            await Context.Channel.SendFileAsync("tmpManifest.json");
        }

        [Command("queryManifestByName")]
        public async Task QueryManifestByName(string itemName)
        {
            var item = ManifestConnection.ManifestRepository.GetInventoryItemByName(itemName);

            await File.WriteAllTextAsync("tmpManifest.json", Encoding.Default.GetString(item.json));

            await Context.Channel.SendFileAsync("tmpManifest.json");
        }
    }
}