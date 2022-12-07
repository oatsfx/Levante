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

namespace Levante.Commands
{
    public static class QuizInstance
    {
        public static List<QuizEntry> ActiveEmblemQuizzes = new();
    }

    [Group("quiz", "Test your knowledge on various Destiny 2 entities.")]
    public class Quiz : InteractionModuleBase<SocketInteractionContext>
    {
        public InteractiveService Interactive { get; set; }

        [ComponentInteraction("playEmblemQuizAgain:*", ignoreGroupNames: true)]
        public async Task PlayEmblemAgain(ulong DiscordID)
        {
            if (Context.Interaction.User.Id == DiscordID)
                await Emblem();
            else
            {
                var embed = Embeds.GetErrorEmbed();
                embed.Description = $"Insufficient Permissions. Only the user who ran the original command can use this button.\n" +
                    $"Use the `/quiz emblem` command to start a quiz yourself!";
                await RespondAsync(embed: embed.Build(), ephemeral: true);
            }
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
        public async Task Emblem()
        {
            await DeferAsync();

            if (QuizInstance.ActiveEmblemQuizzes.Any(x => x.ChannelId == Context.Interaction.Channel.Id))
            {
                var embed = Embeds.GetErrorEmbed();
                embed.Description = $"There is an active quiz in this channel! Wait for the quiz to end before starting a new one!";
                await RespondAsync(embed: embed.Build(), ephemeral: true);
                return;
            }

            var quiz = new QuizEntry(Context);
            Log.Information("Quiz added: {Id}", quiz.MessageId);
            QuizInstance.ActiveEmblemQuizzes.Add(quiz);

            // Wait to end the quiz. I'm sure there is a better way to handle this.
            await Task.Delay(BotConfig.DurationToWaitForNextMessage * 1000);

            await quiz.HandleEnd(Context);
            QuizInstance.ActiveEmblemQuizzes.Remove(quiz);
        }
    }

    public class QuizEntry
    {
        public ulong MessageId;
        public ulong ChannelId;
        public Dictionary<ulong, long> VotedUsers = new();
        //public DateTime Timeout;
        public int Answer;
        public Emblem _Emblem;

        public EmbedBuilder Embed;
        public ComponentBuilder Buttons;
        public RestFollowupMessage Message;

        public List<OptionEntry> Options = new();

        public QuizEntry(SocketInteractionContext<SocketInteraction> context)
        {
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

            Answer = rand.Next(0, 4);
            _Emblem = new Emblem(Options[Answer].Hash);
            var auth = new EmbedAuthorBuilder()
            {
                Name = $"Test your Emblem Knowledge!",
                IconUrl = _Emblem.GetIconUrl()
            };
            var foot = new EmbedFooterBuilder()
            {
                Text = $"Powered by the Bungie API and Levante v{BotConfig.Version}"
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
            for (int i = 0; i < 5; i++)
            {
                builder += $"{ManifestHelper.Emblems[Options[i].Hash]}: **{Options[i].Votes}**\n";
            }

            Embed.AddField(x =>
            {
                x.Name = "Votes";
                x.Value = $"{builder}";
                x.IsInline = true;
            });

            Message = context.Interaction.FollowupAsync(embed: Embed.Build(), components: Buttons.Build()).Result;
            MessageId = Message.Id;
            ChannelId = context.Channel.Id;
        }

        public async Task HandleVote(SocketInteractionContext<SocketInteraction> context, long Hash)
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
                VotedUsers.Add(context.Interaction.User.Id, Options[0].Hash);
                //Log.Debug($"{context.Interaction.User.Username} voted for {ManifestHelper.Emblems[Options[0].Hash]}.");
            }
            else if (Hash == Options[1].Hash)
            {
                Options[1].Votes++;
                await context.Interaction.FollowupAsync($"You voted for {ManifestHelper.Emblems[Options[1].Hash]}!", ephemeral: true).ConfigureAwait(false);
                VotedUsers.Add(context.Interaction.User.Id, Options[1].Hash);
                //Log.Debug($"{context.Interaction.User.Username} voted for {ManifestHelper.Emblems[Options[1].Hash]}.");
            }
            else if (Hash == Options[2].Hash)
            {
                Options[2].Votes++;
                await context.Interaction.FollowupAsync($"You voted for {ManifestHelper.Emblems[Options[2].Hash]}!", ephemeral: true).ConfigureAwait(false);
                VotedUsers.Add(context.Interaction.User.Id, Options[2].Hash);
                //Log.Debug($"{context.Interaction.User.Username} voted for {ManifestHelper.Emblems[Options[2].Hash]}.");
            }
            else if (Hash == Options[3].Hash)
            {
                Options[3].Votes++;
                await context.Interaction.FollowupAsync($"You voted for {ManifestHelper.Emblems[Options[3].Hash]}!", ephemeral: true).ConfigureAwait(false);
                VotedUsers.Add(context.Interaction.User.Id, Options[3].Hash);
                //Log.Debug($"{context.Interaction.User.Username} voted for {ManifestHelper.Emblems[Options[3].Hash]}.");
            }
            else if (Hash == Options[4].Hash)
            {
                Options[4].Votes++;
                await context.Interaction.FollowupAsync($"You voted for {ManifestHelper.Emblems[Options[4].Hash]}!", ephemeral: true).ConfigureAwait(false);
                VotedUsers.Add(context.Interaction.User.Id, Options[4].Hash);
                //Log.Debug($"{context.Interaction.User.Username} voted for {ManifestHelper.Emblems[Options[4].Hash]}.");
            }

            var sorted = Options.OrderBy(x => x.Votes).Reverse().ToList();
            string builder = "";
            for (int i = 0; i < 5; i++)
            {
                builder += $"{ManifestHelper.Emblems[sorted[i].Hash]}: **{sorted[i].Votes}**\n";
            }
            Embed.Fields.Clear();
            Embed.AddField(x =>
            {
                x.Name = "Votes";
                x.Value = $"{builder}";
                x.IsInline = true;
            });
            await context.Interaction.ModifyOriginalResponseAsync(message => { message.Embed = Embed.Build(); });
        }

        public async Task HandleEnd(SocketInteractionContext<SocketInteraction> context)
        {
            Embed.Description = $"This emblem is **[{ManifestHelper.Emblems[Options[Answer].Hash]}]({_Emblem.GetDECUrl()})**!";
            var topWinners = VotedUsers.Where(x => x.Value == Options[Answer].Hash).Take(5);
            if (topWinners.Any())
            {
                string winBuild = "";
                foreach (var winner in topWinners)
                    winBuild += $"<@{winner.Key}>\n";
                Embed.AddField(x =>
                {
                    x.Name = $"Top {topWinners.Count()}";
                    x.Value = winBuild;
                    x.IsInline = true;
                });
            }
            var playAgainBuilder = new ComponentBuilder()
                .WithButton($"Play Again", customId: $"playEmblemQuizAgain:{context.User.Id}", ButtonStyle.Success, row: 0);
            await Message.ModifyAsync(message => { message.Components = playAgainBuilder.Build(); message.Embed = Embed.Build(); });
        }
    }

    public class OptionEntry
    {
        public long Hash;
        public long Votes;
    }
}
