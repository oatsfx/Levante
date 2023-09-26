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
using Levante.Helpers;
using Levante.Rotations;
using System.Collections.Generic;
using Discord.Interactions;
using System.Diagnostics;
using Levante.Services;
using Levante.Util.Attributes;
using Serilog;

namespace Levante.Commands
{
    [DontAutoRegister]
    [DevGuildOnly]
    public class Owner : InteractionModuleBase<ShardedInteractionContext>
    {
        private readonly InteractiveService Interactive;
        private readonly XPLoggingService XpLoggingService;

        public Owner(XPLoggingService xpLoggingService, InteractiveService interactive)
        {
            XpLoggingService = xpLoggingService;
            Interactive = interactive;
        }

        [RequireOwner]
        [SlashCommand("force", "[OWNER]: Sends a button to force a daily/weekly reset.")]
        public async Task Force()
        {
            Emoji helpEmote = new("❔");

            var buttonBuilder = new ComponentBuilder()
                .WithButton("Force Daily Reset", customId: "dailyForce", ButtonStyle.Secondary, helpEmote, row: 0)
                .WithButton("Force Weekly Reset", customId: "weeklyForce", ButtonStyle.Secondary, helpEmote, row: 0);

            await RespondAsync("This shouldn't really be used... Additionally, make sure the API is not disabled.", components: buttonBuilder.Build());
        }

        [RequireBotStaff]
        [SlashCommand("active-logging-users", "[BOT STAFF]: Gets a list of the users that are using my XP logging feature.")]
        public async Task ActiveAFK()
        {
            var xpLoggingList = XpLoggingService.GetXpLoggingUsers();
            var priorityXpLoggingList = XpLoggingService.GetPriorityXpLoggingUsers();

            var app = await Context.Client.GetApplicationInfoAsync();
            var auth = new EmbedAuthorBuilder()
            {
                Name = $"Active XP Logging Users",
                IconUrl = app.IconUrl
            };
            string p = priorityXpLoggingList.Count != 0 ? $" (+{priorityXpLoggingList.Count})" : "";
            var foot = new EmbedFooterBuilder()
            {
                Text = $"{xpLoggingList.Count + priorityXpLoggingList.Count}/{XpLoggingService.GetMaxLoggingUsers()}{p} people are logging their XP."
            };
            var embed = new EmbedBuilder()
            {
                Color = new Discord.Color(BotConfig.EmbedColor.R, BotConfig.EmbedColor.G, BotConfig.EmbedColor.B),
                Author = auth,
                Footer = foot,
            };

            if (xpLoggingList.Count >= 1)
            {
                embed.Description = $"__Priority XP Logging List:__\n";
                foreach (var aau in priorityXpLoggingList)
                {
                    embed.Description +=
                        $"{aau.UniqueBungieName}: Level {aau.LastLevel}\n";
                }
                embed.Description += $"__XP Logging List:__\n";
                foreach (var aau in xpLoggingList)
                {
                    embed.Description +=
                        $"{aau.UniqueBungieName}: Level {aau.LastLevel}\n";
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
                    Color = new Discord.Color(BotConfig.EmbedColor.R, BotConfig.EmbedColor.G, BotConfig.EmbedColor.B),
                    Title = "Supporters"
                };
                string result = "";
                foreach (var supporter in BotConfig.BotSupportersDiscordIDs)
                    result += $"<@{supporter}>\n";

                embed.Description = result;
                await RespondAsync(embed: embed.Build());
                return;
            }

            if (BotConfig.BotSupportersDiscordIDs.Contains(User.Id))
            {
                BotConfig.BotSupportersDiscordIDs.Remove(User.Id);
                await RespondAsync($"{User.Mention} has been removed as a bot supporter!");
            }
            else
            {
                BotConfig.BotSupportersDiscordIDs.Add(User.Id);
                await RespondAsync($"Added {User.Mention} to my list of Supporters!");
            }
            var bConfig = new BotConfig();
            File.WriteAllText(BotConfig.FilePath, JsonConvert.SerializeObject(bConfig, Formatting.Indented));
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

            var buttonResponse = await Interactive.NextMessageComponentAsync(x => x.Channel.Id == Context.Channel.Id && x.User.Id == Context.User.Id, timeout: TimeSpan.FromSeconds(BotConfig.DurationToWaitForNextMessage));

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
                await buttonResponse.Value.Message.ModifyAsync(message => { message.Content = $"Sending to {DataConfig.AnnounceEmblemLinks.Count} channels..."; message.Components = new ComponentBuilder().Build(); message.Embed = null; });
                newOffer.CreateJSON();
                await SendToAllAnnounceChannels(newOffer);
                await buttonResponse.Value.Message.ModifyAsync(message => { message.Content = $"Sent to {DataConfig.AnnounceEmblemLinks.Count} channels!"; });
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
        [SlashCommand("new-countdown", "[BOT STAFF]: Add a new countdown to the /countdown command.")]
        public async Task NewCountdown([Summary("event", "Event that should be tracked on /countdown.")] string Name,
            [Summary("start-time", "Date and time the event starts.")] DateTime StartTime)
        {
            await DeferAsync();
            if (CountdownConfig.Countdowns.ContainsKey(Name))
            {
                await Context.Interaction.ModifyOriginalResponseAsync(message => message.Content = $"Countdown of name: \"{Name}\" already exists.");
                return;
            }

            if (StartTime < DateTime.Now)
            {
                await Context.Interaction.ModifyOriginalResponseAsync(message => message.Content = $"I won't add a countdown that has already occurred.");
                return;
            }

            var buttonBuilder = new ComponentBuilder()
                .WithButton("Yes", customId: $"sendYes", ButtonStyle.Success, row: 0)
                .WithButton("No", customId: $"sendNo", ButtonStyle.Danger, row: 0);

            var embed = new EmbedBuilder
            {
                Color = new Discord.Color(BotConfig.EmbedColor.R, BotConfig.EmbedColor.G, BotConfig.EmbedColor.B),
                Author = new EmbedAuthorBuilder() { IconUrl = Context.Client.CurrentUser.GetAvatarUrl() },
                Footer = new EmbedFooterBuilder() { Text = $"{Context.User.Username}" },
                Title = "New Countdown",
                Description = $"**{Name}**: Starts {TimestampTag.FromDateTime(StartTime, TimestampTagStyles.Relative)} ({TimestampTag.FromDateTime(StartTime, TimestampTagStyles.ShortDate)})"
            };

            await Context.Interaction.ModifyOriginalResponseAsync(message => { message.Content = "This is the countdown you are adding, continue?"; message.Components = buttonBuilder.Build(); message.Embed = embed.Build(); });
            var buttonResponse = await Interactive.NextMessageComponentAsync(x => x.Channel.Id == Context.Channel.Id && x.User.Id == Context.User.Id, timeout: TimeSpan.FromSeconds(BotConfig.DurationToWaitForNextMessage));

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
                CountdownConfig.AddCountdown(Name, StartTime);
                await buttonResponse.Value.Message.ModifyAsync(message => { message.Content = $"Added this countdown."; message.Components = new ComponentBuilder().Build(); });
            }
            else
            {
                await buttonResponse.Value.Message.ModifyAsync(message => { message.Content = "Did not add this countdown."; message.Components = new ComponentBuilder().Build(); });
            }
        }

        [RequireBotStaff]
        [SlashCommand("remove-countdown", "[BOT STAFF]: Remove a countdown from the /countdown command.")]
        public async Task RemoveCountdown([Summary("countdown", "Countdown to remove."), Autocomplete(typeof(CountdownAutocomplete))] string Name)
        {
            if (!CountdownConfig.Countdowns.ContainsKey(Name))
            {
                await RespondAsync($"No countdown found under \"{Name}\".");
                return;
            }

            await DeferAsync();
            var buttonBuilder = new ComponentBuilder()
                .WithButton("Yes", customId: $"removeYes", ButtonStyle.Success, row: 0)
                .WithButton("No", customId: $"removeNo", ButtonStyle.Danger, row: 0);

            var embed = new EmbedBuilder
            {
                Color = new Discord.Color(BotConfig.EmbedColor.R, BotConfig.EmbedColor.G, BotConfig.EmbedColor.B),
                Author = new EmbedAuthorBuilder() { IconUrl = Context.Client.CurrentUser.GetAvatarUrl() },
                Footer = new EmbedFooterBuilder() { Text = $"{Context.User.Username}" },
                Title = "Countdown Removal",
                Description = $"**{Name}**: Starts {TimestampTag.FromDateTime(CountdownConfig.Countdowns[Name], TimestampTagStyles.Relative)} ({TimestampTag.FromDateTime(CountdownConfig.Countdowns[Name], TimestampTagStyles.ShortDate)})"
            };

            await Context.Interaction.ModifyOriginalResponseAsync(message => { message.Content = "Remove this countdown?"; message.Embed = embed.Build(); message.Components = buttonBuilder.Build(); });

            var buttonResponse = await Interactive.NextMessageComponentAsync(x => x.Channel.Id == Context.Channel.Id && x.User.Id == Context.User.Id, timeout: TimeSpan.FromSeconds(BotConfig.DurationToWaitForNextMessage));

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
                CountdownConfig.RemoveCountdown(Name);

                await buttonResponse.Value.Message.ModifyAsync(message => { message.Content = "Removed this countdown."; message.Components = new ComponentBuilder().Build();});
            }
            else
            {
                await buttonResponse.Value.Message.ModifyAsync(message => { message.Content = "Did not remove this countdown."; message.Components = new ComponentBuilder().Build(); });
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
                Color = new Discord.Color(BotConfig.EmbedColor.R, BotConfig.EmbedColor.G, BotConfig.EmbedColor.B),
                Author = new EmbedAuthorBuilder() { IconUrl = Context.Client.CurrentUser.GetAvatarUrl() },
                Footer = new EmbedFooterBuilder() { Text = $"{removeHashes.Count} expired Emblem offers" },
                Title = "Remove these offers?",
                Description = result
            };

            var buttonBuilder = new ComponentBuilder()
                .WithButton("Yes", customId: $"removeYes", ButtonStyle.Success, row: 0)
                .WithButton("No", customId: $"removeNo", ButtonStyle.Danger, row: 0);

            await Context.Interaction.ModifyOriginalResponseAsync(message => { message.Embed = embed.Build(); message.Components = buttonBuilder.Build(); });

            var buttonResponse = await Interactive.NextMessageComponentAsync(x => x.Channel.Id == Context.Channel.Id && x.User.Id == Context.User.Id, timeout: TimeSpan.FromSeconds(BotConfig.DurationToWaitForNextMessage));

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

            var buttonResponse = await Interactive.NextMessageComponentAsync(x => x.Channel.Id == Context.Channel.Id && x.User.Id == Context.User.Id, timeout: TimeSpan.FromSeconds(BotConfig.DurationToWaitForNextMessage));

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
                var buttonResponse = await Interactive.NextMessageComponentAsync(x => x.Channel.Id == Context.Channel.Id && x.User.Id == Context.User.Id, timeout: TimeSpan.FromSeconds(BotConfig.DurationToWaitForNextMessage));

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
                    await buttonResponse.Value.Message.ModifyAsync(message => { message.Content = $"Sending to {DataConfig.AnnounceEmblemLinks.Count} channels..."; message.Components = new ComponentBuilder().Build(); message.Embed = null; });
                    await SendToAllAnnounceChannels(eo);
                    await buttonResponse.Value.Message.ModifyAsync(message => { message.Content = $"Sent {DataConfig.AnnounceEmblemLinks.Count} channels!"; message.Embed = eo.BuildEmbed().Build(); });
                }
                else
                {
                    await buttonResponse.Value.Message.ModifyAsync(message => { message.Content = "Did not send offer."; message.Components = new ComponentBuilder().Build(); });
                }
            }
        }

        [RequireOwner]
        [SlashCommand("flush-tracking", "[OWNER]: Force send out rotation tracking reminders.")]
        public async Task FlushTracking()
        {
            await DeferAsync();
            // Send reset embeds if applicable.
            if (DateTime.Today.DayOfWeek == DayOfWeek.Tuesday)
                await DataConfig.PostWeeklyResetUpdate(Context.Client);

            await DataConfig.PostDailyResetUpdate(Context.Client);

            // Send users their tracking if applicable.
            if (DateTime.Today.DayOfWeek == DayOfWeek.Tuesday)
                await CurrentRotations.CheckUsersWeeklyTracking(Context.Client);

            await CurrentRotations.CheckUsersDailyTracking(Context.Client);
            await Context.Interaction.ModifyOriginalResponseAsync(message => { message.Content = "Flushed tracking!"; });
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

            int old = XpLoggingService.GetMaxLoggingUsers();
            XpLoggingService.SetMaxLoggingUsers(NewMaxUserCount);

            await RespondAsync($"Changed maximum XP Logging users to {NewMaxUserCount} (was {old}).");
        }

        [RequireBotStaff]
        [SlashCommand("metrics", "[BOT STAFF]: Get somewhat confidential information about the bot.")]
        public async Task Metrics()
        {
            var app = await Context.Client.GetApplicationInfoAsync();
            var embed = new EmbedBuilder
            {
                Color = new Discord.Color(BotConfig.EmbedColor.R, BotConfig.EmbedColor.G, BotConfig.EmbedColor.B),
                Author = new EmbedAuthorBuilder() { IconUrl = Context.Client.CurrentUser.GetAvatarUrl() },
                Footer = new EmbedFooterBuilder() { Text = $"{BotConfig.AppName} v{BotConfig.Version}" },
                Title = "Metrics",
                ThumbnailUrl = BotConfig.BotLogoUrl
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
        [SlashCommand("check-manifest", "[BOT STAFF]: Check Bungie.net for an updated Manifest.")]
        public async Task CheckManifest()
        {
            await DeferAsync();
            if (ManifestHelper.IsNewManifest())
            {
                var oldVersion = ManifestHelper.DestinyManifestVersion;
                await Context.Interaction.ModifyOriginalResponseAsync(message => message.Content = $"New Manifest found! Updating the stuffs...");
                await Task.Run(() => ManifestHelper.LoadManifestDictionaries());
                await Context.Interaction.ModifyOriginalResponseAsync(message => message.Content = $"Updated! {oldVersion} -> {ManifestHelper.DestinyManifestVersion}");
                return;
            }

            await Context.Interaction.ModifyOriginalResponseAsync(message => message.Content = $"No new Manifest. Current Version: {ManifestHelper.DestinyManifestVersion}");
            return;
        }

        [RequireBotStaff]
        [SlashCommand("creations-stats", "[BOT STAFF]: Grab various statistics regarding Bungie's Community Creations page.")]
        public async Task CreationsStats()
        {
            await DeferAsync();
            Dictionary<KeyValuePair<string, string>, int> moderators = new();
            DateTime oldest = DateTime.Now;
            int totalIndexed = 0;
            
            using (var client = new HttpClient())
            {
                try
                {
                    // Name, Id, Amount Approved
                    client.DefaultRequestHeaders.Add("X-API-Key", BotConfig.BungieApiKey);
                    int page = 0;
                    bool hasMore = true;
                    do
                    {
                        var response = client.GetAsync($"https://www.bungie.net/Platform/CommunityContent/Get/1/0/{page}/").Result;
                        var content = response.Content.ReadAsStringAsync().Result;
                        dynamic item = JsonConvert.DeserializeObject(content);
                        totalIndexed += (int)item.Response.totalResults;
                        hasMore = item.Response.hasMore;
                        foreach (var creation in item.Response.results)
                        {
                            // Find Editor
                            foreach (var author in item.Response.authors)
                            {
                                if ($"{author.membershipId}".Equals($"{creation.editorMembershipId}"))
                                {
                                    var editorNameId = new KeyValuePair<string, string>($"{author.displayName}", $"{author.membershipId}");
                                    if (moderators.ContainsKey(editorNameId))
                                    {
                                        moderators[editorNameId]++;
                                    }
                                    else
                                        moderators.Add(editorNameId, 1);
                                }

                                if (oldest > DateTime.SpecifyKind(DateTime.Parse($"{creation.creationDate}"), DateTimeKind.Utc))
                                    oldest = DateTime.SpecifyKind(DateTime.Parse($"{creation.creationDate}"), DateTimeKind.Utc);
                            }
                        }
                        page++;
                    }
                    while (hasMore);
                }
                catch (Exception x)
                {
                    Console.WriteLine($"{x}");
                }
            }

            var sorted = moderators.OrderBy(x => x.Value).Reverse();
            var auth = new EmbedAuthorBuilder()
            {
                Name = $"Community Creations Stats",
            };
            var foot = new EmbedFooterBuilder()
            {
                Text = $"Powered by the Bungie API"
            };
            var embed = new EmbedBuilder
            {
                Color = new Discord.Color(BotConfig.EmbedColor.R, BotConfig.EmbedColor.G, BotConfig.EmbedColor.B),
                Author = auth,
                Footer = foot,
                Timestamp = DateTime.Now,
                Description = $"Indexed {totalIndexed} Creations from {TimestampTag.FromDateTime(oldest)} to Now."
            };
            foreach (var moderator in sorted)
            {
                embed.AddField(x =>
                {
                    x.Name = $"{moderator.Key.Key}";
                    x.Value = $"{moderator.Value} ({(double)(moderator.Value / (double) totalIndexed)*100:0.00}%)";
                    x.IsInline = true;
                });
            }
            await Context.Interaction.ModifyOriginalResponseAsync(x => { x.Embed = embed.Build(); });
        }

        [RequireBotStaff]
        [SlashCommand("my-creations", "[BOT STAFF]: Find your Community Creation uploads.")]
        public async Task Testing()
        {
            await DeferAsync();
            var linkedUser = DataConfig.GetLinkedUser(Context.User.Id);
            DateTime oldest = DateTime.Now;
            int totalIndexed = 0;

            var auth = new EmbedAuthorBuilder()
            {
                Name = $"Creations by {linkedUser.UniqueBungieName}",
            };
            var foot = new EmbedFooterBuilder()
            {
                Text = $"Powered by the Bungie API"
            };
            var embed = new EmbedBuilder()
            {
                Color = new Discord.Color(BotConfig.EmbedColor.R, BotConfig.EmbedColor.G, BotConfig.EmbedColor.B),
                Author = auth,
                Footer = foot,
                Timestamp = DateTime.Now,
            };

            using (var client = new HttpClient())
            {
                try
                {
                    // Name, Id, Amount Approved
                    client.DefaultRequestHeaders.Add("X-API-Key", BotConfig.BungieApiKey);
                    int page = 0;
                    bool hasMore = true;
                    var response = client.GetAsync($"https://www.bungie.net/Platform/Destiny2/{linkedUser.BungieMembershipType}/Profile/{linkedUser.BungieMembershipID}/LinkedProfiles/").Result;
                    var content = response.Content.ReadAsStringAsync().Result;
                    dynamic item = JsonConvert.DeserializeObject(content);
                    string bngMemId = $"{item.Response.bnetMembership.membershipId}";

                    do
                    {
                        response = client.GetAsync($"https://www.bungie.net/Platform/CommunityContent/Get/1/0/{page}/").Result;
                        content = response.Content.ReadAsStringAsync().Result;
                        item = JsonConvert.DeserializeObject(content);
                        totalIndexed += (int)item.Response.totalResults;
                        hasMore = item.Response.hasMore;
                        bool pageHasAuthor = false;

                        foreach (var author in item.Response.authors)
                        {
                            if ($"{author.membershipId}".Equals(bngMemId))
                                pageHasAuthor = true;
                        }

                        foreach (var creation in item.Response.results)
                        {
                            // Find Editor
                            if ($"{creation.authorMembershipId}".Equals(bngMemId))
                            {
                                embed.AddField(x =>
                                {
                                    x.Name = $"{creation.subject}";
                                    x.Value = $"Upvotes: **{creation.upvotes}**\n" +
                                        $"Uploaded: {TimestampTag.FromDateTime(DateTime.SpecifyKind(DateTime.Parse($"{creation.creationDate}"), DateTimeKind.Utc), TimestampTagStyles.ShortDate)}\n" +
                                        $"Approved: {TimestampTag.FromDateTime(DateTime.SpecifyKind(DateTime.Parse($"{creation.lastModified}"), DateTimeKind.Utc), TimestampTagStyles.ShortDate)}\n" +
                                        $"[LINK](https://www.bungie.net/en/Community/Detail?itemId={creation.postId})";
                                    x.IsInline = true;
                                });
                            }

                            if (oldest > DateTime.SpecifyKind(DateTime.Parse($"{creation.creationDate}"), DateTimeKind.Utc))
                                oldest = DateTime.SpecifyKind(DateTime.Parse($"{creation.creationDate}"), DateTimeKind.Utc);
                        }
                        page++;
                    }
                    while (hasMore);
                }
                catch (Exception x)
                {
                    Console.WriteLine($"{x}");
                }
            }
            embed.Description = $"Indexed {totalIndexed} Creations from {TimestampTag.FromDateTime(oldest)} to Now.";
            await Context.Interaction.ModifyOriginalResponseAsync(x => { x.Embed = embed.Build(); });
        }

        [RequireBotStaff]
        [SlashCommand("test", "[BOT STAFF]: Testing, testing... 1... 2.")]
        public async Task Test()
        {
            var embed = new EmbedBuilder
            {
                Color = new Discord.Color(BotConfig.EmbedColor.R, BotConfig.EmbedColor.G, BotConfig.EmbedColor.B),
                Author = new EmbedAuthorBuilder() { IconUrl = Context.Client.CurrentUser.GetAvatarUrl() },
                Footer = new EmbedFooterBuilder() { Text = $"{BotConfig.AppName} v{BotConfig.Version}" },
                Title = "???",
                ThumbnailUrl = BotConfig.BotLogoUrl,
                Description = "Nothing found."
            };

            embed.AddField(x =>
            {
                x.Name = "???";
                x.Value = $"???";
                x.IsInline = true;
            });

            await RespondAsync(embed: embed.Build());
        }

        public async Task SendToAllAnnounceChannels(EmblemOffer offer)
        {
            var embed = offer.BuildEmbed();
            var button = offer.BuildExternalButton();
            foreach (var Link in DataConfig.AnnounceEmblemLinks.ToList())
            {
                var channel = Context.Client.GetChannel(Link.ChannelID) as SocketTextChannel;
                var guildChannel = Context.Client.GetChannel(Link.ChannelID) as SocketGuildChannel;
                try
                {
                    if (channel == null || guildChannel == null)
                    {
                        Log.Information("[{Type}] Could not find channel {Id}.", "Offers", Link.ChannelID);
                        continue;
                    }

                    foreach (var chan in guildChannel.Guild.TextChannels)
                    {
                        if (DataConfig.IsExistingEmblemLinkedChannel(chan.Id) && Link.ChannelID != chan.Id)
                        {
                            Log.Information("[{Type}] Duplicate channel detected. Removing: {Id}", "Offers", chan.Id);
                            DataConfig.DeleteEmblemChannel(chan.Id);
                        }
                    }

                    if (Link.RoleID != 0)
                    {
                        var role = Context.Client.GetGuild(guildChannel.Guild.Id).GetRole(Link.RoleID);
                        var msg = await channel.SendMessageAsync($"{role.Mention}", embed: embed.Build(), components: button.Build());
                        if (channel is SocketNewsChannel && channel.Guild.Id == BotConfig.SupportServerID)
                            await msg.CrosspostAsync();
                    }
                    else
                    {
                        // Crosspost/Publish for the support server news channel.
                        var msg = await channel.SendMessageAsync("", embed: embed.Build(), components: button.Build());
                        if (channel is SocketNewsChannel && channel.Guild.Id == BotConfig.SupportServerID)
                            await msg.CrosspostAsync();
                    }
                }
                catch (Exception x)
                {
                    //LogHelper.ConsoleLog($"[OFFERS] Could not send to channel {guildChannel.Id} in guild {guildChannel.Guild.Id}.");
                    Log.Warning("[{Type}] Could not send to channel {ChannelId} in guild {GuildId}. Reason: {Reason}", "Offers", guildChannel.Id, guildChannel.Guild.Id, x);
                }
            }
        }
    }
}
