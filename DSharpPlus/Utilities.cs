﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using DSharpPlus.Net;
using Microsoft.Extensions.Logging;

namespace DSharpPlus
{
    /// <summary>
    /// Various Discord-related utilities.
    /// </summary>
    public static class Utilities
    {
        /// <summary>
        /// Gets the version of the library
        /// </summary>
        private static string VersionHeader { get; set; }
        private static Dictionary<Permissions, string> PermissionStrings { get; set; }

        internal static UTF8Encoding UTF8 { get; } = new UTF8Encoding(false);

        static Utilities()
        {
            PermissionStrings = new Dictionary<Permissions, string>();
            var t = typeof(Permissions);
            var ti = t.GetTypeInfo();
            var vals = Enum.GetValues(t).Cast<Permissions>();

            foreach (var xv in vals)
            {
                var xsv = xv.ToString();
                var xmv = ti.DeclaredMembers.FirstOrDefault(xm => xm.Name == xsv);
                var xav = xmv.GetCustomAttribute<PermissionStringAttribute>();

                PermissionStrings[xv] = xav.String;
            }

            var a = typeof(DiscordClient).GetTypeInfo().Assembly;

            var vs = "";
            var iv = a.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
            if (iv != null)
                vs = iv.InformationalVersion;
            else
            {
                var v = a.GetName().Version;
                vs = v.ToString(3);
            }

            VersionHeader = $"DiscordBot (https://github.com/DSharpPlus/DSharpPlus, v{vs})";
        }

        internal static string GetApiBaseUri() 
            => Endpoints.BASE_URI;

        internal static Uri GetApiUriFor(string path)
            => new Uri($"{GetApiBaseUri()}{path}");

        internal static Uri GetApiUriFor(string path, string queryString)
            => new Uri($"{GetApiBaseUri()}{path}{queryString}");

        internal static QueryUriBuilder GetApiUriBuilderFor(string path)
            => new QueryUriBuilder($"{GetApiBaseUri()}{path}");

        internal static string GetFormattedToken(BaseDiscordClient client)
        {
            return GetFormattedToken(client.Configuration);
        }

        internal static string GetFormattedToken(DiscordConfiguration config)
        { 
            switch (config.TokenType)
            {
                case TokenType.Bearer:
                    return $"Bearer {config.Token}";

                case TokenType.Bot:
                    return $"Bot {config.Token}";

                default:
                    throw new ArgumentException("Invalid token type specified.", nameof(config.Token));
            }
        }

        internal static Dictionary<string, string> GetBaseHeaders()
            => new Dictionary<string, string>();

        internal static string GetUserAgent()
            => VersionHeader;

        internal static bool ContainsUserMentions(string message)
        {
            string pattern = @"<@(\d+)>";
            Regex regex = new Regex(pattern, RegexOptions.ECMAScript);
            return regex.IsMatch(message);
        }

        internal static bool ContainsNicknameMentions(string message)
        {
            string pattern = @"<@!(\d+)>";
            Regex regex = new Regex(pattern, RegexOptions.ECMAScript);
            return regex.IsMatch(message);
        }

        internal static bool ContainsChannelMentions(string message)
        {
            string pattern = @"<#(\d+)>";
            Regex regex = new Regex(pattern, RegexOptions.ECMAScript);
            return regex.IsMatch(message);
        }

        internal static bool ContainsRoleMentions(string message)
        {
            string pattern = @"<@&(\d+)>";
            Regex regex = new Regex(pattern, RegexOptions.ECMAScript);
            return regex.IsMatch(message);
        }

        internal static bool ContainsEmojis(string message)
        {
            string pattern = @"<:(.*):(\d+)>";
            Regex regex = new Regex(pattern, RegexOptions.ECMAScript);
            return regex.IsMatch(message);
        }

        internal static IEnumerable<ulong> GetUserMentions(DiscordMessage message)
        {
            var regex = new Regex(@"<@!?(\d+)>", RegexOptions.ECMAScript);
            var matches = regex.Matches(message.Content);
            foreach (Match match in matches)
                yield return ulong.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
        }

        internal static IEnumerable<ulong> GetRoleMentions(DiscordMessage message)
        {
            var regex = new Regex(@"<@&(\d+)>", RegexOptions.ECMAScript);
            var matches = regex.Matches(message.Content);
            foreach (Match match in matches)
                yield return ulong.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
        }

        internal static IEnumerable<ulong> GetChannelMentions(DiscordMessage message)
        {
            var regex = new Regex(@"<#(\d+)>", RegexOptions.ECMAScript);
            var matches = regex.Matches(message.Content);
            foreach (Match match in matches)
                yield return ulong.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
        }

        internal static IEnumerable<ulong> GetEmojis(DiscordMessage message)
        {
            var regex = new Regex(@"<:([a-zA-Z0-9_]+):(\d+)>", RegexOptions.ECMAScript);
            var matches = regex.Matches(message.Content);
            foreach (Match match in matches)
                yield return ulong.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);
        }
        
        internal static bool HasMessageIntents(DiscordIntents? intents)
        {
            if (intents.HasValue)
                return intents.Value.HasIntent(DiscordIntents.GuildMessages) || intents.Value.HasIntent(DiscordIntents.DirectMessages);

            return true; //Will be false in the future.
        }

        internal static bool HasReactionIntents(DiscordIntents? intents)
        {
            if (intents.HasValue)
                return intents.Value.HasIntent(DiscordIntents.GuildMessageReactions) || intents.Value.HasIntent(DiscordIntents.DirectMessageReactions);

            return true; //Will be false in the future.
        }

        internal static bool HasTypingIntents(DiscordIntents? intents)
        {
            if (intents.HasValue)
                return intents.Value.HasIntent(DiscordIntents.GuildMessageTyping) || intents.Value.HasIntent(DiscordIntents.DirectMessageTyping);

            return true; //Will be false in the future.
        }
      
        // https://discord.com/developers/docs/topics/gateway#sharding-sharding-formula
        /// <summary>
        /// Gets a shard id from a guild id and total shard count.
        /// </summary>
        /// <param name="guildId">The guild id the shard is on.</param>
        /// <param name="shardCount">The total amount of shards.</param>
        /// <returns>The shard id.</returns>
        public static int GetShardId(ulong guildId, int shardCount)
            => (int)(guildId >> 22) % shardCount;

        /// <summary>
        /// Helper method to create a <see cref="DateTimeOffset"/> from Unix time seconds for targets that do not support this natively.
        /// </summary>
        /// <param name="unixTime">Unix time seconds to convert.</param>
        /// <param name="shouldThrow">Whether the method should throw on failure. Defaults to true.</param>
        /// <returns>Calculated <see cref="DateTimeOffset"/>.</returns>
        public static DateTimeOffset GetDateTimeOffset(long unixTime, bool shouldThrow = true)
        {
            try
            {
                return DateTimeOffset.FromUnixTimeSeconds(unixTime);
            }
            catch (Exception)
            {
                if (shouldThrow)
                    throw;

                return DateTimeOffset.MinValue;
            }
        }

        /// <summary>
        /// Helper method to create a <see cref="DateTimeOffset"/> from Unix time milliseconds for targets that do not support this natively.
        /// </summary>
        /// <param name="unixTime">Unix time milliseconds to convert.</param>
        /// <param name="shouldThrow">Whether the method should throw on failure. Defaults to true.</param>
        /// <returns>Calculated <see cref="DateTimeOffset"/>.</returns>
        public static DateTimeOffset GetDateTimeOffsetFromMilliseconds(long unixTime, bool shouldThrow = true)
        {
            try
            {
                return DateTimeOffset.FromUnixTimeMilliseconds(unixTime);
            }
            catch (Exception)
            {
                if (shouldThrow)
                    throw;

                return DateTimeOffset.MinValue;
            }
        }

        /// <summary>
        /// Helper method to calculate Unix time seconsd from a <see cref="DateTimeOffset"/> for targets that do not support this natively.
        /// </summary>
        /// <param name="dto"><see cref="DateTimeOffset"/> to calculate Unix time for.</param>
        /// <returns>Calculated Unix time.</returns>
        public static long GetUnixTime(DateTimeOffset dto)
            => dto.ToUnixTimeMilliseconds();
        
        /// <summary>
        /// Converts this <see cref="Permissions"/> into human-readable format.
        /// </summary>
        /// <param name="perm">Permissions enumeration to convert.</param>
        /// <returns>Human-readable permissions.</returns>
        public static string ToPermissionString(this Permissions perm)
        {
            if (perm == Permissions.None)
                return PermissionStrings[perm];

            perm &= PermissionMethods.FULL_PERMS;

            var strs = PermissionStrings
                .Where(xkvp => xkvp.Key != Permissions.None && (perm & xkvp.Key) == xkvp.Key)
                .Select(xkvp => xkvp.Value);

            return string.Join(", ", strs.OrderBy(xs => xs));
        }

        /// <summary>
        /// Checks whether this string contains given characters.
        /// </summary>
        /// <param name="str">String to check.</param>
        /// <param name="characters">Characters to check for.</param>
        /// <returns>Whether the string contained these characters.</returns>
        public static bool Contains(this string str, params char[] characters)
        {
            foreach (var xc in str)
                if (characters.Contains(xc))
                    return true;

            return false;
        }

        internal static void LogTaskFault(this Task task, ILogger<BaseDiscordClient> logger, LogLevel level, EventId eventId, string message)
        {
            if (task == null)
                throw new ArgumentNullException(nameof(task));

            if (logger == null)
                return;

#if NETSTANDARD1_3
            task.ContinueWith(t =>
            {
                switch (level)
                {
                    case LogLevel.Trace:
                        logger.LogTrace(eventId, t.Exception, message);
                        break;
                    case LogLevel.Debug:
                        logger.LogCritical(eventId, t.Exception, message);
                        break;
                    case LogLevel.Information:
                        logger.LogInformation(eventId, t.Exception, message);
                        break;
                    case LogLevel.Warning:
                        logger.LogWarning(eventId, t.Exception, message);
                        break;
                    case LogLevel.Error:
                        logger.LogError(eventId, t.Exception, message);
                        break;
                    case LogLevel.Critical:
                        logger.LogCritical(eventId, t.Exception, message);
                        break;
                }
            });
#else
            task.ContinueWith(t => logger.Log(level, eventId, t.Exception, message), TaskContinuationOptions.OnlyOnFaulted);
#endif 
        }

        internal static void Deconstruct<TKey, TValue>(this KeyValuePair<TKey, TValue> kvp, out TKey key, out TValue value)
        {
            key = kvp.Key;
            value = kvp.Value;
        }
    }
}
