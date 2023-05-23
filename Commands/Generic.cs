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
    public class Generic : InteractionModuleBase<ShardedInteractionContext>
    {
        public InteractiveService Interactive { get; set; }

        [SlashCommand("help", "Get the command list for Levante.")]
        public async Task Help([Summary("hide", "Hide this post from users except yourself. Default: false")] bool hide = false)
        {
            var auth = new EmbedAuthorBuilder()
            {
                Name = "Command List",
                IconUrl = Context.Client.CurrentUser.GetAvatarUrl()
            };

            var embed = new EmbedBuilder()
            {
                Color = new Discord.Color(BotConfig.EmbedColor.R, BotConfig.EmbedColor.G, BotConfig.EmbedColor.B),
                Author = auth,
            };

            string desc = "For in-depth explanations for all commands and features, visit [this page](https://www.levante.dev/features/).\n" +
                "__Commands:__\n";
            foreach (var cmd in Context.Client.Shards.FirstOrDefault().GetGlobalApplicationCommandsAsync().Result.OrderBy(cmd => cmd.Name))
                desc += $"/{cmd.Name}\n";
            embed.Description = desc;

            await RespondAsync(embed: embed.Build(), ephemeral: hide);
        }
        
        [SlashCommand("info", "Get information about Levante and the other projects she links you to.")]
        public async Task InfoAsync([Summary("hide", "Hide this post from users except yourself. Default: false")] bool hide = false)
        {
            var app = await Context.Client.GetApplicationInfoAsync();
            var embed = new EmbedBuilder();
            embed.WithColor(new Discord.Color(BotConfig.EmbedColor.R, BotConfig.EmbedColor.G, BotConfig.EmbedColor.B));

            embed.ThumbnailUrl = BotConfig.BotLogoUrl;

            embed.Title = "Bot Information";
            embed.Description =
                "Levante is an [open-source](https://github.com/oatsfx/Levante) Discord bot, developed by [@OatsFX](https://twitter.com/OatsFX), that uses Discord.NET for your Destiny 2 needs.";

            embed.AddField(x =>
            {
                x.Name = "Guild Count";
                x.Value = $"{Context.Client.Guilds.Count:n0} Guilds";
                x.IsInline = true;
            }).AddField(x =>
            {
                x.Name = "User Count";
                x.Value = $"{Context.Client.Guilds.Sum(x => x.MemberCount):n0} Users";
                x.IsInline = true;
            }).AddField(x =>
            {
                x.Name = "Shard Count";
                x.Value = $"{Context.Client.Shards.Count} Shards";
                x.IsInline = true;
            }).AddField(x =>
            {
                x.Name = "Bot Version";
                x.Value = $"v{BotConfig.Version}";
                x.IsInline = true;
            }).AddField(x =>
            {
                x.Name = "Website";
                x.Value = $"https://{BotConfig.Website}/";
                x.IsInline = true;
            }).AddField(x =>
            {
                x.Name = "Support Server";
                x.Value = $"https://discord.gg/{BotConfig.SupportServer}";
                x.IsInline = true;
            }).AddField(x =>
            {
                x.Name = "> Third-Parties";
                x.Value = "*Other projects you'll see linked through Levante.*";
                x.IsInline = false;
            });

            foreach (var thirdParty in BotConfig.ThirdPartyProjects)
            {
                string linkBuilder = "";
                foreach (var link in thirdParty.Links)
                    linkBuilder += $"\n[{link.Name}]({link.Link})";

                embed.AddField(x =>
                {
                    x.Name = thirdParty.Name;
                    x.Value = $"{thirdParty.Description}{linkBuilder}";
                    x.IsInline = true;
                });
            }

            //TODO: Make this a JSON object, not hard-coded bozo.

            await RespondAsync(embed: embed.Build(), ephemeral: hide);
        }

        [SlashCommand("invite", "Gives an invite link for the bot.")]
        public async Task Invite([Summary("hide", "Hide this post from users except yourself. Default: false")] bool hide = false)
        {
            var embed = new EmbedBuilder();

            embed.WithColor(new Discord.Color(BotConfig.EmbedColor.R, BotConfig.EmbedColor.G, BotConfig.EmbedColor.B));
            embed.ThumbnailUrl = BotConfig.BotLogoUrl;
            embed.Title = "Looking to have me in your server?";
            embed.Description =
                "The button below will redirect you to my invite page. **Make sure you are allowed to Manage Applications in the server you want me in!**";

            await RespondAsync(embed: embed.Build(), components: new ComponentBuilder().WithButton("Invite Levante", style: ButtonStyle.Link, url: "https://invite.levante.dev", emote: Emote.Parse(Emotes.Logo), row: 0).Build(), ephemeral: hide);
        }

        [SlashCommand("ping", "Replies with Levante's latency to Discord in milliseconds.")]
        public async Task PingAsync([Summary("hide", "Hide this post from users except yourself. Default: false")] bool hide = false)
        {
            var foot = new EmbedFooterBuilder()
            {
                Text = $"Powered by {BotConfig.AppName} v{BotConfig.Version}"
            };
            var embed = new EmbedBuilder
            {
                Footer = foot
            };

            int latency = 0;
            foreach (var shard in Context.Client.Shards)
            {
                latency += shard.Latency;
                embed.AddField(x =>
                {
                    x.Name = $"Shard {shard.ShardId}{(shard.ShardId == Context.Client.GetShardFor(Context.Guild).ShardId ? " ★" : "")}";

                    switch (shard.Latency)
                    {
                        case >= 0 and < 110: x.Value = "🟢"; break;
                        case >= 110 and < 300: x.Value = "🟡"; break;
                        default: x.Value = "🔴"; break;
                    }

                    x.Value += $" {shard.Latency} ms";
                    x.IsInline = true;
                });
            }
            latency /= Context.Client.Shards.Count;

            int[] colors = new int[3];
            switch (latency)
            {
                case >= 0 and < 110:
                    colors[0] = 123;
                    colors[1] = 232;
                    colors[2] = 98;
                    break;
                case >= 110 and < 300:
                    colors[0] = 251;
                    colors[1] = 254;
                    colors[2] = 50;
                    break;
                default:
                    colors[0] = 237;
                    colors[1] = 69;
                    colors[2] = 69;
                    break;
            }

            embed.Description = $"Pong! (Shard Average: {latency} ms)\n★ - Your Shard";
            embed.WithColor(new Discord.Color(colors[0], colors[1], colors[2]));

            await RespondAsync(embed: embed.Build(), ephemeral: hide);
        }

        [SlashCommand("support", "Supporting is the best way to keep Levante around and get supporter perks!")]
        public async Task Support([Summary("hide", "Hide this post from users except yourself. Default: false")] bool hide = false)
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
            embed.WithColor(new Discord.Color(BotConfig.EmbedColor.R, BotConfig.EmbedColor.G, BotConfig.EmbedColor.B));

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

            await RespondAsync(embed: embed.Build(), components: buttonBuilder.Build(), ephemeral: hide);
        }
    }
}
