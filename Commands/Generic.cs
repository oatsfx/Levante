using Discord;
using System.Threading.Tasks;
using Levante.Configs;
using Fergun.Interactive;
using Discord.Interactions;
using System.Linq;

namespace Levante.Commands
{
    public class Generic : InteractionModuleBase<SocketInteractionContext>
    {
        public InteractiveService Interactive { get; set; }

        [SlashCommand("help", "Get the command list for Levante.")]
        public async Task Help()
        {
            var auth = new EmbedAuthorBuilder()
            {
                Name = "Command List",
                IconUrl = Context.Client.CurrentUser.GetAvatarUrl()
            };

            var embed = new EmbedBuilder()
            {
                Color = new Discord.Color(BotConfig.EmbedColorGroup.R, BotConfig.EmbedColorGroup.G, BotConfig.EmbedColorGroup.B),
                Author = auth,
            };

            string desc = "";
            foreach (var cmd in Context.Client.GetGlobalApplicationCommandsAsync().Result.OrderBy(cmd => cmd.Name))
                desc += $"/{cmd.Name}\n";
            embed.Description = desc;

            await RespondAsync(embed: embed.Build());
        }
        
        [SlashCommand("info", "Get the info of Levante.")]
        public async Task InfoAsync()
        {
            var app = await Context.Client.GetApplicationInfoAsync();
            var embed = new EmbedBuilder();
            embed.WithColor(new Discord.Color(BotConfig.EmbedColorGroup.R, BotConfig.EmbedColorGroup.G, BotConfig.EmbedColorGroup.B));

            embed.ThumbnailUrl = app.IconUrl;

            embed.Title = "Bot Information";
            embed.Description =
                "Levante is an [open-source](https://github.com/oatsfx/Levante) Discord bot using Discord.Net for various Destiny 2 Needs. " +
                "This bot is actively developed by [@OatsFX](https://twitter.com/OatsFX). It pulls most of its information from the Bungie API.";

            embed.AddField(x =>
            {
                x.Name = "Guild Count";
                x.Value = $"{Context.Client.Guilds.Count} Guilds";
                x.IsInline = true;
            })
            .AddField(x =>
            {
                x.Name = "User Count";
                x.Value = $"{Context.Client.Guilds.Sum(x => x.MemberCount)} Users";
                x.IsInline = true;
            })
            .AddField(x =>
            {
                x.Name = "Bot Version";
                x.Value = $"v{BotConfig.Version}";
                x.IsInline = true;
            })
            .AddField(x =>
            {
                x.Name = "Website";
                x.Value = $"https://{BotConfig.Website}/";
                x.IsInline = true;
            })
            .AddField(x =>
            {
                x.Name = "Support Server";
                x.Value = $"https://discord.gg/{BotConfig.SupportServer}";
                x.IsInline = true;
            });

            await RespondAsync(embed: embed.Build());
        }

        [SlashCommand("invite", "Gives an invite link for the bot.")]
        public async Task Invite()
        {
            var app = await Context.Client.GetApplicationInfoAsync();
            var embed = new EmbedBuilder();

            embed.WithColor(new Discord.Color(BotConfig.EmbedColorGroup.R, BotConfig.EmbedColorGroup.G, BotConfig.EmbedColorGroup.B));
            embed.ThumbnailUrl = app.IconUrl;
            embed.Title = "Invite Link";
            embed.Description =
                "__**Invite me to your server!**__" +
                "\n[Invite](https://discord.com/api/oauth2/authorize?client_id=882303133643047005&permissions=8&scope=applications.commands%20bot)";

            await RespondAsync(embed: embed.Build(), ephemeral: true);
        }

        [SlashCommand("ping", "Replies with latency in milliseconds.")]
        public async Task PingAsync()
        {
            int[] colors = new int[3];
            int latency = Context.Client.Latency;

            if (latency >= 0 && latency < 120)
            {
                colors[0] = 123;
                colors[1] = 232;
                colors[2] = 98;
            }
            else if (latency >= 120 && latency < 300)
            {
                colors[0] = 251;
                colors[1] = 254;
                colors[2] = 50;
            }
            else
            {
                colors[0] = 237;
                colors[1] = 69;
                colors[2] = 69;
            }

            var embed = new EmbedBuilder()
            {
                Color = new Discord.Color(colors[0], colors[1], colors[2]),
            };
            embed.Description =
                $"Pong! ({latency} ms)";

            await RespondAsync(embed: embed.Build());
        }
    }
}
