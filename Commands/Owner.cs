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
using Levante.Util.Attributes;

namespace Levante.Commands
{
    [DontAutoRegister]
    [DevGuildOnly]
    public class Owner : InteractionModuleBase<SocketInteractionContext>
    {
        public InteractiveService Interactive { get; set; }

        [RequireOwner]
        [SlashCommand("force", "[OWNER]: Sends a button to force a daily/weekly reset.")]
        public async Task Force()
        {
            Emoji helpEmote = new Emoji("❔");

            var buttonBuilder = new ComponentBuilder()
                .WithButton("Force Daily Reset", customId: "dailyForce", ButtonStyle.Secondary, helpEmote, row: 0)
                .WithButton("Force Weekly Reset", customId: "weeklyForce", ButtonStyle.Secondary, helpEmote, row: 0);

            await RespondAsync("This shouldn't really be used... Additionally, make sure the API is not disabled.", components: buttonBuilder.Build());
        }

        [RequireBotStaff]
        [SlashCommand("active-logging-users", "[BOT STAFF]: Gets a list of the users that are using my XP logging feature.")]
        public async Task ActiveAFK()
        {
            var app = await Context.Client.GetApplicationInfoAsync();
            var auth = new EmbedAuthorBuilder()
            {
                Name = $"Active XP Logging Users",
                IconUrl = app.IconUrl
            };
            string p = ActiveConfig.PriorityActiveAFKUsers.Count != 0 ? $" (+{ActiveConfig.PriorityActiveAFKUsers.Count})" : "";
            var foot = new EmbedFooterBuilder()
            {
                Text = $"{ActiveConfig.ActiveAFKUsers.Count}/{ActiveConfig.MaximumLoggingUsers}{p} people are logging their XP."
            };
            var embed = new EmbedBuilder()
            {
                Color = new Discord.Color(BotConfig.EmbedColorGroup.R, BotConfig.EmbedColorGroup.G, BotConfig.EmbedColorGroup.B),
                Author = auth,
                Footer = foot,
            };

            if (ActiveConfig.ActiveAFKUsers.Count >= 1)
            {
                embed.Description = $"__Priority XP Logging List:__\n";
                foreach (var aau in ActiveConfig.PriorityActiveAFKUsers)
                {
                    embed.Description +=
                        $"{aau.UniqueBungieName}: Level {aau.LastLevel}\n";
                }
                embed.Description += $"__XP Logging List:__\n";
                foreach (var aau in ActiveConfig.ActiveAFKUsers)
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

        [RequireOwner]
        [SlashCommand("give-config", "[OWNER]: Sends the botConfig.json file.")]
        public async Task GiveConfigs()
        {
            await Context.User.SendFileAsync(BotConfig.FilePath);
            await RespondAsync("Check your DMs.");
        }

        [RequireOwner]
        [SlashCommand("replace-config", "[OWNER]: Replaces the botConfig.json file and refreshes the bot's activity.")]
        public async Task ReplaceConfig([Summary("config-json", "Replacement botConfig.json file.")] IAttachment attachment)
        {
            using (var httpCilent = new HttpClient())
            {
                var url = attachment.Url;
                byte[] bytes = await httpCilent.GetByteArrayAsync(url);
                using (var fs = new FileStream(BotConfig.FilePath, FileMode.Create))
                {
                    fs.Write(bytes, 0, bytes.Length);
                }
            }

            await Refresh();
            await Context.Interaction.ModifyOriginalResponseAsync(x => x.Content = "Config replaced and bot is refreshed.");
        }

        [RequireOwner]
        [SlashCommand("refresh", "[OWNER]: Refreshes the bot's activity.")]
        public async Task Refresh()
        {
            ConfigHelper.CheckAndLoadConfigFiles();
            await Context.Client.SetActivityAsync(new Game($"{BotConfig.Notes[0]} | v{String.Format("{0:0.00#}", BotConfig.Version)}", ActivityType.Playing));

            await RespondAsync($"Activity refreshed.");
        }

        [RequireOwner]
        [SlashCommand("supporter", "[OWNER]: Add or remove a bot supporter.")]
        public async Task AddSupporter([Summary("discord-id", "Discord ID of the user to handle supporter status for.")] IUser User)
        {
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

            var buttonBuilder = new ComponentBuilder()
                .WithButton("Post Offer", customId: $"newPost", ButtonStyle.Success, row: 0)
                .WithButton("Skip Posting", customId: $"newSkip", ButtonStyle.Secondary, row: 0)
                .WithButton("Cancel", customId: $"newCancel", ButtonStyle.Danger, row: 0);

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
                await SendToAllAnnounceChannels(newOffer.BuildEmbed());
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

            var embed = new EmbedBuilder()
            {
                Color = new Discord.Color(BotConfig.EmbedColorGroup.R, BotConfig.EmbedColorGroup.G, BotConfig.EmbedColorGroup.B),
                Author = new EmbedAuthorBuilder() { IconUrl = Context.Client.CurrentUser.GetAvatarUrl() },
                Footer = new EmbedFooterBuilder() { Text = $"{Context.User.Username}" },
            };
            embed.Title = "New Countdown";
            embed.Description = $"**{Name}**: Starts {TimestampTag.FromDateTime(StartTime, TimestampTagStyles.Relative)} ({TimestampTag.FromDateTime(StartTime, TimestampTagStyles.ShortDate)})";

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

            var embed = new EmbedBuilder()
            {
                Color = new Discord.Color(BotConfig.EmbedColorGroup.R, BotConfig.EmbedColorGroup.G, BotConfig.EmbedColorGroup.B),
                Author = new EmbedAuthorBuilder() { IconUrl = Context.Client.CurrentUser.GetAvatarUrl() },
                Footer = new EmbedFooterBuilder() { Text = $"{Context.User.Username}" },
            };
            embed.Title = "Countdown Removal";
            embed.Description = $"**{Name}**: Starts {TimestampTag.FromDateTime(CountdownConfig.Countdowns[Name], TimestampTagStyles.Relative)} ({TimestampTag.FromDateTime(CountdownConfig.Countdowns[Name], TimestampTagStyles.ShortDate)})";

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

            var embed = new EmbedBuilder()
            {
                Color = new Discord.Color(BotConfig.EmbedColorGroup.R, BotConfig.EmbedColorGroup.G, BotConfig.EmbedColorGroup.B),
                Author = new EmbedAuthorBuilder() { IconUrl = Context.Client.CurrentUser.GetAvatarUrl() },
                Footer = new EmbedFooterBuilder() { Text = $"{removeHashes.Count} expired Emblem offers" },
            };
            embed.Title = "Remove these offers?";
            embed.Description = result;

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

            string timestamp = offerToDelete.EndDate == null ? "This offer has no end." : $"End{(offerToDelete.EndDate > DateTime.Now ? "s" : "ed")} {TimestampTag.FromDateTime((DateTime)offerToDelete.EndDate, TimestampTagStyles.Relative)}.";

            var buttonBuilder = new ComponentBuilder()
                .WithButton("Yes", customId: $"removeYes", ButtonStyle.Success, row: 0)
                .WithButton("No", customId: $"removeNo", ButtonStyle.Danger, row: 0);

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
                var buttonBuilder = new ComponentBuilder()
                    .WithButton("Yes", customId: $"sendYes", ButtonStyle.Success, row: 0)
                    .WithButton("No", customId: $"sendNo", ButtonStyle.Danger, row: 0);
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
                    await SendToAllAnnounceChannels(eo.BuildEmbed());
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
            // Send reset embeds if applicable.
            if (DateTime.Today.DayOfWeek == DayOfWeek.Tuesday)
                await DataConfig.PostWeeklyResetUpdate(Context.Client);

            await DataConfig.PostDailyResetUpdate(Context.Client);

            // Send users their tracking if applicable.
            if (DateTime.Today.DayOfWeek == DayOfWeek.Tuesday)
                await CurrentRotations.CheckUsersWeeklyTracking(Context.Client);

            await CurrentRotations.CheckUsersDailyTracking(Context.Client);
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

            int old = ActiveConfig.MaximumLoggingUsers;
            ActiveConfig.MaximumLoggingUsers = NewMaxUserCount;
            ActiveConfig.UpdateActiveAFKUsersConfig();

            string s = ActiveConfig.ActiveAFKUsers.Count == 1 ? "'s" : "s'";
            string p = ActiveConfig.PriorityActiveAFKUsers.Count != 0 ? $" (+{ActiveConfig.PriorityActiveAFKUsers.Count})" : "";
            await Context.Client.SetActivityAsync(new Game($"{ActiveConfig.ActiveAFKUsers.Count}/{ActiveConfig.MaximumLoggingUsers}{p} User{s} XP", ActivityType.Watching));
            await RespondAsync($"Changed maximum XP Logging users to {NewMaxUserCount} (was {old}).");
        }

        [RequireBotStaff]
        [SlashCommand("metrics", "[BOT STAFF]: Get somewhat confidential information about the bot.")]
        public async Task Metrics()
        {
            var app = await Context.Client.GetApplicationInfoAsync();
            var embed = new EmbedBuilder()
            {
                Color = new Discord.Color(BotConfig.EmbedColorGroup.R, BotConfig.EmbedColorGroup.G, BotConfig.EmbedColorGroup.B),
                Author = new EmbedAuthorBuilder() { IconUrl = Context.Client.CurrentUser.GetAvatarUrl() },
                Footer = new EmbedFooterBuilder() { Text = $"Levante v{BotConfig.Version:0.00}" },
            };
            embed.Title = "Metrics";
            embed.ThumbnailUrl = app.IconUrl;

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
            if (!ManifestHelper.IsNewManifest())
            {
                await Context.Interaction.ModifyOriginalResponseAsync(message => message.Content = $"No new Manifest. Current Version: {ManifestHelper.DestinyManifestVersion}");
                return;
            }

            var oldVersion = ManifestHelper.DestinyManifestVersion;
            await Context.Interaction.ModifyOriginalResponseAsync(message => message.Content = $"New Manifest found! Updating the stuffs...");
            await Task.Run(() => ManifestHelper.LoadManifestDictionaries());
            await Context.Interaction.ModifyOriginalResponseAsync(message => message.Content = $"Updated! {oldVersion} -> {ManifestHelper.DestinyManifestVersion}");
            return;
        }

        public async Task SendToAllAnnounceChannels(EmbedBuilder embed)
        {
            List<ulong> guildsWithKeptChannel = new List<ulong>();
            List<ulong> keptChannels = new List<ulong>();
            foreach (var Link in DataConfig.AnnounceEmblemLinks.ToList())
            {
                var channel = Context.Client.GetChannel(Link.ChannelID) as SocketTextChannel;
                var guildChannel = Context.Client.GetChannel(Link.ChannelID) as SocketGuildChannel;
                try
                {
                    if (channel == null || guildChannel == null)
                    {
                        LogHelper.ConsoleLog($"[OFFERS] Could not find channel {Link.ChannelID}. Removing this element.");
                        DataConfig.DeleteEmblemChannel(Link.ChannelID);
                        continue;
                    }

                    if (!guildsWithKeptChannel.Contains(guildChannel.Guild.Id))
                    {
                        keptChannels.Add(Link.ChannelID);
                        guildsWithKeptChannel.Add(guildChannel.Guild.Id);
                    }

                    foreach (var chan in guildChannel.Guild.TextChannels)
                    {
                        if (DataConfig.IsExistingEmblemLinkedChannel(chan.Id) && Link.ChannelID != chan.Id && guildsWithKeptChannel.Contains(chan.Guild.Id) && !keptChannels.Contains(chan.Id))
                        {
                            LogHelper.ConsoleLog($"[OFFERS] Duplicate channel detected. Removing: {chan.Id}");
                            DataConfig.DeleteEmblemChannel(chan.Id);
                        }
                    }

                    if (Link.RoleID != 0)
                    {
                        var role = Context.Client.GetGuild(guildChannel.Guild.Id).GetRole(Link.RoleID);
                        await channel.SendMessageAsync($"{role.Mention}", false, embed.Build()).Result.CrosspostAsync();
                    }
                    else
                    {
                        // Crosspost/Publish for news channels the bot posts into.
                        await channel.SendMessageAsync("", false, embed.Build()).Result.CrosspostAsync();
                    }
                }
                catch
                {
                    LogHelper.ConsoleLog($"[OFFERS] Could not send to channel {guildChannel.Id} in guild {guildChannel.Guild.Id}.");
                }
            }
        }
    }
}
