using Discord;
using Discord.Interactions;
using Levante.Configs;
using Levante.Helpers;
using Levante.Util.Attributes;
using Levante.Util;
using Microsoft.VisualBasic;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fergun.Interactive;
using Discord.WebSocket;
using Discord.Rest;
using System.Data;
using Levante.Rotations;
using APIHelper.Structs;
using APIHelper;
using System.Reactive;
using Newtonsoft.Json;
using System.Threading;

namespace Levante.Commands
{
    public static class QuizInstance
    {
        public static List<EmblemQuiz> ActiveEmblemQuizzes = new();
        public static List<WeaponPerkQuiz> ActiveWeaponPerkQuizzes = new();
    }

    // TODO: Let users determine quiz timer.
    [Group("quiz", "Test your knowledge on various Destiny 2 entities.")]
    public class Quiz : InteractionModuleBase<ShardedInteractionContext>
    {
        public InteractiveService Interactive { get; set; }

        [ComponentInteraction("playEmblemQuizAgain:*:*", ignoreGroupNames: true)]
        public async Task PlayEmblemQuizAgain(ulong DiscordID, bool hideVotes)
        {
            if (Context.Interaction.User.Id == DiscordID)
                await Emblem(hideVotes);
            else
            {
                var embed = Embeds.GetErrorEmbed();
                embed.Description = $"Insufficient Permissions. Only the user who ran the original command can use this button.\n" +
                    $"Use the `/quiz emblem` command to start a quiz yourself!";
                await RespondAsync(embed: embed.Build(), ephemeral: true);
            }
        }

        [ComponentInteraction("endEmblemQuiz:*", ignoreGroupNames: true)]
        public async Task EndEmblemQuizEarly(ulong DiscordID)
        {
            if (Context.Interaction.User.Id != DiscordID)
            {
                var embed = Embeds.GetErrorEmbed();
                embed.Description = $"Insufficient Permissions. You can't end a quiz you didn't start, only the user who started the quiz can end it.\n" +
                    $"Use the `/quiz emblem` command to start a quiz yourself!";
                await RespondAsync(embed: embed.Build(), ephemeral: true);
            }

            await DeferAsync();
            var quiz = QuizInstance.ActiveEmblemQuizzes.FirstOrDefault(x => x.MessageId == Context.Interaction.GetOriginalResponseAsync().Result.Id);
            if (quiz == null)
            {
                Log.Warning("Quiz not found. {Id}", Context.Interaction.GetOriginalResponseAsync().Result.Id);
                return;
            }
            await quiz.HandleEnd(Context);
            QuizInstance.ActiveEmblemQuizzes.Remove(quiz);
        }

        [ComponentInteraction("quizEmblem:*", ignoreGroupNames: true)]
        public async Task EmblemQuiz(long Hash)
        {
            await DeferAsync();
            var quiz = QuizInstance.ActiveEmblemQuizzes.FirstOrDefault(x => x.MessageId == Context.Interaction.GetOriginalResponseAsync().Result.Id);
            if (quiz == null)
            {
                Log.Warning("Quiz not found. {Id}", Context.Interaction.GetOriginalResponseAsync().Result.Id);
                return;
            }
            await quiz.HandleVote(Context, Hash).ConfigureAwait(false);
        }

        [SlashCommand("emblem", "Think you know all the emblems? Test your emblem knowledge.")]
        public async Task Emblem([Summary("hide-votes", "Hide votes toward specific answers. This is off by default.")] bool hideVotes = false)
        {
            if (HasActiveQuiz(Context.Interaction.Channel.Id))
            {
                var embed = Embeds.GetErrorEmbed();
                embed.Description = $"There is an active quiz in this channel! Wait for the quiz to end before starting a new one!";
                await RespondAsync(embed: embed.Build(), ephemeral: true);
                return;
            }

            await DeferAsync();

            var quiz = new EmblemQuiz(Context, hideVotes);
            Log.Information("Emblem Quiz added: {Id}", quiz.MessageId);
            QuizInstance.ActiveEmblemQuizzes.Add(quiz);

            // Wait to end the quiz. I'm sure there is a better way to handle this.
            await Task.Delay(BotConfig.DurationToWaitForNextMessage * 1000);

            // Find the quiz (just in case it was ended early).
            //quiz = QuizInstance.ActiveEmblemQuizzes.FirstOrDefault(x => x.MessageId == Context.Interaction.GetOriginalResponseAsync().Result.Id);
            if (!quiz.cancelToken.IsCancellationRequested)
            {
                await quiz.HandleEnd(Context);
                QuizInstance.ActiveEmblemQuizzes.Remove(quiz);
            }
        }

        // ==============

        [ComponentInteraction("playWeaponPerkQuizAgain:*:*", ignoreGroupNames: true)]
        public async Task PlayWeaponPerkQuizAgain(ulong DiscordID, bool hideVotes)
        {
            if (Context.Interaction.User.Id == DiscordID)
                await WeaponPerk(hideVotes);
            else
            {
                var embed = Embeds.GetErrorEmbed();
                embed.Description = $"Insufficient Permissions. Only the user who ran the original command can use this button.\n" +
                    $"Use the `/quiz perk` command to start a quiz yourself!";
                await RespondAsync(embed: embed.Build(), ephemeral: true);
            }
        }

        [ComponentInteraction("endWeaponPerkQuiz:*", ignoreGroupNames: true)]
        public async Task EndWeaponPerkQuizEarly(ulong DiscordID)
        {
            if (Context.Interaction.User.Id != DiscordID)
            {
                var embed = Embeds.GetErrorEmbed();
                embed.Description = $"Insufficient Permissions. You can't end a quiz you didn't start, only the user who started the quiz can end it.\n" +
                    $"Use the `/quiz perk` command to start a quiz yourself!";
                await RespondAsync(embed: embed.Build(), ephemeral: true);
            }

            await DeferAsync();
            var quiz = QuizInstance.ActiveWeaponPerkQuizzes.FirstOrDefault(x => x.MessageId == Context.Interaction.GetOriginalResponseAsync().Result.Id);
            if (quiz == null)
            {
                Log.Warning("Quiz not found. {Id}", Context.Interaction.GetOriginalResponseAsync().Result.Id);
                return;
            }
            await quiz.HandleEnd(Context);
            QuizInstance.ActiveWeaponPerkQuizzes.Remove(quiz);
        }

        [ComponentInteraction("quizWeaponPerk:*", ignoreGroupNames: true)]
        public async Task WeaponPerkQuiz(long Hash)
        {
            await DeferAsync();
            var quiz = QuizInstance.ActiveWeaponPerkQuizzes.FirstOrDefault(x => x.MessageId == Context.Interaction.GetOriginalResponseAsync().Result.Id);
            if (quiz == null)
            {
                Log.Warning("Quiz not found. {Id}", Context.Interaction.GetOriginalResponseAsync().Result.Id);
                return;
            }
            await quiz.HandleVote(Context, Hash).ConfigureAwait(false);
        }

        [SlashCommand("perk", "Think you know all the weapon perks? Test your weapon perk knowledge.")]
        public async Task WeaponPerk([Summary("hide-votes", "Hide votes toward specific answers. This is off by default.")] bool hideVotes = false)
        {
            if (HasActiveQuiz(Context.Interaction.Channel.Id))
            {
                var embed = Embeds.GetErrorEmbed();
                embed.Description = $"There is an active quiz in this channel! Wait for the quiz to end before starting a new one!";
                await RespondAsync(embed: embed.Build(), ephemeral: true);
                return;
            }

            await DeferAsync();

            var quiz = new WeaponPerkQuiz(Context, hideVotes);
            Log.Information("Weapon Perk Quiz added: {Id}", quiz.MessageId);
            QuizInstance.ActiveWeaponPerkQuizzes.Add(quiz);

            // Wait to end the quiz. I'm sure there is a better way to handle this.
            await Task.Delay(BotConfig.DurationToWaitForNextMessage * 1000);

            // Find the quiz (just in case it was ended early).
            //quiz = QuizInstance.ActiveWeaponPerkQuizzes.FirstOrDefault(x => x.MessageId == Context.Interaction.GetOriginalResponseAsync().Result.Id);
            if (!quiz.cancelToken.IsCancellationRequested)
            {
                await quiz.HandleEnd(Context);
                QuizInstance.ActiveWeaponPerkQuizzes.Remove(quiz);
            } 
        }

        private bool HasActiveQuiz(ulong ChannelID) =>
            QuizInstance.ActiveEmblemQuizzes.Any(x => x.ChannelId == ChannelID) ||
            QuizInstance.ActiveWeaponPerkQuizzes.Any(x => x.ChannelId == ChannelID);
    }

    public abstract class QuizSkeleton
    {
        public ulong MessageId;
        public ulong ChannelId;
        // <Discord ID, <Option #, Time Elapsed>>
        public Dictionary<ulong, KeyValuePair<int, double>> VotedUsers = new();
        //public DateTime Timeout;
        public int Answer;
        public DateTime TimeStarted;

        public bool HideVotes;

        public EmbedBuilder Embed;
        public ComponentBuilder Buttons;
        public RestFollowupMessage Message;

        // To prevent the embed from being updated when a quiz ends.
        public CancellationTokenSource cancelToken = new();

        public List<OptionEntry> Options = new();

        public abstract Task HandleVote(SocketInteractionContext<SocketInteraction> context, long Hash);

        public abstract Task HandleEnd(SocketInteractionContext<SocketInteraction> context);
    }

    public class EmblemQuiz : QuizSkeleton
    {
        public Emblem _Emblem;

        public EmblemQuiz(SocketInteractionContext<SocketInteraction> context, bool hideVotes)
        {
            HideVotes = hideVotes;

            var rand = new Random();
            Buttons = new ComponentBuilder();

            while (Options.Count() < 5)
            {
                int val = rand.Next(0, ManifestHelper.Emblems.Count);
                if (!Options.Exists(x => x.Hash == ManifestHelper.Emblems.ElementAt(val).Key))
                {
                    Options.Add(new OptionEntry { Hash = ManifestHelper.Emblems.ElementAt(val).Key, Votes = 0 });
                    Buttons.WithButton($"{ManifestHelper.Emblems.ElementAt(val).Value}", customId: $"quizEmblem:{ManifestHelper.Emblems.ElementAt(val).Key}", ButtonStyle.Secondary, row: 0);
                }
            }
            Buttons.WithButton($"End Quiz", customId: $"endEmblemQuiz:{context.User.Id}", ButtonStyle.Danger, row: 1);

            Answer = rand.Next(0, 4);
            _Emblem = new Emblem(Options[Answer].Hash);
            var auth = new EmbedAuthorBuilder()
            {
                Name = $"Test your Emblem Knowledge!",
                IconUrl = _Emblem.GetIconUrl()
            };
            var foot = new EmbedFooterBuilder()
            {
                Text = $"Powered by the Bungie API and {BotConfig.AppName} v{BotConfig.Version}"
            };
            Embed = new EmbedBuilder()
            {
                Author = auth,
                Footer = foot
            };
            int[] emblemRGB = _Emblem.GetRGBAsIntArray();
            Embed.WithColor(emblemRGB[0], emblemRGB[1], emblemRGB[2]);

            Embed.Description = $"What Emblem is this? Answer using the buttons below! Guessing ends {TimestampTag.FromDateTime(DateTime.Now + TimeSpan.FromSeconds(BotConfig.DurationToWaitForNextMessage), TimestampTagStyles.Relative)}.";
            Embed.ImageUrl = _Emblem.GetBackgroundUrl();
            Embed.ThumbnailUrl = _Emblem.GetIconUrl();

            string builder = "";
            if (!HideVotes)
                for (int i = 0; i < 5; i++)
                    builder += $"{ManifestHelper.Emblems[Options[i].Hash]}: **{Options[i].Votes}**\n";
            else
                builder = $"**{Options.Sum(x => x.Votes)}**";

            Embed.Fields.Clear();
            Embed.AddField(x =>
            {
                x.Name = "Votes";
                x.Value = $"{builder}";
                x.IsInline = true;
            });

            Message = context.Interaction.FollowupAsync(embed: Embed.Build(), components: Buttons.Build()).Result;
            MessageId = Message.Id;
            ChannelId = context.Channel.Id;
            TimeStarted = DateTime.Now;
        }

        public override async Task HandleVote(SocketInteractionContext<SocketInteraction> context, long Hash)
        {
            if (VotedUsers.ContainsKey(context.User.Id))
            {
                await context.Interaction.FollowupAsync($"You've already voted!", ephemeral: true).ConfigureAwait(false);
                return;
            }

            if (Hash == Options[0].Hash)
            {
                Options[0].Votes++;
                await context.Interaction.FollowupAsync($"You voted for {ManifestHelper.Emblems[Options[0].Hash]}!", ephemeral: true).ConfigureAwait(false);
                VotedUsers.Add(context.Interaction.User.Id, new(0, (DateTime.Now - TimeStarted).TotalSeconds));
                //Log.Debug($"{context.Interaction.User.Username} voted for {ManifestHelper.Emblems[Options[0].Hash]}.");
            }
            else if (Hash == Options[1].Hash)
            {
                Options[1].Votes++;
                await context.Interaction.FollowupAsync($"You voted for {ManifestHelper.Emblems[Options[1].Hash]}!", ephemeral: true).ConfigureAwait(false);
                VotedUsers.Add(context.Interaction.User.Id, new(1, (DateTime.Now - TimeStarted).TotalSeconds));
                //Log.Debug($"{context.Interaction.User.Username} voted for {ManifestHelper.Emblems[Options[1].Hash]}.");
            }
            else if (Hash == Options[2].Hash)
            {
                Options[2].Votes++;
                await context.Interaction.FollowupAsync($"You voted for {ManifestHelper.Emblems[Options[2].Hash]}!", ephemeral: true).ConfigureAwait(false);
                VotedUsers.Add(context.Interaction.User.Id, new(2, (DateTime.Now - TimeStarted).TotalSeconds));
                //Log.Debug($"{context.Interaction.User.Username} voted for {ManifestHelper.Emblems[Options[2].Hash]}.");
            }
            else if (Hash == Options[3].Hash)
            {
                Options[3].Votes++;
                await context.Interaction.FollowupAsync($"You voted for {ManifestHelper.Emblems[Options[3].Hash]}!", ephemeral: true).ConfigureAwait(false);
                VotedUsers.Add(context.Interaction.User.Id, new(3, (DateTime.Now - TimeStarted).TotalSeconds));
                //Log.Debug($"{context.Interaction.User.Username} voted for {ManifestHelper.Emblems[Options[3].Hash]}.");
            }
            else if (Hash == Options[4].Hash)
            {
                Options[4].Votes++;
                await context.Interaction.FollowupAsync($"You voted for {ManifestHelper.Emblems[Options[4].Hash]}!", ephemeral: true).ConfigureAwait(false);
                VotedUsers.Add(context.Interaction.User.Id, new(4, (DateTime.Now - TimeStarted).TotalSeconds));
                //Log.Debug($"{context.Interaction.User.Username} voted for {ManifestHelper.Emblems[Options[4].Hash]}.");
            }

            var sorted = Options.OrderBy(x => x.Votes).Reverse().ToList();
            string builder = "";
            if (!HideVotes)
                for (int i = 0; i < 5; i++)
                    builder += $"{ManifestHelper.Emblems[sorted[i].Hash]}: **{sorted[i].Votes}**\n";
            else
                builder = $"**{Options.Sum(x => x.Votes)}**";

            Embed.Fields.Clear();
            Embed.AddField(x =>
            {
                x.Name = "Votes";
                x.Value = $"{builder}";
                x.IsInline = true;
            });

            if (!cancelToken.IsCancellationRequested)
                await context.Interaction.ModifyOriginalResponseAsync(message => { message.Embed = Embed.Build(); });
        }

        public override async Task HandleEnd(SocketInteractionContext<SocketInteraction> context)
        {
            cancelToken.Cancel();
            Embed.Description = $"This emblem is **[{ManifestHelper.Emblems[Options[Answer].Hash]}]({_Emblem.GetDECUrl()})**!";

            var sorted = Options.OrderBy(x => x.Votes).Reverse().ToList();
            string builder = "";
            for (int i = 0; i < 5; i++)
                builder += $"{ManifestHelper.Emblems[sorted[i].Hash]}: **{sorted[i].Votes}**\n";

            Embed.Fields.Clear();
            Embed.AddField(x =>
            {
                x.Name = "Votes";
                x.Value = $"{builder}";
                x.IsInline = true;
            });

            var topWinners = VotedUsers.Where(x => x.Value.Key == Answer).Take(5);
            if (topWinners.Any())
            {
                string winBuild = "";
                foreach (var winner in topWinners)
                    winBuild += $"- <@{winner.Key}> ({winner.Value.Value:0.00}s)\n";
                Embed.AddField(x =>
                {
                    x.Name = $"Top {topWinners.Count()}";
                    x.Value = winBuild;
                    x.IsInline = true;
                });
            }
            var playAgainBuilder = new ComponentBuilder()
                .WithButton($"Play Again", customId: $"playEmblemQuizAgain:{context.User.Id}:{HideVotes}", ButtonStyle.Success, row: 0);
            await Message.ModifyAsync(message => { message.Components = playAgainBuilder.Build(); message.Embed = Embed.Build(); });
        }
    }

    public class WeaponPerkQuiz : QuizSkeleton
    {
        public WeaponPerk _Perk;

        public WeaponPerkQuiz(SocketInteractionContext<SocketInteraction> context, bool hideVotes)
        {
            HideVotes = hideVotes;

            var rand = new Random();
            Buttons = new ComponentBuilder();

            //string json = File.ReadAllText(EmoteConfig.FilePath);
            //var emoteCfg = JsonConvert.DeserializeObject<EmoteConfig>(json);

            while (Options.Count() < 5)
            {
                int val = rand.Next(0, ManifestHelper.Perks.Count);
                if (!Options.Exists(x => x.Hash == ManifestHelper.Perks.ElementAt(val).Key))
                {
                    Options.Add(new OptionEntry { Hash = ManifestHelper.Perks.ElementAt(val).Key, Votes = 0 });
                    // If wanted to not show the perk image, you could have the perk image in a button.
                    //var emote = Emote.Parse(emoteCfg.Emotes[_Perk.GetName().Replace(" ", "").Replace("-", "").Replace("'", "")]);
                    Buttons.WithButton($"{ManifestHelper.Perks.ElementAt(val).Value}", customId: $"quizWeaponPerk:{ManifestHelper.Perks.ElementAt(val).Key}", ButtonStyle.Secondary, row: 0/*, emote: emote*/);
                }
            }
            Buttons.WithButton($"End Quiz", customId: $"endWeaponPerkQuiz:{context.User.Id}", ButtonStyle.Danger, row: 1);

            Answer = rand.Next(0, 4);
            _Perk = new WeaponPerk(Options[Answer].Hash);
            var auth = new EmbedAuthorBuilder()
            {
                Name = $"Test your Weapon Perk Knowledge!",
                IconUrl = _Perk.GetIconUrl()
            };
            var foot = new EmbedFooterBuilder()
            {
                Text = $"Powered by the Bungie API and {BotConfig.AppName} v{BotConfig.Version}"
            };
            Embed = new EmbedBuilder()
            {
                Author = auth,
                Footer = foot
            };
            Embed.WithColor(254, 254, 254);

            Embed.Description = $"{_Perk.GetDescription()}\n\nWhat Weapon Perk is this? Answer using the buttons below! Guessing ends {TimestampTag.FromDateTime(DateTime.Now + TimeSpan.FromSeconds(BotConfig.DurationToWaitForNextMessage), TimestampTagStyles.Relative)}.";
            Embed.ThumbnailUrl = _Perk.GetIconUrl();

            string builder = "";
            if (!HideVotes)
                for (int i = 0; i < 5; i++)
                    builder += $"{ManifestHelper.Perks[Options[i].Hash]}: **{Options[i].Votes}**\n";
            else
                builder = $"**{Options.Sum(x => x.Votes)}**";

            Embed.Fields.Clear();
            Embed.AddField(x =>
            {
                x.Name = "Votes";
                x.Value = $"{builder}";
                x.IsInline = true;
            });

            Message = context.Interaction.FollowupAsync(embed: Embed.Build(), components: Buttons.Build()).Result;
            MessageId = Message.Id;
            ChannelId = context.Channel.Id;
            TimeStarted = DateTime.Now;
        }

        public override async Task HandleVote(SocketInteractionContext<SocketInteraction> context, long Hash)
        {
            if (VotedUsers.ContainsKey(context.User.Id))
            {
                await context.Interaction.FollowupAsync($"You've already voted!", ephemeral: true).ConfigureAwait(false);
                return;
            }

            if (Hash == Options[0].Hash)
            {
                Options[0].Votes++;
                await context.Interaction.FollowupAsync($"You voted for {ManifestHelper.Perks[Options[0].Hash]}!", ephemeral: true).ConfigureAwait(false);
                VotedUsers.Add(context.Interaction.User.Id, new(0, (DateTime.Now - TimeStarted).TotalSeconds));
                //Log.Debug($"{context.Interaction.User.Username} voted for {ManifestHelper.Emblems[Options[0].Hash]}.");
            }
            else if (Hash == Options[1].Hash)
            {
                Options[1].Votes++;
                await context.Interaction.FollowupAsync($"You voted for {ManifestHelper.Perks[Options[1].Hash]}!", ephemeral: true).ConfigureAwait(false);
                VotedUsers.Add(context.Interaction.User.Id, new(1, (DateTime.Now - TimeStarted).TotalSeconds));
                //Log.Debug($"{context.Interaction.User.Username} voted for {ManifestHelper.Emblems[Options[1].Hash]}.");
            }
            else if (Hash == Options[2].Hash)
            {
                Options[2].Votes++;
                await context.Interaction.FollowupAsync($"You voted for {ManifestHelper.Perks[Options[2].Hash]}!", ephemeral: true).ConfigureAwait(false);
                VotedUsers.Add(context.Interaction.User.Id, new(2, (DateTime.Now - TimeStarted).TotalSeconds));
                //Log.Debug($"{context.Interaction.User.Username} voted for {ManifestHelper.Emblems[Options[2].Hash]}.");
            }
            else if (Hash == Options[3].Hash)
            {
                Options[3].Votes++;
                await context.Interaction.FollowupAsync($"You voted for {ManifestHelper.Perks[Options[3].Hash]}!", ephemeral: true).ConfigureAwait(false);
                VotedUsers.Add(context.Interaction.User.Id, new(3, (DateTime.Now - TimeStarted).TotalSeconds));
                //Log.Debug($"{context.Interaction.User.Username} voted for {ManifestHelper.Emblems[Options[3].Hash]}.");
            }
            else if (Hash == Options[4].Hash)
            {
                Options[4].Votes++;
                await context.Interaction.FollowupAsync($"You voted for {ManifestHelper.Perks[Options[4].Hash]}!", ephemeral: true).ConfigureAwait(false);
                VotedUsers.Add(context.Interaction.User.Id, new(4, (DateTime.Now - TimeStarted).TotalSeconds));
                //Log.Debug($"{context.Interaction.User.Username} voted for {ManifestHelper.Emblems[Options[4].Hash]}.");
            }

            var sorted = Options.OrderBy(x => x.Votes).Reverse().ToList();
            string builder = "";
            if (!HideVotes)
                for (int i = 0; i < 5; i++)
                    builder += $"{ManifestHelper.Perks[sorted[i].Hash]}: **{sorted[i].Votes}**\n";
            else
                builder = $"**{Options.Sum(x => x.Votes)}**";

            Embed.Fields.Clear();
            Embed.AddField(x =>
            {
                x.Name = "Votes";
                x.Value = $"{builder}";
                x.IsInline = true;
            });

            if (!cancelToken.IsCancellationRequested)
                await context.Interaction.ModifyOriginalResponseAsync(message => { message.Embed = Embed.Build(); });
        }

        public override async Task HandleEnd(SocketInteractionContext<SocketInteraction> context)
        {
            cancelToken.Cancel();
            Embed.Description = $"{_Perk.GetDescription()}\n\nThis weapon perk is **{ManifestHelper.Perks[Options[Answer].Hash]}**!";

            var sorted = Options.OrderBy(x => x.Votes).Reverse().ToList();
            string builder = "";
            for (int i = 0; i < 5; i++)
                builder += $"{ManifestHelper.Perks[sorted[i].Hash]}: **{sorted[i].Votes}**\n";

            Embed.Fields.Clear();
            Embed.AddField(x =>
            {
                x.Name = "Votes";
                x.Value = $"{builder}";
                x.IsInline = true;
            });

            var topWinners = VotedUsers.Where(x => x.Value.Key == Answer).Take(5);
            if (topWinners.Any())
            {
                string winBuild = "";
                foreach (var winner in topWinners)
                    winBuild += $"- <@{winner.Key}> ({winner.Value.Value:0.00}s)\n";
                Embed.AddField(x =>
                {
                    x.Name = $"Top {topWinners.Count()}";
                    x.Value = winBuild;
                    x.IsInline = true;
                });
            }
            var playAgainBuilder = new ComponentBuilder()
                .WithButton($"Play Again", customId: $"playWeaponPerkQuizAgain:{context.User.Id}:{HideVotes}", ButtonStyle.Success, row: 0);
            await Message.ModifyAsync(message => { message.Components = playAgainBuilder.Build(); message.Embed = Embed.Build(); });
        }
    }

    public class OptionEntry
    {
        public long Hash;
        public long Votes;
    }
}
