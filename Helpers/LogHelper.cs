using System;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Levante.Configs;
using Serilog.Configuration;
using Serilog.Core;
using Serilog.Events;
using Serilog;

namespace Levante.Helpers
{
    public static class LogHelper
    {
        public async static Task Log(IMessageChannel Channel, string Message)
        {
            if (Channel == null) return;
            try
            {
                await Channel.SendMessageAsync($"> [{GetTimePrefix()}]: {Message}");
            }
            catch
            {
                ConsoleLog("Unable to send message.");
            }
        }

        public async static Task Log(IMessageChannel Channel, string Message, ComponentBuilder CB)
        {
            if (Channel == null) return;
            try
            {
                await Channel.SendMessageAsync($"> [{GetTimePrefix()}]: {Message}", components: CB.Build());
            }
            catch
            {
                ConsoleLog("Unable to send message.");
            }
        }

        public async static Task Log(IMessageChannel Channel, string Message, EmbedBuilder Embed)
        {
            if (Channel == null) return;
            try
            {
                await Channel.SendMessageAsync($"> [{GetTimePrefix()}]: {Message}", embed: Embed.Build());
            }
            catch
            {
                ConsoleLog("Unable to send message.");
            }
        }

        public async static Task Log(IMessageChannel Channel, string Message, EmbedBuilder Embed, ComponentBuilder CB)
        {
            if (Channel == null) return;
            try
            {
                await Channel.SendMessageAsync($"> [{GetTimePrefix()}]: {Message}", embed: Embed.Build(), components: CB.Build());
            }
            catch
            {
                ConsoleLog("Unable to send message.");
            }
        }

        public static void ConsoleLog(string Message)
        {
            Serilog.Log.Information("{Message}", Message);

            if (LevanteCordInstance.Client != null && BotConfig.LoggingChannel != null)
                BotConfig.LoggingChannel.SendMessageAsync($"> [{GetTimePrefix()}]: {Message}");
        }
            

        private static TimestampTag GetTimePrefix() => TimestampTag.FromDateTime(DateTime.Now, TimestampTagStyles.LongTime);
    }

    public class DiscordLogSink : ILogEventSink
    {
        private readonly IFormatProvider _formatProvider;

        public DiscordLogSink(IFormatProvider formatProvider)
        {
            _formatProvider = formatProvider;
        }

        public void Emit(LogEvent logEvent)
        {
            var message = logEvent.RenderMessage(_formatProvider).Replace("\"", "");
            if (logEvent.Level >= LogEventLevel.Warning)
            {
                if (LevanteCordInstance.Client != null && BotConfig.LoggingChannel != null)
                    BotConfig.LoggingChannel.SendMessageAsync($"> [{GetTimePrefix(logEvent.Timestamp.DateTime)}]: {message}");
            }
        }

        private TimestampTag GetTimePrefix(DateTime timestamp) => TimestampTag.FromDateTime(timestamp, TimestampTagStyles.LongTime);
    }

    public static class DiscordLogSinkExtenstions
    {
        public static LoggerConfiguration DiscordLogSink(
                  this LoggerSinkConfiguration loggerConfiguration,
                  IFormatProvider formatProvider = null)
        {
            return loggerConfiguration.Sink(new DiscordLogSink(formatProvider));
        }
    }

    public enum LogType
    {

    }
}
