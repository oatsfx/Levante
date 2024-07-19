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
using Levante.Util;
using Levante.Commands;
using System.Collections;

namespace Levante.Services
{
    public class LoggingService
    {
        private readonly Timer _timer;
        private readonly DiscordShardedClient _client;

        public readonly string FilePath = @"Configs/loggingConfig.json";
        public readonly string OverridesDirectory = @"LoggingOverrides/";

        private LoggingConfig loggingConfig;
        private List<LoggingOverride> loggingOverrides = new();

        public LoggingService(DiscordShardedClient client)
        {
            _client = client;

            if (Directory.Exists(OverridesDirectory))
            {
                foreach (string fileName in Directory.GetFiles(OverridesDirectory).Where(x => x.EndsWith(".json")))
                {
                    string json = File.ReadAllText(fileName);
                    var loggingOverride = JsonConvert.DeserializeObject<LoggingOverride>(json);
                    loggingOverrides.Add(loggingOverride);
                }
                Log.Information("[{Type}] Loaded {Count} Logging Overrides.", "Logging", loggingOverrides.Count);
            }
            else
            {
                Directory.CreateDirectory(OverridesDirectory);
                Log.Warning("No {Directory} directory detected. A new one has been created. No action is needed.", OverridesDirectory);
            }

            if (File.Exists(FilePath))
            {
                string json = File.ReadAllText(FilePath);
                loggingConfig = JsonConvert.DeserializeObject<LoggingConfig>(json);
            }
            else
            {
                loggingConfig = new LoggingConfig();
                File.WriteAllText(FilePath, JsonConvert.SerializeObject(loggingConfig, Formatting.Indented));
                Log.Warning("No {FilePath} file detected. A new one has been created. No action is needed.", FilePath);
            }
            _timer = new Timer(RefreshXPUsersCallback, null, 5000, AppConfig.Logging.TimeBetweenRefresh * 60000);

            Log.Information("[{Type}] Logging Service Loaded. Continued logging for {Count} (+{PriorityCount}) Users.", "Logging", loggingConfig.Users.Count, loggingConfig.PriorityUsers.Count);
        }

        public async void RefreshXPUsersCallback(object o)
        {
            // Stop Timer
            _timer.Change(Timeout.Infinite, Timeout.Infinite);
            await RefreshXPUsers();
            UpdateUsersConfig();

            // In place if the experimental function decides to break!
            if (AppConfig.Logging.RefreshScaling >= 0)
            {
                int msTilNext = (int)Math.Floor((-((loggingConfig.Users.Count + loggingConfig.PriorityUsers.Count) * AppConfig.Logging.RefreshScaling) + AppConfig.Logging.TimeBetweenRefresh) * 60000);
                msTilNext = msTilNext < 0 ? 0 : msTilNext;
                double minutesTilNext = msTilNext / (double)60000;

                _timer.Change(msTilNext, msTilNext);
                Log.Information("[{Type}] XP Logging Refreshed! Next refresh in: {Time} minute(s).", "Logging", minutesTilNext);
            }
            else
            {
                _timer.Change(AppConfig.Logging.TimeBetweenRefresh * 60000, AppConfig.Logging.TimeBetweenRefresh * 60000);
                Log.Information("[{Type}] XP Logging Refreshed! Next refresh in: {Time} minute(s).", "Logging", AppConfig.Logging.TimeBetweenRefresh);
            }
        }

        private async Task RefreshXPUsers()
        {
            if (loggingConfig.Users.Count <= 0 && loggingConfig.PriorityUsers.Count <= 0)
            {
                Log.Information("[{Type}] Skipping refresh, no active logging users...", "Logging");
                return;
            }

            try // XP Logs
            {
                Log.Information("[{Type}] Refreshing XP Logging Users...", "Logging");
                var combinedUsers = loggingConfig.PriorityUsers.Concat(loggingConfig.Users);
                foreach (var aau in combinedUsers.ToList())
                    await Task.Run(() => HandleXPUser(aau)).ConfigureAwait(false);
            }
            catch (Exception x)
            {
                Log.Warning("[{Type}] Refresh failed, trying again! Reason: {Message} ({StackTrace})", "Logging", x.Message, x.StackTrace);
                await Task.Delay(8000);
                await RefreshXPUsers().ConfigureAwait(false);
                return;
            }
        }

        private async Task HandleXPUser(LoggingUser aau)
        {
            var tempAau = aau;

            // If the user has removed themselves from logging while we are refreshing.
            if (!(loggingConfig.Users.Exists(x => x.DiscordChannelID == tempAau.DiscordChannelID) ||
                loggingConfig.PriorityUsers.Exists(x => x.DiscordChannelID == tempAau.DiscordChannelID)))
                return;

            Log.Information("[{Type}] Checking {User}.", "Logging", tempAau.UniqueBungieName);
            var actualUser = loggingConfig.Users.FirstOrDefault(x => x.DiscordChannelID == tempAau.DiscordChannelID);
            actualUser ??= loggingConfig.PriorityUsers.FirstOrDefault(x => x.DiscordChannelID == tempAau.DiscordChannelID);

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
                await LogHelper.Log(dmChannel, $"Here is the session summary from <#{tempAau.DiscordChannelID}>, beginning on {TimestampTag.FromDateTime(tempAau.Start.Timestamp)}.", GenerateSessionSummary(tempAau));

                Log.Information("[{Type}] Stopped logging for {User} via automation.", "Logging", tempAau.UniqueBungieName);

                DeleteActiveUserFromConfig(tempAau.DiscordID);
                await Task.Run(() => LeaderboardHelper.CheckLeaderboardData(tempAau));
            }

            var currentOverride = loggingOverrides.FirstOrDefault(x => x.Hash == tempAau.OverrideHash);
            var loggingValues = new XpLoggingValueResponse(tempAau.DiscordID, currentOverride);
            var updatedLevel = loggingValues.CurrentLevel;
            var updatedExtraLevel = loggingValues.CurrentExtraLevel;
            var updatedProgression = loggingValues.XpProgress;
            var powerBonus = loggingValues.PowerBonus;
            var errorStatus = loggingValues.ErrorStatus;
            var activityHash = loggingValues.ActivityHash;
            var nextLevelAt = loggingValues.NextLevelAt;
            var levelCap = loggingValues.LevelCap;

            var combinedLevel = updatedLevel + updatedExtraLevel;
            var lastCombinedLevel = aau.Last.Level + aau.Last.ExtraLevel;

            if (!errorStatus.Equals("Success"))
            {
                if (tempAau.NoGainRefreshes >= AppConfig.Logging.RefreshesBeforeKick)
                {
                    string uniqueName = tempAau.UniqueBungieName;

                    await LogHelper.Log(logChannel, $"Refresh unsuccessful. Reason: {errorStatus}.");
                    await LogHelper.Log(logChannel, $"<@{tempAau.DiscordID}>: Refresh unsuccessful. Reason: {errorStatus}. Here is your session summary:", GenerateSessionSummary(tempAau), GenerateChannelButtons(tempAau.DiscordID));

                    await LogHelper.Log(dmChannel, $"Here is the session summary from <#{tempAau.DiscordChannelID}>, beginning on {TimestampTag.FromDateTime(tempAau.Start.Timestamp)}.", GenerateSessionSummary(tempAau));

                    Log.Information("[{Type}] Stopped logging for {User} via automation.", "Logging", tempAau.UniqueBungieName);
                    DeleteActiveUserFromConfig(tempAau.DiscordID);
                    await Task.Run(() => LeaderboardHelper.CheckLeaderboardData(tempAau));
                }
                else
                {
                    actualUser.NoGainRefreshes = tempAau.NoGainRefreshes + 1;
                    await LogHelper.Log(logChannel, $"Refresh unsuccessful. Reason: {errorStatus}. Warning {tempAau.NoGainRefreshes} of {AppConfig.Logging.RefreshesBeforeKick}.");
                    Log.Information("[{Type}] Refresh unsuccessful for {User}. Reason: {Reason}", "Logging", tempAau.UniqueBungieName, errorStatus);
                }
                return;
            }

            // Power Bonus increase.
            if (powerBonus > tempAau.Last.PowerBonus)
            {
                await LogHelper.Log(logChannel, $"Power bonus increase!: {tempAau.Last.PowerBonus} -> {powerBonus} (Start: {tempAau.Start.PowerBonus}).");

                actualUser.Last.PowerBonus = powerBonus;
            }

            // A player's activity has changed, unless an override says to ignore it.
            var isAllowedActivityChange = currentOverride != null && !currentOverride.IsAllowedActivityChange;
            if (isAllowedActivityChange && activityHash != tempAau.ActivityHash)
            {
                string uniqueName = tempAau.UniqueBungieName;

                await LogHelper.Log(logChannel, $"<@{tempAau.DiscordID}>: Player activity has changed. Logging terminated by automation. Here is your session summary:", GenerateSessionSummary(tempAau), GenerateChannelButtons(tempAau.DiscordID));

                //await LogHelper.Log(dmChannel, $"<@{tempAau.DiscordID}>: Player has been determined as inactive. Logging will be terminated for {uniqueName}.");
                await LogHelper.Log(dmChannel, $"Here is the session summary from <#{tempAau.DiscordChannelID}>, beginning on {TimestampTag.FromDateTime(tempAau.Start.Timestamp)}.", GenerateSessionSummary(tempAau));

                Log.Information("[{Type}] Stopped logging for {User} via automation.", "Logging", tempAau.UniqueBungieName);
                DeleteActiveUserFromConfig(tempAau.DiscordID);
                await Task.Run(() => LeaderboardHelper.CheckLeaderboardData(tempAau)).ConfigureAwait(false);

                return;
            }

            // Check override values, if a user has chosen to use an override when logging.
            if (currentOverride != null)
            {
                // This is how we determine if a user is no longer AFK.
                // It is understood that whatever value goes here is going to change between each time we refresh.
                // If not, oh well, hope you get XP from it.
                switch (currentOverride.OverrideType)
                {
                    case LoggingOverrideType.ProfileProgression:
                    case LoggingOverrideType.CharacterProgression:
                    case LoggingOverrideType.Consumable:
                    case LoggingOverrideType.ProfileStringVariable:
                    case LoggingOverrideType.CharacterStringVariable:
                        {
                            // Likely tracking an integer.
                            var startValue = tempAau.Start.OverrideValue;
                            var oldValue = tempAau.Last.OverrideValue;
                            var newValue = loggingValues.OverrideValue;

                            var checkGreaterThan = currentOverride.CheckForIncrease;
                            var isIncreased = oldValue < newValue;
                            var isDecreased = oldValue > newValue;

                            // If we aren't tracking XP, we are tracking the value. So do necessary inactivity checks on value instead of XP.
                            // The value is not increased, and the override calls for checking increases. OR
                            // The value is increased, and the override calls for checking decreases, but not wanting increases.
                            // This creates a case where something happens like no XP change.
                            if (!currentOverride.TrackXp && ((!isIncreased && checkGreaterThan) || (!isDecreased && !checkGreaterThan)))
                            {
                                if (tempAau.NoGainRefreshes >= AppConfig.Logging.RefreshesBeforeKick)
                                {
                                    string uniqueName = tempAau.UniqueBungieName;

                                    await LogHelper.Log(logChannel, $"<@{tempAau.DiscordID}>: Player has been determined as inactive. Logging terminated by automation. Here is your session summary:", GenerateSessionSummary(tempAau), GenerateChannelButtons(tempAau.DiscordID));

                                    await LogHelper.Log(dmChannel, $"Here is the session summary from <#{tempAau.DiscordChannelID}>, beginning on {TimestampTag.FromDateTime(tempAau.Start.Timestamp)}.", GenerateSessionSummary(tempAau));

                                    Log.Information("[{Type}] Stopped logging for {User} via automation.", "Logging", tempAau.UniqueBungieName);

                                    DeleteActiveUserFromConfig(tempAau.DiscordID);
                                    await Task.Run(() => LeaderboardHelper.CheckLeaderboardData(tempAau)).ConfigureAwait(false);
                                }
                                else
                                {
                                    actualUser.NoGainRefreshes = tempAau.NoGainRefreshes + 1;
                                    await LogHelper.Log(logChannel, $"No {currentOverride.ShortName} change detected, waiting for next refresh... Warning {tempAau.NoGainRefreshes} of {AppConfig.Logging.RefreshesBeforeKick}.");
                                }

                                return;
                            }

                            if ((isIncreased && checkGreaterThan) || (isDecreased && !checkGreaterThan))
                            {
                                // Send a value change and rate for the inventory item.
                                var refreshDelta = newValue - oldValue;
                                var startDelta = newValue - startValue;
                                Log.Debug($"{refreshDelta} - {startDelta}");

                                await LogHelper.Log(logChannel, $"{oldValue:n0}{(refreshDelta != 0 ? $" ({(refreshDelta < 0 ? "-" : "+")}{refreshDelta:n0})" : "")} -> {newValue:n0} {currentOverride.ShortName}" +
                                    $" | Rate: {(int)Math.Floor(startDelta / (DateTime.Now - aau.Start.Timestamp).TotalHours):n0} {currentOverride.ShortName}/Hour" +
                                    $" | Inst. Rate: {(int)Math.Floor(refreshDelta / (DateTime.Now - aau.Last.Timestamp).TotalHours):n0} {currentOverride.ShortName}/Hour");

                                actualUser.Last.OverrideValue = newValue;
                            }

                            // Don't bother doing XP calculations.
                            if (!currentOverride.TrackXp)
                            {
                                actualUser.NoGainRefreshes = 0;
                                actualUser.Last.Timestamp = DateTime.Now;

                                return;
                            }

                            break;
                        }
                    case LoggingOverrideType.InventoryItem:
                        {
                            // Likely tracking a list of instance IDs.
                            var startValue = tempAau.Start.OverrideInventoryList;
                            var oldValue = tempAau.Last.OverrideInventoryList;
                            var newValue = loggingValues.OverrideInventoryList;

                            var setDiffNew = newValue.Except(oldValue).ToList();
                            var setDiffOld = oldValue.Except(newValue).ToList();

                            var checkGreaterThan = currentOverride.CheckForIncrease;
                            var isIncreased = setDiffNew.Count > 0;
                            var isDecreased = setDiffOld.Count > 0;

                            // If we aren't tracking XP, we are tracking the value. So do necessary inactivity checks on value instead of XP.
                            // The value is not increased, and the override calls for checking increases. OR
                            // The value is increased, and the override calls for checking decreases, but not wanting increases.
                            // This creates a case where something happens like no XP change.
                            if (!currentOverride.TrackXp && ((!isIncreased && checkGreaterThan) || (!isDecreased && !checkGreaterThan)))
                            {
                                if (tempAau.NoGainRefreshes >= AppConfig.Logging.RefreshesBeforeKick)
                                {
                                    string uniqueName = tempAau.UniqueBungieName;

                                    await LogHelper.Log(logChannel, $"<@{tempAau.DiscordID}>: Player has been determined as inactive. Logging terminated by automation. Here is your session summary:", GenerateSessionSummary(tempAau), GenerateChannelButtons(tempAau.DiscordID));

                                    await LogHelper.Log(dmChannel, $"Here is the session summary from <#{tempAau.DiscordChannelID}>, beginning on {TimestampTag.FromDateTime(tempAau.Start.Timestamp)}.", GenerateSessionSummary(tempAau));

                                    Log.Information("[{Type}] Stopped logging for {User} via automation.", "Logging", tempAau.UniqueBungieName);

                                    DeleteActiveUserFromConfig(tempAau.DiscordID);
                                    await Task.Run(() => LeaderboardHelper.CheckLeaderboardData(tempAau)).ConfigureAwait(false);
                                }
                                else
                                {
                                    actualUser.NoGainRefreshes = tempAau.NoGainRefreshes + 1;
                                    await LogHelper.Log(logChannel, $"No change detected, waiting for next refresh... Warning {tempAau.NoGainRefreshes} of {AppConfig.Logging.RefreshesBeforeKick}.");
                                }

                                return;
                            }

                            if ((isIncreased && checkGreaterThan) || (isDecreased && !checkGreaterThan))
                            {
                                // Send a value change and rate for the inventory item.
                                var refreshDelta = checkGreaterThan ? setDiffNew.Count : setDiffOld.Count;
                                var startDelta = actualUser.Last.OverrideValue + refreshDelta - actualUser.Start.OverrideValue;
                                Log.Debug($"{refreshDelta} - {startDelta}");

                                await LogHelper.Log(logChannel, $"{actualUser.Last.OverrideValue:n0}{(refreshDelta != 0 ? $" ({(refreshDelta < 0 ? "-" : "+")}{refreshDelta})" : "")} -> {actualUser.Last.OverrideValue + refreshDelta:n0} {currentOverride.ShortName}" +
                                    $" | Rate: {startDelta / (DateTime.Now - aau.Start.Timestamp).TotalHours:n2} {currentOverride.ShortName}/Hour" +
                                    $" | Inst. Rate: {refreshDelta / (DateTime.Now - aau.Last.Timestamp).TotalHours:n2} {currentOverride.ShortName}/Hour");

                                actualUser.Last.OverrideInventoryList = newValue;
                                actualUser.Last.OverrideValue = actualUser.Last.OverrideValue + refreshDelta;
                            }

                            // Don't bother doing XP calculations.
                            if (!currentOverride.TrackXp)
                            {
                                actualUser.NoGainRefreshes = 0;
                                actualUser.Last.Timestamp = DateTime.Now;

                                return;
                            }

                            break;
                        }
                    default:
                        {
                            break;
                        }
                }
            }
            
            // Level increase.
            if (lastCombinedLevel < combinedLevel)
            {
                await LogHelper.Log(logChannel, $"Level up! Now: {tempAau.Last.Level}{(tempAau.Last.ExtraLevel > 0 ? $" (+{tempAau.Last.ExtraLevel})" : "")}" +
                    $" -> {updatedLevel}{(updatedExtraLevel > 0 ? $" (+{updatedExtraLevel})" : "")} " +
                    $"({updatedProgression:n0}/{nextLevelAt:n0} XP). " +
                    $"Start: {tempAau.Start.Level}{(tempAau.Start.ExtraLevel > 0 ? $" (+{tempAau.Start.ExtraLevel})" : "")} ({tempAau.Start.LevelProgress:n0}/{tempAau.Start.NextLevelAt:n0} XP).");

                actualUser.Last.Level = updatedLevel;
                actualUser.Last.ExtraLevel = updatedExtraLevel;
                actualUser.Last.LevelProgress = updatedProgression;
                actualUser.Last.NextLevelAt = nextLevelAt;
                actualUser.NoGainRefreshes = 0;
                actualUser.Last.Timestamp = DateTime.Now;

                return;
            }

            // No progression update detected.
            if (updatedProgression <= tempAau.Last.LevelProgress)
            {
                if (tempAau.NoGainRefreshes >= AppConfig.Logging.RefreshesBeforeKick)
                {
                    string uniqueName = tempAau.UniqueBungieName;

                    await LogHelper.Log(logChannel, $"<@{tempAau.DiscordID}>: Player has been determined as inactive. Logging terminated by automation. Here is your session summary:", GenerateSessionSummary(tempAau), GenerateChannelButtons(tempAau.DiscordID));

                    //await LogHelper.Log(dmChannel, $"<@{tempAau.DiscordID}>: Player has been determined as inactive. Logging will be terminated for {uniqueName}.");
                    await LogHelper.Log(dmChannel, $"Here is the session summary from <#{tempAau.DiscordChannelID}>, beginning on {TimestampTag.FromDateTime(tempAau.Start.Timestamp)}.", GenerateSessionSummary(tempAau));

                    Log.Information("[{Type}] Stopped logging for {User} via automation.", "Logging", tempAau.UniqueBungieName);
                    //listOfRemovals.Add(tempAau);

                    DeleteActiveUserFromConfig(tempAau.DiscordID);
                    //ActiveConfig.ActiveAFKUsers.Remove(ActiveConfig.ActiveAFKUsers.FirstOrDefault(x => x.DiscordChannelID == tempAau.DiscordChannelID));
                    await Task.Run(() => LeaderboardHelper.CheckLeaderboardData(tempAau)).ConfigureAwait(false);
                }
                else
                {
                    actualUser.NoGainRefreshes = tempAau.NoGainRefreshes + 1;
                    await LogHelper.Log(logChannel, $"No XP change detected, waiting for next refresh... Warning {tempAau.NoGainRefreshes} of {AppConfig.Logging.RefreshesBeforeKick}.");
                }

                return;
            }

            // Track the XP of the player.
            int levelsGained = (aau.Last.Level - aau.Start.Level) + (aau.Last.ExtraLevel - aau.Start.ExtraLevel);
            long xpGained = 0;
            // If the user hit the cap during the session, calculate gained XP differently.
            if (aau.Start.Level < levelCap && aau.Last.Level >= levelCap)
            {
                int levelsToCap = levelCap - aau.Start.Level;
                int levelsPastCap = levelsGained - levelsToCap;
                // StartNextLevelAt should be the 100,000 or whatever Bungie decides to change it to later on.
                xpGained = (levelsToCap * aau.Start.NextLevelAt) + (levelsPastCap * nextLevelAt) - aau.Start.LevelProgress + updatedProgression;
            }
            else // The nextLevelAt stays the same because it did not change mid-logging session.
            {
                xpGained = (levelsGained * nextLevelAt) - aau.Start.LevelProgress + updatedProgression;
            }

            long xpGainedInOneRefresh = updatedProgression - tempAau.Last.LevelProgress;

            await LogHelper.Log(logChannel, $"{tempAau.Last.LevelProgress:n0} (+{xpGainedInOneRefresh:n0}) -> {updatedProgression:n0} XP" +
                $" | Level: {updatedLevel}{(updatedExtraLevel > 0 ? $" (+{updatedExtraLevel})" : "")}" +
                $" | Power: +{powerBonus}" +
                $" | Rate: {(int)Math.Floor(xpGained / (DateTime.Now - aau.Start.Timestamp).TotalHours):n0} XP/Hour" +
                $" | Inst. Rate: {(int)Math.Floor(xpGainedInOneRefresh / (DateTime.Now - aau.Last.Timestamp).TotalHours):n0} XP/Hour");

            actualUser.Last.Level = updatedLevel;
            actualUser.Last.ExtraLevel = updatedExtraLevel;
            actualUser.Last.LevelProgress = updatedProgression;
            actualUser.Last.NextLevelAt = nextLevelAt;
            actualUser.NoGainRefreshes = 0;
            actualUser.Last.Timestamp = DateTime.Now;
        }

        public EmbedBuilder GenerateSessionSummary(LoggingUser aau)
        {
            var auth = new EmbedAuthorBuilder()
            {
                Name = $"Session Summary: {aau.UniqueBungieName}",
                IconUrl = AppConfig.App.AvatarUrl,
            };
            var foot = new EmbedFooterBuilder()
            {
                Text = $"XP Logging Session Summary"
            };
            var embed = new EmbedBuilder()
            {
                Color = new Discord.Color(AppConfig.Discord.EmbedColor.R, AppConfig.Discord.EmbedColor.G, AppConfig.Discord.EmbedColor.B),
                Author = auth,
                Footer = foot,
            };
            int powerBonusGained = aau.Last.PowerBonus - aau.Start.PowerBonus;
            var levelCap = ManifestHelper.CurrentLevelCap;
            var nextLevelAt = aau.Last.NextLevelAt;

            int levelsGained = (aau.Last.Level - aau.Start.Level) + (aau.Last.ExtraLevel - aau.Start.ExtraLevel);
            long xpGained = 0;
            // If the user hit the cap during the session, calculate gained XP differently.
            if (aau.Start.Level < levelCap && aau.Last.Level >= levelCap)
            {
                int levelsToCap = levelCap - aau.Start.Level;
                int levelsPastCap = levelsGained - levelsToCap;
                // StartNextLevelAt should be the 100,000 or whatever Bungie decides to change it to later on.
                xpGained = ((levelsToCap) * aau.Start.NextLevelAt) + (levelsPastCap * nextLevelAt) - aau.Start.LevelProgress + aau.Last.LevelProgress;
            }
            else // The nextLevelAt stays the same because it did not change mid-logging session.
            {
                xpGained = (levelsGained * nextLevelAt) - aau.Start.LevelProgress + aau.Last.LevelProgress;
            }

            var timeSpan = DateTime.Now - aau.Start.Timestamp;
            string timeString = $"{(Math.Floor(timeSpan.TotalHours) > 0 ? $"{Math.Floor(timeSpan.TotalHours)}h " : "")}" +
                    $"{(timeSpan.Minutes > 0 ? $"{timeSpan.Minutes:00}m " : "")}" +
                    $"{timeSpan.Seconds:00}s";
            int xpPerHour = (int)Math.Floor(xpGained / (DateTime.Now - aau.Start.Timestamp).TotalHours);
            embed.WithCurrentTimestamp();
            embed.Description = $"Time Logged: {timeString}\n";

            embed.AddField(x =>
            {
                x.Name = "Level";
                x.Value = $"Start: {aau.Start.Level}{(aau.Start.ExtraLevel > 0 ? $" (+{aau.Start.ExtraLevel})" : "")} ({aau.Start.LevelProgress:n0}/{aau.Start.NextLevelAt:n0})\n" +
                    $"Now: {aau.Last.Level}{(aau.Last.ExtraLevel > 0 ? $" (+{aau.Last.ExtraLevel})" : "")} ({aau.Last.LevelProgress:n0}/{aau.Last.NextLevelAt:n0})\n" +
                    $"Gained: {levelsGained}\n";
                x.IsInline = true;
            }).AddField(x =>
            {
                x.Name = "XP";
                x.Value = $"Gained: {xpGained:n0}\n" +
                    $"XP/Hour: {xpPerHour:n0}";
                x.IsInline = true;
            }).AddField(x =>
            {
                x.Name = "Power Bonus";
                x.Value = $"Start: {aau.Start.PowerBonus}\n" +
                    $"Now: {aau.Last.PowerBonus}\n" +
                    $"Gained: {powerBonusGained}\n";
                x.IsInline = true;
            });

            // We have a Logging Override, display it.
            if (aau.OverrideHash != 0)
            {
                var logOverride = GetLoggingOverride(aau.OverrideHash);
                embed.AddField(x =>
                {
                    x.Name = logOverride.ShortName;
                    x.Value = $"Start: {aau.Start.OverrideValue}\n" +
                        $"Now: {aau.Last.OverrideValue}\n" +
                        $"Gained: {aau.Last.OverrideValue - aau.Start.OverrideValue}\n";
                });
            }

            return embed;
        }

        public static ComponentBuilder GenerateChannelButtons(ulong DiscordID)
        {
            var deleteEmote = new Emoji("⛔");
            var restartEmote = new Emoji("✅");

            var buttonBuilder = new ComponentBuilder()
                .WithButton("Restart Logging", customId: $"restartLogging:{DiscordID}", ButtonStyle.Success, restartEmote, row: 0)
                .WithButton("Delete Log Channel", customId: $"deleteChannel", ButtonStyle.Secondary, deleteEmote, row: 0);

            return buttonBuilder;
        }

        public int GetXpLoggingUserCount() => loggingConfig.Users.Count;

        public int GetPriorityXpLoggingUserCount() => loggingConfig.PriorityUsers.Count;

        public bool IsXpLoggingFull() => loggingConfig.Users.Count >= AppConfig.Logging.MaximumLoggingUsers;

        public List<LoggingUser> GetXpLoggingUsers() => loggingConfig.Users;

        public List<LoggingUser> GetPriorityXpLoggingUsers() => loggingConfig.PriorityUsers;

        public LoggingUser GetLoggingUser(ulong DiscordID)
        {
            var result = loggingConfig.Users.FirstOrDefault(x => x.DiscordID == DiscordID);
            result ??= loggingConfig.PriorityUsers.FirstOrDefault(x => x.DiscordID == DiscordID);
            return result;
        }

        public List<LoggingOverride> GetLoggingOverrides() => loggingOverrides;

        public LoggingOverride GetLoggingOverride(long Hash) => loggingOverrides.FirstOrDefault(x => x.Hash == Hash);

        public void AddLoggingOverride(LoggingOverride lo)
        {
            string overridePath = @"Configs/EmblemOffers/" + lo.ShortName + @".json";
            loggingOverrides.Add(lo);

            if (!File.Exists(overridePath))
                File.Create(overridePath).Close();

            string output = JsonConvert.SerializeObject(lo, Formatting.Indented);
            File.WriteAllText(overridePath, output);
            Log.Information("[{Type}] Created Logging Override: {Name} ({Hash}).", "Logging", lo.ShortName, lo.Hash);
        }

        public void RemoveLoggingOverride(LoggingOverride lo)
        {
            string emblemOfferPath = @"Configs/EmblemOffers/";
            loggingOverrides.Remove(lo);
            File.Delete(emblemOfferPath + @"/" + lo.ShortName + @".json");
            Log.Information("[{Type}] Deleted Logging Override: {Name} ({Hash}).", "Logging", lo.ShortName, lo.Hash);
        }

        public bool HasExistingOverride(long Hash) => loggingOverrides.Any(x => x.Hash == Hash);

        public int GetMaxLoggingUsers() => AppConfig.Logging.MaximumLoggingUsers;

        public void SetMaxLoggingUsers(int MaxLoggingUsers) => AppConfig.Logging.MaximumLoggingUsers = MaxLoggingUsers;

        public void UpdateUsersConfig()
        {
            string output = JsonConvert.SerializeObject(loggingConfig, Formatting.Indented);
            File.WriteAllText(FilePath, output);
        }

        public void AddUserToConfig(LoggingUser aau, LoggingType Type)
        {
            switch (Type)
            {
                case LoggingType.Basic: loggingConfig.Users.Add(aau); break;
                case LoggingType.Priority: loggingConfig.PriorityUsers.Add(aau); break;
                default: loggingConfig.Users.Add(aau); break;
            }
        }

        public void DeleteActiveUserFromConfig(ulong DiscordID)
        {
            if (loggingConfig.Users.Exists(x => x.DiscordID == DiscordID))
                loggingConfig.Users.Remove(loggingConfig.Users.First(x => x.DiscordID == DiscordID));
            else
                loggingConfig.PriorityUsers.Remove(loggingConfig.PriorityUsers.First(x => x.DiscordID == DiscordID));
        }

        public bool IsExistingActiveUser(ulong DiscordID) => loggingConfig.Users.Exists(x => x.DiscordID == DiscordID) || loggingConfig.PriorityUsers.Exists(x => x.DiscordID == DiscordID);
    }
}
