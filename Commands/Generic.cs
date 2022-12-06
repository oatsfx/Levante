using Discord;
using System.Threading.Tasks;
using Levante.Configs;
using Fergun.Interactive;
using Discord.Interactions;
using System.Linq;
using System;
using Levante.Util;

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

            string desc = "For in-depth explanations for all commands and features, visit [this page](https://www.levante.dev/features/).\n" +
                "__Commands:__\n";
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

            embed.ThumbnailUrl = BotConfig.BotLogoUrl;

            embed.Title = "Bot Information";
            embed.Description =
                "Levante is an [open-source](https://github.com/oatsfx/Levante) Discord bot using Discord.Net for various Destiny 2 needs. " +
                "This bot is actively developed by [@OatsFX](https://twitter.com/OatsFX). It pulls most of its information from the Bungie API.";

            embed.AddField(x =>
            {
                x.Name = "Guild Count";
                x.Value = $"{Context.Client.Guilds.Count:n0} Guilds";
                x.IsInline = true;
            })
            .AddField(x =>
            {
                x.Name = "User Count";
                x.Value = $"{Context.Client.Guilds.Sum(x => x.MemberCount):n0} Users";
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
            var embed = new EmbedBuilder();

            embed.WithColor(new Discord.Color(BotConfig.EmbedColorGroup.R, BotConfig.EmbedColorGroup.G, BotConfig.EmbedColorGroup.B));
            embed.ThumbnailUrl = BotConfig.BotLogoUrl;
            embed.Title = "Invite Link";
            embed.Description =
                "__**Invite me to your server!**__" +
                "\n[Invite](https://invite.levante.dev)";

            await RespondAsync(embed: embed.Build(), ephemeral: true);
        }

        [SlashCommand("ping", "Replies with latency in milliseconds.")]
        public async Task PingAsync()
        {
            int[] colors = new int[3];
            int latency = Context.Client.Latency;

            if (latency >= 0 && latency < 110)
            {
                colors[0] = 123;
                colors[1] = 232;
                colors[2] = 98;
            }
            else if (latency >= 110 && latency < 300)
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
                Color = new Discord.Color(colors[0], colors[1], colors[2]),
                Description = $"Pong! ({latency} ms)"
            };

            await RespondAsync(embed: embed.Build());
        }

        [SlashCommand("support", "Supporting is the best way to keep Levante around and get supporter perks!")]
        public async Task Support()
        {
            var app = await Context.Client.GetApplicationInfoAsync();
            var auth = new EmbedAuthorBuilder()
            {
                IconUrl = BotConfig.BotAvatarUrl,
            };
            var embed = new EmbedBuilder()
            {
                Author = auth,
            };
            embed.WithColor(new Discord.Color(BotConfig.EmbedColorGroup.R, BotConfig.EmbedColorGroup.G, BotConfig.EmbedColorGroup.B));

            embed.ThumbnailUrl = BotConfig.BotLogoUrl;

            embed.Title = "Support Levante";
            embed.Url = "https://donate.levante.dev/";
            embed.Description =
                "Levante is available to everyone for free, and will continue to stay this way. " +
                "Anyone who supports Levante and her team by donating will be eligible for [supporter perks](https://www.levante.dev/features/). " +
                "Those extra perks are a way of saying thank you to all of our generous supporters.\n" +
                "> You can support us by boosting our [support server](https://support.levante.dev/) or by [donating directly](https://donate.levante.dev/).";

            var buttonBuilder = new ComponentBuilder()
                .WithButton("Support Levante", style: ButtonStyle.Link, url: $"https://donate.levante.dev/", emote: Emote.Parse(Emotes.KoFi), row: 0);

            await RespondAsync(embed: embed.Build(), components: buttonBuilder.Build());
        }
    }
}
