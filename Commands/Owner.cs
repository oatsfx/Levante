using Discord;
using Discord.Commands;
using System;
using System.Threading.Tasks;
using Levante.Configs;
using Levante.Util;
using System.Net.Http;
using System.Linq;
using System.Text;
using APIHelper;
using APIHelper.Structs;
using Discord.WebSocket;
using Fergun.Interactive;
using Newtonsoft.Json;
using System.IO;
using Levante.Helpers;
using Levante.Rotations;
using System.Collections.Generic;

namespace Levante.Commands
{
    public class Owner : ModuleBase<SocketCommandContext>
    {
        // These commands will need to be ran through DMs when verified after April 2022.
        public InteractiveService Interactive { get; set; }

        [Command("force", RunMode = RunMode.Async)]
        [Summary("Sends a button to force a daily reset.")]
        [RequireOwner]
        public async Task Force()
        {
            Emoji helpEmote = new Emoji("❔");

            var buttonBuilder = new ComponentBuilder()
                .WithButton("Force Reset", customId: $"force", ButtonStyle.Secondary, helpEmote, row: 0);

            await ReplyAsync($"This shouldn't really be used...", components: buttonBuilder.Build());
        }

        [Command("activeLogging")]
        [Summary("Gets a list of the users that are using my XP logging feature.")]
        [RequireBotStaff]
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

            await Context.Message.ReplyAsync(embed: embed.Build());
        }

        [Command("giveConfig", RunMode = RunMode.Async)]
        [Alias("config", "getConfig")]
        [RequireOwner]
        public async Task GiveConfigs() => await Context.User.SendFileAsync(BotConfig.FilePath);

        [Command("replaceConfig", RunMode = RunMode.Async)]
        [RequireOwner]
        public async Task ReplaceConfig()
        {
            var attachments = Context.Message.Attachments;
            if (attachments.Count == 0)
            {
                await ReplyAsync("No file attached.");
                return;
            }

            using (var httpCilent = new HttpClient())
            {
                var url = attachments.First().Url;
                byte[] bytes = await httpCilent.GetByteArrayAsync(url);
                using (var fs = new FileStream(BotConfig.FilePath, FileMode.Create))
                {
                    fs.Write(bytes, 0, bytes.Length);
                }
            }

            await Refresh();
            await ReplyAsync($"Config replaced and bot is refreshed.");
        }

        [Command("refresh", RunMode = RunMode.Async)]
        [Summary("Refreshes activity.")]
        [RequireOwner]
        public async Task Refresh()
        {
            ConfigHelper.CheckAndLoadConfigFiles();
            await Context.Client.SetActivityAsync(new Game($"{BotConfig.Note} | v{BotConfig.Version}", ActivityType.Playing));

            var react = Emote.Parse("<:complete:927315594951426048>");

            await Context.Message.AddReactionAsync(react);
        }

        [Command("addSupporter", RunMode = RunMode.Async)]
        [Summary("Add a bot supporter.")]
        [RequireOwner]
        public async Task AddSupporter(ulong DiscordID = 0)
        {
            if (DiscordID <= 0)
            {
                await Context.Message.ReplyAsync($"No Discord User ID attached.");
                return;
            }

            if (BotConfig.BotSupportersDiscordIDs.Contains(DiscordID))
            {
                await Context.Message.ReplyAsync($"{DiscordID} already exists!");
                return;
            }

            BotConfig.BotSupportersDiscordIDs.Add(DiscordID);
            var bConfig = new BotConfig();
            File.WriteAllText(BotConfig.FilePath, JsonConvert.SerializeObject(bConfig, Formatting.Indented));
            await Context.Message.ReplyAsync($"Added {DiscordID} to my list of Supporters!");
        }

        [Command("deleteBotMessage", RunMode = RunMode.Async)]
        [RequireOwner]
        public async Task DeleteBotMessage(ulong MessageId)
        {
            var app = await Context.Client.GetApplicationInfoAsync();
            var msg = await Context.Channel.GetMessageAsync(MessageId);

            if (msg.Author.Id != app.Id)
            {
                await ReplyAsync($"Message does not belong to {Context.Client.GetUser(app.Id).Mention}.");
                return;
            }

            await msg.DeleteAsync();
        }

        [Command("newEmblemOffer", RunMode = RunMode.Async)]
        [Alias("makeOffer", "newOffer")]
        [RequireBotStaff]
        public async Task NewEmblemOffer()
        {
            var attachments = Context.Message.Attachments;
            if (attachments.Count > 0)
            {
                string content;
                using (var httpCilent = new HttpClient())
                {
                    var url = attachments.First().Url;
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
                    await Context.Message.ReplyAsync("Error with JSON file. Make sure all values are correct.");
                    return;
                }

                if (EmblemOffer.HasExistingOffer(newOffer.EmblemHashCode))
                {
                    await Context.Message.ReplyAsync("Emblem has an offer already. Delete it first.");
                    return;
                }

                var buttonBuilder = new ComponentBuilder()
                    .WithButton("Post Offer", customId: $"newPost", ButtonStyle.Success, row: 0)
                    .WithButton("Skip Posting", customId: $"newSkip", ButtonStyle.Secondary, row: 0)
                    .WithButton("Cancel", customId: $"newCancel", ButtonStyle.Danger, row: 0);

                await Context.Message.ReplyAsync("This is what the embed looks like. What would you like to do?", embed: newOffer.BuildEmbed().Build(), components: buttonBuilder.Build());

                var buttonResponse = await Interactive.NextMessageComponentAsync(x => x.Channel.Id == Context.Channel.Id && x.User.Id == Context.User.Id, timeout: TimeSpan.FromSeconds(BotConfig.DurationToWaitForNextMessage));

                if (buttonResponse == null)
                {
                    await Context.Message.ReplyAsync($"Closed command, invaild response.");
                    return;
                }

                if (buttonResponse.IsTimeout)
                {
                    await Context.Message.ReplyAsync($"Closed command, timed out.");
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
            else
            {
                await ReplyAsync("No file detected.");
                return;
            }
        }

        [Command("flushOffers", RunMode = RunMode.Async)]
        [Alias("removeOffers", "deleteOffers")]
        [Summary("Removes all Emblem offers where the end date has passed.")]
        [RequireBotStaff]
        public async Task RemoveOffersAsync()
        {
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
                await Context.Message.ReplyAsync($"No emblem offers are past their end dates.");
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

            await Context.Message.ReplyAsync(embed: embed.Build(), components: buttonBuilder.Build());

            var buttonResponse = await Interactive.NextMessageComponentAsync(x => x.Channel.Id == Context.Channel.Id && x.User.Id == Context.User.Id, timeout: TimeSpan.FromSeconds(BotConfig.DurationToWaitForNextMessage));

            if (buttonResponse == null)
            {
                await Context.Message.ReplyAsync($"Closed command, invaild response.");
                return;
            }

            if (buttonResponse.IsTimeout)
            {
                await Context.Message.ReplyAsync($"Closed command, timed out.");
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

        [Command("removeEmblemOffer", RunMode = RunMode.Async)]
        [Alias("deleteEmblemOffer", "removeOffer", "deleteOffer")]
        [Summary("Removes an offer from database.")]
        [RequireBotStaff]
        public async Task RemoveOfferAsync(long HashCode = -1)
        {
            if (HashCode == -1)
            {
                await RemoveOffersAsync();
                return;
            }

            if (!EmblemOffer.HasExistingOffer(HashCode))
            {
                await ReplyAsync("No such offer exists.");
                return;
            }

            var offerToDelete = EmblemOffer.GetSpecificOffer(HashCode);

            var embed = new EmbedBuilder()
            {
                Color = new Discord.Color(BotConfig.EmbedColorGroup.R, BotConfig.EmbedColorGroup.G, BotConfig.EmbedColorGroup.B),
                Author = new EmbedAuthorBuilder() { IconUrl = Context.Client.CurrentUser.GetAvatarUrl() },
                Footer = new EmbedFooterBuilder() { Text = $"Hash Code: {offerToDelete.EmblemHashCode}" },
            };
            string timestamp = offerToDelete.EndDate == null ? "This offer has no end." : $"End{(offerToDelete.EndDate > DateTime.Now ? "s" : "ed")} {TimestampTag.FromDateTime((DateTime)offerToDelete.EndDate, TimestampTagStyles.Relative)}.";
            embed.Title = "Remove this offer?";
            embed.Description = $"[{offerToDelete.OfferedEmblem.GetName()}]({offerToDelete.SpecialUrl}): {timestamp}";

            var buttonBuilder = new ComponentBuilder()
                .WithButton("Yes", customId: $"removeYes", ButtonStyle.Success, row: 0)
                .WithButton("No", customId: $"removeNo", ButtonStyle.Danger, row: 0);

            await Context.Message.ReplyAsync(embed: embed.Build(), components: buttonBuilder.Build());

            var buttonResponse = await Interactive.NextMessageComponentAsync(x => x.Channel.Id == Context.Channel.Id && x.User.Id == Context.User.Id, timeout: TimeSpan.FromSeconds(BotConfig.DurationToWaitForNextMessage));

            if (buttonResponse == null)
            {
                await Context.Message.ReplyAsync($"Closed command, invaild response.");
                return;
            }

            if (buttonResponse.IsTimeout)
            {
                await Context.Message.ReplyAsync($"Closed command, timed out.");
                return;
            }

            if (buttonResponse.Value.Data.CustomId.Equals("removeYes"))
            {
                EmblemOffer.DeleteOffer(offerToDelete);

                embed.Title = "Removed this offer";
                await buttonResponse.Value.Message.ModifyAsync(message => { message.Components = new ComponentBuilder().Build(); message.Embed = embed.Build(); });
                return;
            }
            else
            {
                embed.Title = "Did not remove this offer";
                await buttonResponse.Value.Message.ModifyAsync(message => { message.Components = new ComponentBuilder().Build(); message.Embed = embed.Build(); });
                return;
            }
        }

        [Command("sendOffer", RunMode = RunMode.Async)]
        [Summary("Sends a test offer to all channels with announcements set up.")]
        [RequireBotStaff]
        public async Task TestOffer(long EmblemHashCode = -1)
        {
            EmblemOffer.LoadCurrentOffers();

            if (EmblemHashCode == -1)
            {
                await ReplyAsync($"", false, EmblemOffer.GetOfferListEmbed().Build());
                return;
            }

            if (!EmblemOffer.HasExistingOffer(EmblemHashCode))
            {
                await ReplyAsync("Are you sure you entered the correct hash code?");
                return;
            }
            else
            {
                EmblemOffer eo = EmblemOffer.GetSpecificOffer(EmblemHashCode);
                await ReplyAsync("This is what the embed looks like. Ready to send to all channels? Reply \"yes\" to confirm. Reply with anything else to cancel.", false, eo.BuildEmbed().Build());
                var sendResponse = await Interactive.NextMessageAsync(x => x.Channel.Id == Context.Channel.Id && x.Author == Context.Message.Author, timeout: TimeSpan.FromSeconds(BotConfig.DurationToWaitForNextMessage));

                if (sendResponse.Value.ToString().Equals("yes"))
                {
                    await ReplyAsync("Sending...");
                    await SendToAllAnnounceChannels(eo.BuildEmbed());
                    await ReplyAsync("Sent!");
                }
                if (sendResponse.Value.ToString().Equals("no"))
                {
                    await ReplyAsync("Cancelled operation.");
                }
                else
                {
                    await ReplyAsync("Cancelled operation.");
                }
                return;
            }
        }

        [Command("restart", RunMode = RunMode.Async)]
        [Summary("Restarts the program/bot.")]
        [RequireBotStaff]
        public async Task Restart()
        {
            await ReplyAsync($"I'll see you shortly.");
            System.Diagnostics.Process.Start(AppDomain.CurrentDomain.FriendlyName);
            Environment.Exit(0);
        }

        [Command("flushTracking", RunMode = RunMode.Async)]
        [Summary("If the resets break, use this command to send out the tracking reminders.")]
        [RequireOwner]
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

        [Command("maxUsers", RunMode = RunMode.Async)]
        [RequireBotStaff]
        public async Task ChangeMaxUsers(int NewMaxUserCount)
        {
            if (NewMaxUserCount > 50)
            {
                await ReplyAsync($"That's too high.");
                return;
            }
            else if (NewMaxUserCount < 1)
            {
                await ReplyAsync($"That's too low.");
                return;
            }

            ActiveConfig.MaximumLoggingUsers = NewMaxUserCount;
            ActiveConfig.UpdateActiveAFKUsersConfig();

            string s = ActiveConfig.ActiveAFKUsers.Count == 1 ? "'s" : "s'";
            string p = ActiveConfig.PriorityActiveAFKUsers.Count != 0 ? $" (+{ActiveConfig.PriorityActiveAFKUsers.Count})" : "";
            await Context.Client.SetActivityAsync(new Game($"{ActiveConfig.ActiveAFKUsers.Count}/{ActiveConfig.MaximumLoggingUsers}{p} User{s} XP", ActivityType.Watching));
            await ReplyAsync($"Changed maximum XP Logging users to {NewMaxUserCount}.");
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
                        LogHelper.ConsoleLog($"Could not find channel {Link.ChannelID}. Removing this element.");
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
                            LogHelper.ConsoleLog($"Duplicate channel detected. Removing: {chan.Id}");
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
                    LogHelper.ConsoleLog($"Could not send to channel {guildChannel.Id} in guild {guildChannel.Guild.Id}.");
                }
            }
        }
    }
}
