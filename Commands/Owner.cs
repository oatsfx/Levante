using Discord;
using Discord.Commands;
using System;
using System.Threading.Tasks;
using Levante.Configs;
using Levante.Util;
using System.Net.Http;
using System.Linq;
using System.Text;
using Fergun.Interactive;
using Newtonsoft.Json;
using Discord.WebSocket;
using System.IO;
using Levante.Helpers;
using Levante.Rotations;

namespace Levante.Commands
{
    public class Owner : ModuleBase<SocketCommandContext>
    {
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
                    await ReplyAsync("Error with JSON file. Make sure all values are correct.");
                    return;
                }

                await ReplyAsync("This is what the embed looks like. Ready to send to all channels? Reply \"yes\" to confirm. Reply \"skip\" to skip this step.", false, newOffer.BuildEmbed().Build());

                var sendResponse = await Interactive.NextMessageAsync(x => x.Channel.Id == Context.Channel.Id && x.Author == Context.Message.Author, timeout: TimeSpan.FromSeconds(BotConfig.DurationToWaitForNextMessage));

                if (sendResponse.Value.ToString().Equals("yes"))
                {
                    await ReplyAsync("Sending...");
                    await SendToAllAnnounceChannels(newOffer.BuildEmbed());
                }
                else if (sendResponse.Value.ToString().Equals("skip"))
                {
                    await ReplyAsync("Skipped announcement!");
                }
                else
                {
                    await ReplyAsync("Cancelled operation.");
                }

                return;
            }
            else
            {
                await ReplyAsync("No file detected.");
                return;
            }
        }

        [Command("removeEmblemOffer", RunMode = RunMode.Async)]
        [Alias("deleteEmblemOffer", "removeOffer", "deleteOffer")]
        [Summary("Removes an offer from database.")]
        [RequireBotStaff]
        public async Task RemoveOfferAsync(long HashCode)
        {
            if (!EmblemOffer.HasExistingOffer(HashCode))
            {
                await ReplyAsync("No such offer exists.");
                return;
            }

            var offerToDelete = EmblemOffer.GetSpecificOffer(HashCode);

            EmblemOffer.DeleteOffer(offerToDelete);

            await ReplyAsync($"Removed offer.\n" +
                $"Hash Code: {HashCode}\n" +
                $"Emblem Name: {offerToDelete.OfferedEmblem.GetName()}");
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
                await ReplyAsync("This is what the embed looks like. Ready to send to all channels? Reply \"yes\" to confirm. Reply \"no\" to cancel.", false, eo.BuildEmbed().Build());
                var sendResponse = await Interactive.NextMessageAsync(x => x.Channel.Id == Context.Channel.Id, timeout: TimeSpan.FromSeconds(BotConfig.DurationToWaitForNextMessage));

                if (sendResponse.ToString().Equals("yes"))
                {
                    await ReplyAsync("Sending...");
                    await SendToAllAnnounceChannels(eo.BuildEmbed());
                }
                if (sendResponse.ToString().Equals("no"))
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

        [Command("restart")]
        [Summary("Restarts the program/bot.")]
        [RequireBotStaff]
        public async Task Restart()
        {
            await ReplyAsync($"I'll see you shortly.");
            System.Diagnostics.Process.Start(AppDomain.CurrentDomain.FriendlyName);
            Environment.Exit(0);
        }

        [Command("flushTracking")]
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

            ActiveConfig.MaximumThrallwayUsers = NewMaxUserCount;
            ActiveConfig.UpdateActiveAFKUsersConfig();

            string s = ActiveConfig.ActiveAFKUsers.Count == 1 ? "" : "s";
            await Context.Client.SetActivityAsync(new Game($"{ActiveConfig.ActiveAFKUsers.Count}/{ActiveConfig.MaximumThrallwayUsers} Thrallway Farmer{s}", ActivityType.Watching));
            await ReplyAsync($"Changed maximum Thrallway users to {NewMaxUserCount}.");
        }

        public async Task SendToAllAnnounceChannels(EmbedBuilder embed)
        {
            foreach (var Link in DataConfig.AnnounceEmblemLinks)
            {
                var channel = Context.Client.GetChannel(Link.ChannelID) as SocketTextChannel;
                var guildChannel = Context.Client.GetChannel(Link.ChannelID) as SocketGuildChannel;
                if (Link.RoleID != 0)
                {
                    var role = Context.Client.GetGuild(guildChannel.Guild.Id).GetRole(Link.RoleID);
                    await channel.SendMessageAsync($"{role.Mention}", false, embed.Build());
                }
                else
                {
                    await channel.SendMessageAsync("", false, embed.Build());
                }
            }
            await ReplyAsync("Sent!");
        }
    }
}
