using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using BungieSharper.Entities.Forum;
using Discord;
using System.Threading.Tasks;
using Discord.WebSocket;
using Levante.Configs;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;
using Levante.Helpers;

namespace Levante.Services
{
    public class XPLoggingService
    {
        private readonly Timer _timer;
        private readonly DiscordShardedClient _client;

        public string FilePath = @"Configs/xpConfig.json";

        private XPLoggingConfig xpConfig;

        public XPLoggingService(DiscordShardedClient client)
        {
            _client = client;

            if (File.Exists(FilePath))
            {
                string json = File.ReadAllText(FilePath);
                xpConfig = JsonConvert.DeserializeObject<XPLoggingConfig>(json);
            }
            else
            {
                xpConfig = new XPLoggingConfig();
                File.WriteAllText(FilePath, JsonConvert.SerializeObject(xpConfig, Formatting.Indented));
                Log.Warning("No {FilePath} file detected. A new one has been created. No action is needed.", FilePath);
            }
            _timer = new Timer(RefreshXPUsersCallback, null, 20000, xpConfig.TimeBetweenRefresh * 60000);

            Log.Information("[{Type}] XP Logging Service Loaded. Continued XP logging for {Count} (+{PriorityCount}) Users.", "XP Logging", xpConfig.XPLoggingUsers.Count, xpConfig.PriorityXPLoggingUsers.Count);
        }

        public async void RefreshXPUsersCallback(object o)
        {
            // Stop Timer
            _timer.Change(Timeout.Infinite, Timeout.Infinite);
            await RefreshXPUsers();
            UpdateActiveAFKUsersConfig();

            // In place if the experimental function decides to break!
            if (xpConfig.RefreshScaling >= 0)
            {
                int msTilNext = (int)Math.Floor((-((xpConfig.XPLoggingUsers.Count + xpConfig.PriorityXPLoggingUsers.Count) * xpConfig.RefreshScaling) + xpConfig.TimeBetweenRefresh) * 60000);
                msTilNext = msTilNext < 0 ? 0 : msTilNext;
                double minutesTilNext = msTilNext / (double)60000;

                _timer.Change(msTilNext, msTilNext);
                Log.Information("[{Type}] XP Logging Refreshed! Next refresh in: {Time} minute(s).", "XP Sessions", minutesTilNext);
            }
            else
            {
                _timer.Change(xpConfig.TimeBetweenRefresh * 60000, xpConfig.TimeBetweenRefresh * 60000);
                Log.Information("[{Type}] XP Logging Refreshed! Next refresh in: {Time} minute(s).", "XP Sessions", xpConfig.TimeBetweenRefresh);
            }
        }

        private async Task RefreshXPUsers()
        {
            if (xpConfig.XPLoggingUsers.Count <= 0 && xpConfig.PriorityXPLoggingUsers.Count <= 0)
            {
                Log.Information("[{Type}] Skipping refresh, no active logging users...", "XP Sessions");
                return;
            }

            try // XP Logs
            {
                Log.Information("[{Type}] Refreshing XP Logging Users...", "XP Sessions");
                var combinedAFKUsers = xpConfig.PriorityXPLoggingUsers.Concat(xpConfig.XPLoggingUsers);
                foreach (XPLoggingUser aau in combinedAFKUsers.ToList())
                    await Task.Run(() => HandleXPUser(aau)).ConfigureAwait(false);
            }
            catch (Exception x)
            {
                Log.Warning("[{Type}] Refresh failed, trying again! Reason: {Message} ({StackTrace})", "XP Sessions", x.Message, x.StackTrace);
                await Task.Delay(8000);
                await RefreshXPUsers().ConfigureAwait(false);
                return;
            }
        }

        private async Task HandleXPUser(XPLoggingUser aau)
        {
            var tempAau = aau;

            // If the user has removed themselves from logging while we are refreshing.
            if (!(xpConfig.XPLoggingUsers.Exists(x => x.DiscordChannelID == tempAau.DiscordChannelID) ||
                  xpConfig.PriorityXPLoggingUsers.Exists(x => x.DiscordChannelID == tempAau.DiscordChannelID)))
                return;

            Log.Information("[{Type}] Checking {User}.", "XP Sessions", tempAau.UniqueBungieName);
            var actualUser = xpConfig.XPLoggingUsers.FirstOrDefault(x => x.DiscordChannelID == tempAau.DiscordChannelID);
            actualUser ??= xpConfig.PriorityXPLoggingUsers.FirstOrDefault(x => x.DiscordChannelID == tempAau.DiscordChannelID);

            IUser user = _client.GetUser(tempAau.DiscordID);
            if (user == null)
            {
                var _rClient = _client.Rest;
                user = await _rClient.GetUserAsync(tempAau.DiscordID);
            }

            var logChannel = _client.GetChannel(tempAau.DiscordChannelID) as ITextChannel;
            var dmChannel = user.CreateDMChannelAsync().Result;

            if (logChannel == null)
            {
                await LogHelper.Log(dmChannel, $"<@{tempAau.DiscordID}>: Refresh unsuccessful. Reason: LoggingChannelNotFound. Logging will be terminated for {tempAau.UniqueBungieName}.");
                await LogHelper.Log((IMessageChannel)dmChannel, $"Here is the session summary, beginning on {TimestampTag.FromDateTime(tempAau.TimeStarted)}.", GenerateSessionSummary(tempAau));

                Log.Information("[{Type}] Stopped logging for {User} via automation.", "XP Sessions", tempAau.UniqueBungieName);

                DeleteActiveUserFromConfig(tempAau.DiscordID);
                await Task.Run(() => LeaderboardHelper.CheckLeaderboardData(tempAau));
            }

            int updatedLevel = DataConfig.GetAFKValues(tempAau.DiscordID, out int updatedProgression, out int powerBonus, out string errorStatus, out long activityHash);

            if (!errorStatus.Equals("Success"))
            {
                if (tempAau.NoXPGainRefreshes >= xpConfig.RefreshesBeforeKick)
                {
                    string uniqueName = tempAau.UniqueBungieName;

                    await LogHelper.Log(logChannel, $"Refresh unsuccessful. Reason: {errorStatus}.");
                    await LogHelper.Log(logChannel, $"<@{tempAau.DiscordID}>: Refresh unsuccessful. Reason: {errorStatus}. Here is your session summary:", GenerateSessionSummary(tempAau), XPLoggingHelper.GenerateChannelButtons(tempAau.DiscordID));

                    await LogHelper.Log((IMessageChannel)dmChannel, $"Here is the session summary, beginning on {TimestampTag.FromDateTime(tempAau.TimeStarted)}.", GenerateSessionSummary(tempAau));

                    Log.Information("[{Type}] Stopped logging for {User} via automation.", "XP Sessions", tempAau.UniqueBungieName);
                    DeleteActiveUserFromConfig(tempAau.DiscordID);
                    await Task.Run(() => LeaderboardHelper.CheckLeaderboardData(tempAau));
                }
                else
                {
                    actualUser.NoXPGainRefreshes = tempAau.NoXPGainRefreshes + 1;
                    await LogHelper.Log(logChannel, $"Refresh unsuccessful. Reason: {errorStatus}. Warning {tempAau.NoXPGainRefreshes} of {xpConfig.RefreshesBeforeKick}.");
                    Log.Information("[{Type}] Refresh unsuccessful for {User}. Reason: {Reason}", "XP Sessions", tempAau.UniqueBungieName, errorStatus);
                    // Move onto the next user so everyone gets the message.
                    //newList.Add(tempAau);
                }
                return;
            }

            if (powerBonus > tempAau.LastPowerBonus)
            {
                await LogHelper.Log(logChannel, $"Power bonus increase!: {tempAau.LastPowerBonus} -> {powerBonus} (Start: {tempAau.StartPowerBonus}).");

                actualUser.LastPowerBonus = powerBonus;
            }

            if (activityHash != tempAau.ActivityHash)
            {
                string uniqueName = tempAau.UniqueBungieName;

                await LogHelper.Log(logChannel, $"<@{tempAau.DiscordID}>: Player activity has changed. Logging terminated by automation. Here is your session summary:", GenerateSessionSummary(tempAau), XPLoggingHelper.GenerateChannelButtons(tempAau.DiscordID));
                await LogHelper.Log((IMessageChannel)dmChannel, (string)$"Here is the session summary, beginning on {TimestampTag.FromDateTime(tempAau.TimeStarted)}.", GenerateSessionSummary(tempAau));

                Log.Information("[{Type}] Stopped logging for {User} via automation.", "XP Sessions", tempAau.UniqueBungieName);
                DeleteActiveUserFromConfig(tempAau.DiscordID);
                await Task.Run(() => LeaderboardHelper.CheckLeaderboardData(tempAau)).ConfigureAwait(false);
            }
            else if (updatedLevel > tempAau.LastLevel)
            {
                await LogHelper.Log(logChannel, $"Level up!: {tempAau.LastLevel} -> {updatedLevel} ({updatedProgression:n0}/100,000 XP). " +
                    $"Start: {tempAau.StartLevel} ({tempAau.StartLevelProgress:n0}/100,000 XP).");

                actualUser.LastLevel = updatedLevel;
                actualUser.LastLevelProgress = updatedProgression;
                actualUser.NoXPGainRefreshes = 0;
            }
            else if (updatedProgression <= tempAau.LastLevelProgress)
            {
                if (tempAau.NoXPGainRefreshes >= xpConfig.RefreshesBeforeKick)
                {
                    string uniqueName = tempAau.UniqueBungieName;

                    await LogHelper.Log(logChannel, $"<@{tempAau.DiscordID}>: Player has been determined as inactive. Logging terminated by automation. Here is your session summary:", GenerateSessionSummary(tempAau), XPLoggingHelper.GenerateChannelButtons(tempAau.DiscordID));

                    await LogHelper.Log((IMessageChannel)dmChannel, (string)$"Here is the session summary, beginning on {TimestampTag.FromDateTime(tempAau.TimeStarted)}.", GenerateSessionSummary(tempAau));

                    Log.Information("[{Type}] Stopped logging for {User} via automation.", "XP Sessions", tempAau.UniqueBungieName);

                    DeleteActiveUserFromConfig(tempAau.DiscordID);
                    await Task.Run(() => LeaderboardHelper.CheckLeaderboardData(tempAau)).ConfigureAwait(false);
                }
                else
                {
                    actualUser.NoXPGainRefreshes = tempAau.NoXPGainRefreshes + 1;
                    await LogHelper.Log(logChannel, $"No XP change detected, waiting for next refresh... Warning {tempAau.NoXPGainRefreshes} of {xpConfig.RefreshesBeforeKick}.");
                }
            }
            else
            {
                int levelsGained = aau.LastLevel - aau.StartLevel;
                long xpGained = (levelsGained * 100_000) - aau.StartLevelProgress + updatedProgression;
                await LogHelper.Log(logChannel, $"Refreshed: {tempAau.LastLevelProgress:n0} XP -> {updatedProgression:n0} XP. Level: {updatedLevel} | Power Bonus: +{powerBonus} | Rate: {(int)Math.Floor(xpGained / (DateTime.Now - aau.TimeStarted).TotalHours):n0} XP/Hour");

                actualUser.LastLevel = updatedLevel;
                actualUser.LastLevelProgress = updatedProgression;
                actualUser.NoXPGainRefreshes = 0;
            }
        }

        public EmbedBuilder GenerateSessionSummary(XPLoggingUser aau)
        {
            var auth = new EmbedAuthorBuilder()
            {
                Name = $"Session Summary: {aau.UniqueBungieName}",
                IconUrl = BotConfig.BotAvatarUrl,
            };
            var foot = new EmbedFooterBuilder()
            {
                Text = $"XP Logging Session Summary"
            };
            var embed = new EmbedBuilder()
            {
                Color = new Discord.Color(BotConfig.EmbedColor.R, BotConfig.EmbedColor.G, BotConfig.EmbedColor.B),
                Author = auth,
                Footer = foot,
            };
            int levelsGained = aau.LastLevel - aau.StartLevel;
            int powerBonusGained = aau.LastPowerBonus - aau.StartPowerBonus;
            long xpGained = (levelsGained * 100_000) - aau.StartLevelProgress + aau.LastLevelProgress;
            var timeSpan = DateTime.Now - aau.TimeStarted;
            string timeString = $"{(Math.Floor(timeSpan.TotalHours) > 0 ? $"{Math.Floor(timeSpan.TotalHours)}h " : "")}" +
                                $"{(timeSpan.Minutes > 0 ? $"{timeSpan.Minutes:00}m " : "")}" +
                                $"{timeSpan.Seconds:00}s";
            int xpPerHour = (int)Math.Floor(xpGained / (DateTime.Now - aau.TimeStarted).TotalHours);
            embed.WithCurrentTimestamp();
            embed.Description = $"Time Logged: {timeString}\n";

            embed.AddField(x =>
            {
                x.Name = "Level Information";
                x.Value = $"Start: {aau.StartLevel} ({aau.StartLevelProgress:n0}/100,000)\n" +
                          $"Now: {aau.LastLevel} ({aau.LastLevelProgress:n0}/100,000)\n" +
                          $"Gained: {levelsGained}\n";
                x.IsInline = true;
            }).AddField(x =>
            {
                x.Name = "XP Information";
                x.Value = $"Gained: {xpGained:n0}\n" +
                          $"XP Per Hour: {xpPerHour:n0}";
                x.IsInline = true;
            }).AddField(x =>
            {
                x.Name = "Power Bonus Information";
                x.Value = $"Start: {aau.StartPowerBonus}\n" +
                          $"Now: {aau.LastPowerBonus}\n" +
                          $"Gained: {powerBonusGained}\n";
                x.IsInline = true;
            });

            return embed;
        }

        public int GetXpLoggingUserCount() => xpConfig.XPLoggingUsers.Count;

        public int GetPriorityXpLoggingUserCount() => xpConfig.PriorityXPLoggingUsers.Count;

        public bool IsXpLoggingFull() => xpConfig.XPLoggingUsers.Count >= xpConfig.MaximumLoggingUsers;

        public List<XPLoggingUser> GetXpLoggingUsers() => xpConfig.XPLoggingUsers;

        public List<XPLoggingUser> GetPriorityXpLoggingUsers() => xpConfig.PriorityXPLoggingUsers;

        public XPLoggingUser GetXpLoggingUser(ulong DiscordID) => xpConfig.XPLoggingUsers.FirstOrDefault(x => x.DiscordID == DiscordID);

        public int GetMaxLoggingUsers() => xpConfig.MaximumLoggingUsers;

        public void SetMaxLoggingUsers(int MaxLoggingUsers) => xpConfig.MaximumLoggingUsers = MaxLoggingUsers;

        public void UpdateActiveAFKUsersList()
        {
            string json = File.ReadAllText(FilePath);
            xpConfig.XPLoggingUsers.Clear();
            XPLoggingConfig jsonObj = JsonConvert.DeserializeObject<XPLoggingConfig>(json);
        }

        public void UpdateActiveAFKUsersConfig()
        {
            string output = JsonConvert.SerializeObject(xpConfig, Formatting.Indented);
            File.WriteAllText(FilePath, output);
        }

        public void AddActiveUserToConfig(XPLoggingUser aau, LoggingType Type)
        {
            //string json = File.ReadAllText(FilePath);
            //ActiveAFKUsers.Clear();
            //ActiveConfig jsonObj = JsonConvert.DeserializeObject<ActiveConfig>(json);

            switch (Type)
            {
                case LoggingType.Basic: xpConfig.XPLoggingUsers.Add(aau); break;
                case LoggingType.Priority: xpConfig.PriorityXPLoggingUsers.Add(aau); break;
                default: xpConfig.XPLoggingUsers.Add(aau); break;
            }

            //ActiveConfig ac = new ActiveConfig();
            //string output = JsonConvert.SerializeObject(ac, Formatting.Indented);
            //File.WriteAllText(FilePath, output);
        }

        public void DeleteActiveUserFromConfig(ulong DiscordID)
        {
            if (xpConfig.XPLoggingUsers.Exists(x => x.DiscordID == DiscordID))
                xpConfig.XPLoggingUsers.Remove(xpConfig.XPLoggingUsers.First(x => x.DiscordID == DiscordID));
            else
                xpConfig.PriorityXPLoggingUsers.Remove(xpConfig.PriorityXPLoggingUsers.First(x => x.DiscordID == DiscordID));
            //ActiveConfig ac = new ActiveConfig();
            //string output = JsonConvert.SerializeObject(ac, Formatting.Indented);
            //File.WriteAllText(FilePath, output);
        }

        public bool IsExistingActiveUser(ulong DiscordID) => xpConfig.XPLoggingUsers.Exists(x => x.DiscordID == DiscordID) || xpConfig.PriorityXPLoggingUsers.Exists(x => x.DiscordID == DiscordID);
    }
}
