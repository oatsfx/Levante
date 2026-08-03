using Discord;
using System;
using System.Threading.Tasks;
using Levante.Configs;
using Levante.Util;
using System.Net.Http;
using System.Linq;
using System.Text;
using Discord.WebSocket;
using Fergun.Interactive;
using Newtonsoft.Json;
using System.IO;
using System.Collections.Generic;
using Discord.Interactions;
using System.Diagnostics;
using Levante.Util.Attributes;
using Serilog;
using BungieSharper.Entities.Destiny;
using Levante.Services;
using Levante.Helpers;
using Levante.Structs;
using System.Xml.Linq;
using System.Collections;
using static System.Net.Mime.MediaTypeNames;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Levante.Commands
{
    [DontAutoRegister]
    [DevGuildOnly]
    public class Owner : InteractionModuleBase<ShardedInteractionContext>
    {
        public LoggingService Logging { get; set; }
        public InteractiveService Interactive { get; set; }
        public UserService Users { get; set; }
        public GuildService Guilds { get; set; }

        public Owner(LoggingService xpLoggingService, InteractiveService interactive, UserService users, GuildService guilds)
        {
            Logging = xpLoggingService;
            Interactive = interactive;
            Users = users;
            Guilds = guilds;
        }

        //[RequireBotStaff]
        //[SlashCommand("migrate-emotes", "[OWNER]: Go through all of the emotes in the emote servers and add them to my config.")]
        //public async Task Force([Summary("start", "Select a server index to start at.")] int start = 0)
        //{
        //    await DeferAsync();

        //    var servers = new List<ulong>
        //    {
        //        915020047154565220,
        //        690373608228519997,
        //        933971948965359626,
        //        947726767907553311,
        //        955276637442813983,
        //        955276672087777280,
        //        955276702492295279,
        //        955276738215166002,
        //        955277044906868786,
        //        974466039989538848,
        //        1021147734138441768,
        //        1049879387002122270,
        //        1081668729693286420,
        //        1128754351800852538,
        //        1247634843857326110,

        //    };
        //    servers.RemoveRange(0, start);

        //    var fields = typeof(DestinyEmote)
        //        .GetFields(BindingFlags.Public | BindingFlags.Static);

        //    var fieldsA = typeof(Emotes)
        //        .GetFields(BindingFlags.Public | BindingFlags.Static);

        //    var regex = new Regex(@"<a?:(\w+):\d+>");

        //    var emoteNames = fields.Concat(fieldsA)
        //        .Select(f => f.GetValue(null) as string)
        //        .Where(val => !string.IsNullOrEmpty(val) && regex.IsMatch(val))
        //        .Select(val => regex.Match(val).Groups[1].Value)
        //        .ToList();

        //    var buttonBuilder = new ComponentBuilder()
        //        .WithButton("Yes", customId: $"sendYes", ButtonStyle.Success, row: 0)
        //        .WithButton("No", customId: $"sendNo", ButtonStyle.Danger, row: 0)
        //        .WithButton("Cancel", customId: $"sendCancel", ButtonStyle.Secondary, row: 0);

        //    using var client = new HttpClient();
        //    client.DefaultRequestHeaders.UserAgent.ParseAdd($"Mozilla/5.0 (compatible; {AppConfig.App.Name}/{AppConfig.App.Version})");
        //    var emotes = EmoteConfig.GetEmotes();

        //    foreach (var id in servers)
        //    {
        //        var server = Context.Client.GetGuild(id);
        //        Log.Debug("Starting guild: {server}.", server.Name);

        //        var auth = new EmbedAuthorBuilder()
        //        {
        //            Name = $"{server.Name}"
        //        };
        //        var foot = new EmbedFooterBuilder()
        //        {
        //            Text = $"Guild ID: {server.Id}"
        //        };

        //        foreach (var emote in await server.GetEmotesAsync())
        //        {
        //            if (emotes.Any(x => x.Name.Equals(emote.Name)))
        //                continue;

        //            if (!emoteNames.Any(x => x.Equals(emote.Name)))
        //                continue;

        //            var byteArray = await client.GetByteArrayAsync(emote.Url).ConfigureAwait(false);
        //            await EmoteConfig.AddEmote(emote.Name, new Discord.Image(new MemoryStream(byteArray)));
        //            Log.Debug("Added Emote: {Emote} to my own emotes.", emote.Name);

        //            //var embed = new EmbedBuilder()
        //            //{
        //            //    Color = new Discord.Color(AppConfig.Discord.EmbedColor.R, AppConfig.Discord.EmbedColor.G, AppConfig.Discord.EmbedColor.B),
        //            //    Author = auth,
        //            //    Footer = foot,
        //            //};
        //            //embed.Description = $"# Emote: {emote} ({emote.Name}) `{emote}`\n" +
        //            //    $"Used in DestinyEmote? **{emoteNames.Any(x => x.Equals(emote.Name))}**\n" +
        //            //    $"Managed by app? **{emote.IsManaged}**\n" +
        //            //    $"Created: {new TimestampTag(emote.CreatedAt, TimestampTagStyles.LongDateTime)}";

        //            //if (emote.CreatorId is ulong userId)
        //            //    embed.Description += $"\nby: {Context.Client.GetUser(userId).Mention}";

        //            //embed.Description += $"";

        //            //await Context.Interaction.ModifyOriginalResponseAsync(message => { message.Embed = embed.Build(); message.Components = buttonBuilder.Build(); });

        //            //var buttonResponse = await Interactive.NextMessageComponentAsync(x => x.Channel.Id == Context.Channel.Id && x.User.Id == Context.User.Id, timeout: TimeSpan.FromSeconds(AppConfig.Discord.DurationToWaitForNextMessage));

        //            //if (buttonResponse == null)
        //            //{
        //            //    await Context.Interaction.ModifyOriginalResponseAsync(message => { message.Content = "Closed command, invaild response."; message.Components = new ComponentBuilder().Build(); message.Embed = null; });
        //            //    return;
        //            //}

        //            //if (buttonResponse.IsTimeout)
        //            //{
        //            //    await Context.Interaction.ModifyOriginalResponseAsync(message => { message.Content = "Closed command, timed out."; message.Components = new ComponentBuilder().Build(); message.Embed = null; });
        //            //    return;
        //            //}

        //            //if (buttonResponse.Value.Data.CustomId.Equals("sendYes"))
        //            //{
        //            //    await buttonResponse.Value!.DeferAsync();

        //            //    if (emotes.Any(x => x.Name.Equals(emote.Name)))
        //            //    {
        //            //        Log.Debug("{Emote} already exists in my emotes.", emote.Name);
        //            //        continue;
        //            //    }

        //            //    var byteArray = await client.GetByteArrayAsync(emote.Url).ConfigureAwait(false);
        //            //    await EmoteConfig.AddEmote(emote.Name, new Discord.Image(new MemoryStream(byteArray)));
        //            //    Log.Debug("Added Emote: {Emote} to my own emotes.", emote.Name);

        //            //}
        //            //else if (buttonResponse.Value.Data.CustomId.Equals("sendNo"))
        //            //{
        //            //    await buttonResponse.Value!.DeferAsync();
        //            //    Log.Debug("Did NOT add Emote: {Emote}.", emote.Name);
        //            //}
        //            //else
        //            //{
        //            //    await Context.Interaction.ModifyOriginalResponseAsync(message => { message.Content = "Cancelled operation."; message.Components = new ComponentBuilder().Build(); message.Embed = null; });
        //            //    return;
        //            //}
        //        }
        //    }
        //}


        [RequireBotStaff]
        [SlashCommand("active-logging-users", "[BOT STAFF]: Gets a list of the users that are using my XP logging feature.")]
        public async Task ActiveAFK()
        {
            var xpLoggingList = Logging.GetXpLoggingUsers();
            var priorityXpLoggingList = Logging.GetPriorityXpLoggingUsers();

            var app = await Context.Client.GetApplicationInfoAsync();
            var auth = new EmbedAuthorBuilder()
            {
                Name = $"Active XP Logging Users",
                IconUrl = app.IconUrl
            };
            string p = priorityXpLoggingList.Count != 0 ? $" (+{priorityXpLoggingList.Count})" : "";
            var foot = new EmbedFooterBuilder()
            {
                Text = $"{xpLoggingList.Count}/{Logging.GetMaxLoggingUsers()}{p} people are logging their XP."
            };
            var embed = new EmbedBuilder()
            {
                Color = new Discord.Color(AppConfig.Discord.EmbedColor.R, AppConfig.Discord.EmbedColor.G, AppConfig.Discord.EmbedColor.B),
                Author = auth,
                Footer = foot,
            };

            if (xpLoggingList.Count >= 1)
            {
                embed.Description = $"## Priority XP Logging List:\n";
                foreach (var aau in priorityXpLoggingList)
                {
                    embed.Description +=
                        $"{aau.UniqueBungieName}: Level {aau.Last.Level}\n";
                }
                embed.Description += $"## XP Logging List:\n";
                foreach (var aau in xpLoggingList)
                {
                    embed.Description +=
                        $"{aau.UniqueBungieName}: Level {aau.Last.Level}\n";
                }
            }
            else
            {
                embed.Description = "No users are using my XP logging feature.";
            }

            await RespondAsync(embed: embed.Build());
        }

        [RequireBotStaff]
        [SlashCommand("supporter", "[BOT STAFF]: Add or remove a bot supporter.")]
        public async Task AddSupporter([Summary("discord-id", "Discord ID of the user to handle supporter status for.")] IUser User = null)
        {
            if (User == null)
            {
                var embed = new EmbedBuilder
                {
                    Color = new Discord.Color(AppConfig.Discord.EmbedColor.R, AppConfig.Discord.EmbedColor.G, AppConfig.Discord.EmbedColor.B),
                    Title = "Supporters"
                };
                string result = "";
                foreach (var supporter in Users.GetSupporters())
                    result += $"<@{supporter.Key}>\n";

                embed.Description = result;
                await RespondAsync(embed: embed.Build());
                return;
            }

            if (Users.GetSupporters().ContainsKey(User.Id))
            {
                Users.RemoveSupporter(User.Id);
                await RespondAsync($"{User.Mention} has been removed as a bot supporter!");
            }
            else
            {
                Users.AddSupporter(User.Id);
                await RespondAsync($"Added {User.Mention} to my list of Supporters!");
            }
        }

        [RequireBotStaff]
        [SlashCommand("new-offer", "[BOT STAFF]: Create an Emblem Offer using a JSON file.")]
        public async Task NewEmblemOffer([Summary("offer-json", "JSON file for the offer.")] IAttachment attachment)
        {
            await DeferAsync();
            string content;
            using (var httpCilent = new HttpClient())
            {
                var url = attachment.Url;
                byte[] bytes = await httpCilent.GetByteArrayAsync(url);
                content = Encoding.UTF8.GetString(bytes);
            }
            EmblemOffer newOffer;

            try
            {
                newOffer = JsonConvert.DeserializeObject<EmblemOffer>(content);
            }
            catch
            {
                await Context.Interaction.ModifyOriginalResponseAsync(message => message.Content = "Error with JSON file. Make sure all values are correct.");
                return;
            }

            if (EmblemOffer.HasExistingOffer(newOffer.EmblemHashCode))
            {
                await Context.Interaction.ModifyOriginalResponseAsync(message => message.Content = "Emblem has an offer already. Delete it first.");
                return;
            }

            var buttonBuilder = newOffer.BuildExternalButton()
                .WithButton("Post Offer", customId: $"newPost", ButtonStyle.Success, row: 1)
                .WithButton("Skip Posting", customId: $"newSkip", ButtonStyle.Secondary, row: 1)
                .WithButton("Cancel", customId: $"newCancel", ButtonStyle.Danger, row: 1);

            await Context.Interaction.ModifyOriginalResponseAsync(message => { message.Content = "This is what the embed looks like. What would you like to do?"; message.Embed = newOffer.BuildEmbed().Build(); message.Components = buttonBuilder.Build(); });

            var buttonResponse = await Interactive.NextMessageComponentAsync(x => x.Channel.Id == Context.Channel.Id && x.User.Id == Context.User.Id, timeout: TimeSpan.FromSeconds(AppConfig.Discord.DurationToWaitForNextMessage));

            if (buttonResponse == null)
            {
                await Context.Interaction.ModifyOriginalResponseAsync(message => { message.Content = "Closed command, invaild response."; message.Components = new ComponentBuilder().Build(); message.Embed = null; });
                return;
            }

            if (buttonResponse.IsTimeout)
            {
                await Context.Interaction.ModifyOriginalResponseAsync(message => { message.Content = "Closed command, timed out."; message.Components = new ComponentBuilder().Build(); message.Embed = null; });
                return;
            }

            if (buttonResponse.Value.Data.CustomId.Equals("newPost"))
            {
                await buttonResponse.Value.DeferAsync();
                await buttonResponse.Value.Message.ModifyAsync(message => { message.Content = $"Sending to {Guilds.GetEmblemChannelCount()} channels..."; message.Components = new ComponentBuilder().Build(); message.Embed = null; });
                newOffer.CreateJSON();
                await Guilds.PostToEmblemChannels(newOffer);
                await buttonResponse.Value.Message.ModifyAsync(message => { message.Content = $"Sent to {Guilds.GetEmblemChannelCount()} channels!"; });
                return;
            }
            else if (buttonResponse.Value.Data.CustomId.Equals("newSkip"))
            {
                await buttonResponse.Value.DeferAsync();
                newOffer.CreateJSON();
                await buttonResponse.Value.Message.ModifyAsync(message => { message.Content = $"Created Emblem Offer, but did not send to channels."; message.Components = new ComponentBuilder().Build(); message.Embed = null; });
                return;
            }
            else
            {
                await buttonResponse.Value.DeferAsync();
                await buttonResponse.Value.Message.ModifyAsync(message => { message.Content = $"Cancelled operation."; message.Components = new ComponentBuilder().Build(); message.Embed = null; });
                return;
            }
        }

        [RequireBotStaff]
        [SlashCommand("flush-offers", "[BOT STAFF]: Remove all Emblem Offers where the end date has passed.")]
        public async Task RemoveOffersAsync()
        {
            await DeferAsync();
            string result = "";
            var removeHashes = new List<long>();
            foreach (var Offer in EmblemOffer.CurrentOffers)
            {
                if (Offer.EndDate != null && Offer.EndDate < DateTime.Now)
                {
                    removeHashes.Add(Offer.EmblemHashCode);
                    result += $"[{Offer.OfferedEmblem.GetName()}]({Offer.SpecialUrl}): Ended {TimestampTag.FromDateTime((DateTime)Offer.EndDate, TimestampTagStyles.Relative)}.\n";
                }
            }

            if (removeHashes.Count == 0)
            {
                await Context.Interaction.ModifyOriginalResponseAsync(message => message.Content = "No emblem offers are past their end dates.");
                return;
            }

            var embed = new EmbedBuilder
            {
                Color = new Discord.Color(AppConfig.Discord.EmbedColor.R, AppConfig.Discord.EmbedColor.G, AppConfig.Discord.EmbedColor.B),
                Author = new EmbedAuthorBuilder() { IconUrl = Context.Client.CurrentUser.GetAvatarUrl() },
                Footer = new EmbedFooterBuilder() { Text = $"{removeHashes.Count} expired Emblem offers" },
                Title = "Remove these offers?",
                Description = result
            };

            var buttonBuilder = new ComponentBuilder()
                .WithButton("Yes", customId: $"removeYes", ButtonStyle.Success, row: 0)
                .WithButton("No", customId: $"removeNo", ButtonStyle.Danger, row: 0);

            await Context.Interaction.ModifyOriginalResponseAsync(message => { message.Embed = embed.Build(); message.Components = buttonBuilder.Build(); });

            var buttonResponse = await Interactive.NextMessageComponentAsync(x => x.Channel.Id == Context.Channel.Id && x.User.Id == Context.User.Id, timeout: TimeSpan.FromSeconds(AppConfig.Discord.DurationToWaitForNextMessage));

            if (buttonResponse == null)
            {
                await Context.Interaction.ModifyOriginalResponseAsync(message => { message.Content = "Closed command, invaild response."; message.Components = new ComponentBuilder().Build(); message.Embed = null; });
                return;
            }

            if (buttonResponse.IsTimeout)
            {
                await Context.Interaction.ModifyOriginalResponseAsync(message => { message.Content = "Closed command, timed out."; message.Components = new ComponentBuilder().Build(); message.Embed = null; });
                return;
            }

            if (buttonResponse.Value.Data.CustomId.Equals("removeYes"))
            {
                await buttonResponse.Value.DeferAsync();
                try
                {
                    foreach (var OfferHash in removeHashes)
                    {
                        var offerToDelete = EmblemOffer.GetSpecificOffer(OfferHash);
                        EmblemOffer.DeleteOffer(offerToDelete);
                    }
                        
                }
                catch (Exception x)
                {
                    Console.WriteLine($"{x}");
                }

                embed.Title = "Removed these offers";
                await buttonResponse.Value.Message.ModifyAsync(message => { message.Components = new ComponentBuilder().Build(); message.Embed = embed.Build(); });
                return;
            }
            else
            {
                embed.Title = "Did not remove these offers";
                await buttonResponse.Value.Message.ModifyAsync(message => { message.Components = new ComponentBuilder().Build(); message.Embed = embed.Build(); });
                return;
            }
        }

        [RequireBotStaff]
        [SlashCommand("remove-offer", "[BOT STAFF]: Remove an Emblem Offer.")]
        public async Task RemoveOfferAsync([Summary("offer-to-remove", "Offer to remove."), Autocomplete(typeof(CurrentOfferAutocomplete))] string EmblemHash)
        {
            await DeferAsync();
            long EmblemHashCode = long.Parse(EmblemHash);
            var offerToDelete = EmblemOffer.GetSpecificOffer(EmblemHashCode);

            var buttonBuilder = offerToDelete.BuildExternalButton()
                .WithButton("Yes", customId: $"removeYes", ButtonStyle.Success, row: 1)
                .WithButton("No", customId: $"removeNo", ButtonStyle.Danger, row: 1);

            await Context.Interaction.ModifyOriginalResponseAsync(message => { message.Content = "Remove this offer?";  message.Embed = offerToDelete.BuildEmbed().Build(); message.Components = buttonBuilder.Build(); });

            var buttonResponse = await Interactive.NextMessageComponentAsync(x => x.Channel.Id == Context.Channel.Id && x.User.Id == Context.User.Id, timeout: TimeSpan.FromSeconds(AppConfig.Discord.DurationToWaitForNextMessage));

            if (buttonResponse == null)
            {
                await Context.Interaction.ModifyOriginalResponseAsync(message => { message.Content = "Closed command, invaild response."; message.Components = new ComponentBuilder().Build(); message.Embed = null; });
                return;
            }

            if (buttonResponse.IsTimeout)
            {
                await Context.Interaction.ModifyOriginalResponseAsync(message => { message.Content = "Closed command, timed out."; message.Components = new ComponentBuilder().Build(); message.Embed = null; });
                return;
            }

            if (buttonResponse.Value.Data.CustomId.Equals("removeYes"))
            {
                EmblemOffer.DeleteOffer(offerToDelete);

                await buttonResponse.Value.Message.ModifyAsync(message => { message.Content = "Removed this offer."; message.Components = new ComponentBuilder().Build(); message.Embed = offerToDelete.BuildEmbed().Build(); });
            }
            else
            {
                await buttonResponse.Value.Message.ModifyAsync(message => { message.Content = "Did not remove this offer."; message.Components = new ComponentBuilder().Build(); message.Embed = offerToDelete.BuildEmbed().Build(); });
            }
        }

        [RequireBotStaff]
        [SlashCommand("send-offer", "[BOT STAFF]: Sends an Emblem Offer embed to all channels with announcements set up.")]
        public async Task SendOffer([Summary("offer-to-send", "Offer to send to ALL linked channels."), Autocomplete(typeof(CurrentOfferAutocomplete))] string EmblemHash)
        {
            EmblemOffer.LoadCurrentOffers();
            long EmblemHashCode = long.Parse(EmblemHash);
            if (!EmblemOffer.HasExistingOffer(EmblemHashCode))
            {
                await RespondAsync("Are you sure you entered the correct hash code? Please use an auto complete result.");
                return;
            }
            else
            {
                EmblemOffer eo = EmblemOffer.GetSpecificOffer(EmblemHashCode);
                var buttonBuilder = eo.BuildExternalButton()
                    .WithButton("Yes", customId: $"sendYes", ButtonStyle.Success, row: 1)
                    .WithButton("No", customId: $"sendNo", ButtonStyle.Danger, row: 1);
                await RespondAsync("This is what the embed looks like. Ready to send to all channels?", embed: eo.BuildEmbed().Build(), components: buttonBuilder.Build());
                var buttonResponse = await Interactive.NextMessageComponentAsync(x => x.Channel.Id == Context.Channel.Id && x.User.Id == Context.User.Id, timeout: TimeSpan.FromSeconds(AppConfig.Discord.DurationToWaitForNextMessage));

                if (buttonResponse == null)
                {
                    await Context.Interaction.ModifyOriginalResponseAsync(message => { message.Content = "Closed command, invaild response."; message.Components = new ComponentBuilder().Build(); message.Embed = null; });
                    return;
                }

                if (buttonResponse.IsTimeout)
                {
                    await Context.Interaction.ModifyOriginalResponseAsync(message => { message.Content = "Closed command, timed out."; message.Components = new ComponentBuilder().Build(); message.Embed = null; });
                    return;
                }

                if (buttonResponse.Value.Data.CustomId.Equals("sendYes"))
                {
                    await buttonResponse.Value.Message.ModifyAsync(message => { message.Content = $"Sending to {Guilds.GetEmblemChannelCount()} channels..."; message.Components = new ComponentBuilder().Build(); message.Embed = null; });
                    await Guilds.PostToEmblemChannels(eo);
                    await buttonResponse.Value.Message.ModifyAsync(message => { message.Content = $"Sent {Guilds.GetEmblemChannelCount()} channels!"; message.Embed = eo.BuildEmbed().Build(); });
                }
                else
                {
                    await buttonResponse.Value.Message.ModifyAsync(message => { message.Content = "Did not send offer."; message.Components = new ComponentBuilder().Build(); });
                }
            }
        }

        [RequireBotStaff]
        [SlashCommand("max-xp-users", "[BOT STAFF]: Change the max number of users allowed in XP Logging.")]
        public async Task ChangeMaxUsers([Summary("max-user-count", "The new maximum logging users.")] int NewMaxUserCount)
        {
            if (NewMaxUserCount > 100)
            {
                await RespondAsync($"That's too high.");
                return;
            }
            else if (NewMaxUserCount < 1)
            {
                await RespondAsync($"That's too low.");
                return;
            }

            int old = Logging.GetMaxLoggingUsers();
            Logging.SetMaxLoggingUsers(NewMaxUserCount);

            await RespondAsync($"Changed maximum XP Logging users to {NewMaxUserCount} (was {old}).");
        }

        [RequireBotStaff]
        [SlashCommand("metrics", "[BOT STAFF]: Get somewhat confidential information about the bot.")]
        public async Task Metrics()
        {
            var app = await Context.Client.GetApplicationInfoAsync();
            var embed = new EmbedBuilder
            {
                Color = new Discord.Color(AppConfig.Discord.EmbedColor.R, AppConfig.Discord.EmbedColor.G, AppConfig.Discord.EmbedColor.B),
                Author = new EmbedAuthorBuilder() { IconUrl = Context.Client.CurrentUser.GetAvatarUrl() },
                Footer = new EmbedFooterBuilder() { Text = $"{AppConfig.App.Name} v{AppConfig.App.Version}" },
                Title = "Metrics",
                ThumbnailUrl = AppConfig.App.LogoUrl
            };

            var process = Process.GetCurrentProcess();
            var uptime = DateTime.Now - process.StartTime;

            var sb = new StringBuilder();
            if (uptime.Days > 0)
                sb.Append($"{uptime.Days}d ");

            if (uptime.Hours > 0)
                sb.Append($"{uptime.Hours}h ");

            if (uptime.Minutes > 0)
                sb.Append($"{uptime.Minutes}m ");

            sb.Append($"{uptime.Seconds}s");

            embed.AddField(x =>
            {
                x.Name = "Memory Used";
                x.Value = $"{process.PrivateMemorySize64 / (double)1_000_000:0.00} MB";
                x.IsInline = true;
            }).AddField(x =>
            {
                x.Name = "Uptime";
                x.Value = $"{sb}";
                x.IsInline = true;
            }).AddField(x =>
            {
                x.Name = "Discord.Net Version";
                x.Value = $"{DiscordConfig.Version}";
                x.IsInline = true;
            })
            .AddField(x =>
            {
                x.Name = "Destiny Manifest Version";
                x.Value = $"{ManifestHelper.DestinyManifestVersion}";
                x.IsInline = true;
            });

            await RespondAsync(embed: embed.Build());
        }

        [RequireBotStaff]
        [SlashCommand("check-manifest", "[BOT STAFF]: Check Bungie.net for an updated ManifestHelper.")]
        public async Task CheckManifest()
        {
            await DeferAsync();
            if (ManifestHelper.IsNewManifest())
            {
                var oldVersion = ManifestHelper.DestinyManifestVersion;
                await Context.Interaction.ModifyOriginalResponseAsync(message => message.Content = $"New ManifestHelper found! Updating the stuffs...");
                await Task.Run(() => ManifestHelper.LoadManifestDictionaries());
                await Context.Interaction.ModifyOriginalResponseAsync(message => message.Content = $"Updated! {oldVersion} -> {ManifestHelper.DestinyManifestVersion}");
                return;
            }

            await Context.Interaction.ModifyOriginalResponseAsync(message => message.Content = $"No new manifest. Current Version: {ManifestHelper.DestinyManifestVersion}");
            return;
        }

        [RequireBotStaff]
        [SlashCommand("test", "[BOT STAFF]: Testing, testing... 1... 2.")]
        public async Task Test()
        {

        }
    }
}
