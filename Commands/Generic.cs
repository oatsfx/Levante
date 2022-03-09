using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Fergun.Interactive;
using Levante.Configs;

// ReSharper disable UnusedMember.Global

namespace Levante.Commands
{
    public class Generic : InteractionModuleBase<SocketInteractionContext>
    {
        public InteractiveService Interactive { get; set; }

        [SlashCommand("help", "Get the command list for Levante.")]
        public async Task Help()
        {
            var auth = new EmbedAuthorBuilder
            {
                Name = "Command List"
            };

            var embed = new EmbedBuilder
            {
                Color =
                    new Color(BotConfig.EmbedColorGroup.R, BotConfig.EmbedColorGroup.G, BotConfig.EmbedColorGroup.B),
                Author = auth,
                Title = "Command List"
            };

            var desc = Context.Client.GetGlobalApplicationCommandsAsync().Result.Aggregate("", (current, cmd) => current + $"/{cmd.Name}\n");
            embed.Description = desc;

            await RespondAsync(embed: embed.Build());
        }

        [SlashCommand("info", "Get the info of Felicity.")]
        public async Task InfoAsync()
        {
            var app = await Context.Client.GetApplicationInfoAsync();
            var embed = new EmbedBuilder();
            embed.WithColor(new Color(BotConfig.EmbedColorGroup.R, BotConfig.EmbedColorGroup.G,
                BotConfig.EmbedColorGroup.B));

            embed.ThumbnailUrl = app.IconUrl;

            embed.Title = "Bot Information";
            embed.Description =
                "Felicity is based on Levante, an [open-source](https://github.com/oatsfx/Levante) Discord bot using Discord.Net-Labs (Discord.NET Wrapper Nightly) for various Destiny 2 Needs. " +
                "This bot is actively developed by [@axsLeaf](https://twitter.com/axsLeaf) & [@OatsFX](https://twitter.com/OatsFX). It pulls most of its information from the Bungie API.";

            embed.AddField(x =>
                {
                    x.Name = "Guild Count";
                    x.Value = $"{Context.Client.Guilds.Count} Guilds";
                    x.IsInline = true;
                })
                .AddField(x =>
                {
                    x.Name = "User Count";
                    x.Value = $"{Context.Client.Guilds.Sum(guild => guild.MemberCount)} Users";
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
                    x.Name = "Support Server";
                    x.Value = "https://discord.flowerpat.ch/";
                    x.IsInline = true;
                });

            await RespondAsync(embed: embed.Build());
        }

        [SlashCommand("invite", "Gives an invite link for the bot.")]
        public async Task Invite()
        {
            var app = await Context.Client.GetApplicationInfoAsync();
            var embed = new EmbedBuilder();

            embed.WithColor(new Color(BotConfig.EmbedColorGroup.R, BotConfig.EmbedColorGroup.G,
                BotConfig.EmbedColorGroup.B));
            embed.ThumbnailUrl = app.IconUrl;
            embed.Title = "Invite Link";
            embed.Description =
                $"[Invite](https://discord.com/api/oauth2/authorize?client_id={Context.Client.CurrentUser.Id}&permissions=8&scope=applications.commands%20bot)";

            await RespondAsync(embed: embed.Build(), ephemeral: true);
        }

        [SlashCommand("ping", "Replies with latency in milliseconds.")]
        public async Task PingAsync()
        {
            var colors = new int[3];
            var latency = Context.Client.Latency;

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

            var embed = new EmbedBuilder
            {
                Color = new Color(colors[0], colors[1], colors[2]),
                Description = $"Pong! ({latency} ms)"
            };

            await RespondAsync(embed: embed.Build());
        }
    }
}