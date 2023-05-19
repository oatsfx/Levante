using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Rest;
using Discord.WebSocket;
using Levante.Configs;
using Levante.Rotations.Interfaces;

namespace Levante.Rotations.Abstracts
{
    public abstract class Rotation<TTracker> where TTracker : IRotationTracker
    {
        protected string FilePath;
        public bool IsDaily { get; protected set; }

        public List<TTracker> Trackers = new();

        public void GetTrackerJSON()
        {
            if (!Directory.Exists("Trackers"))
                Directory.CreateDirectory("Trackers");

            if (File.Exists(FilePath))
            {
                string json = File.ReadAllText(FilePath);
                Trackers = JsonConvert.DeserializeObject<List<TTracker>>(json);
            }
            else
            {
                File.WriteAllText(FilePath, JsonConvert.SerializeObject(Trackers, Formatting.Indented));
                Log.Warning("No {FilePath} file detected; it has been created for you. No action is needed.", FilePath);
            }
        }

        public void AddUserTracking(TTracker Tracker)
        {
            Trackers.Add(Tracker);
            UpdateJSON();
        }

        public void RemoveUserTracking(ulong DiscordID)
        {
            Trackers.Remove(GetUserTracking(DiscordID));
            UpdateJSON();
        }

        public TTracker GetUserTracking(ulong DiscordID) => Trackers.FirstOrDefault(x => x.DiscordID == DiscordID);

        public void UpdateJSON()
        {
            string output = JsonConvert.SerializeObject(Trackers, Formatting.Indented);
            File.WriteAllText(FilePath, output);
        }

        public int GetLinkCount() => Trackers.Count;

        public abstract bool IsTrackerInRotation(TTracker Tracker);

        public EmbedBuilder GenerateTrackerAlertEmbed(TTracker Tracker)
        {
            return new EmbedBuilder()
                .WithAuthor(x =>
                {
                    x.Name = "Rotation Tracking Alert";
                    x.IconUrl = BotConfig.BotAvatarUrl;
                })
                .WithColor(BotConfig.EmbedColor.R, BotConfig.EmbedColor.G, BotConfig.EmbedColor.B)
                .WithTitle(ToString())
                .WithDescription("Hey! A requested rotation is available right now. Good luck!")
                .WithCurrentTimestamp()
                .AddField(x =>
                {
                    x.Name = "Requested Rotation";
                    x.Value = Tracker.ToString();
                    x.IsInline = true;
                });
        }

        public async Task CheckTrackers(DiscordShardedClient Client)
        {
            var newTrackers = new List<TTracker>();
            foreach (var tracker in Trackers)
            {
                try
                {
                    IUser user = Client.GetUser(tracker.DiscordID);
                    if (user == null)
                        user = Client.Rest.GetUserAsync(tracker.DiscordID).Result;

                    if (IsTrackerInRotation(tracker))
                        await user.SendMessageAsync(embed: GenerateTrackerAlertEmbed(tracker).Build());
                    else
                        newTrackers.Add(tracker);
                }
                catch (Exception x)
                {
                    Log.Warning("[{Type}] Unable to send message to user: {Id}. {Exception}", "Tracking", tracker.DiscordID, x);
                    continue;
                }
            }
            Trackers = newTrackers;
            UpdateJSON();
        }
    }
}
