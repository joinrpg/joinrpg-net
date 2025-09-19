using System.Security.Cryptography;
using System.Text;
using JoinRpg.Common.Telegram;
using Microsoft.Extensions.Options;

namespace JoinRpg.Portal.Infrastructure.Authentication.Telegram;

/// <summary>
/// A helper class used to verify authorization data.
/// Based on https://github.com/TelegramBots/Telegram.Bot.Extensions.LoginWidget/
/// </summary>
/// <remarks>
/// Construct a new <see cref="TelegramLoginValidator"/> instance
/// </remarks>
public class TelegramLoginValidator(IOptions<TelegramLoginOptions> options)
{
    private static readonly DateTime _unixStart = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    /// <summary>
    /// Checks whether the authorization data received from the user is valid
    /// </summary>
    /// <param name="fields">A collection containing query string fields as sorted key-value pairs</param>
    /// <returns></returns>
    public TelegramAuthorizationResult CheckAuthorization(SortedDictionary<string, string> fields)
    {
        ArgumentNullException.ThrowIfNull(fields);

        var _hmac = new HMACSHA256(SHA256.HashData(Encoding.ASCII.GetBytes(options.Value.BotId + ":" + options.Value.BotSecret)));

        if (!fields.ContainsKey(Field.Id) ||
            !fields.TryGetValue(Field.AuthDate, out var authDate) ||
            !fields.TryGetValue(Field.Hash, out var hash)
        )
        {
            return TelegramAuthorizationResult.MissingFields;
        }

        if (hash.Length != 64)
        {
            return TelegramAuthorizationResult.InvalidHash;
        }

        if (!long.TryParse(authDate, out var timestamp))
        {
            return TelegramAuthorizationResult.InvalidAuthDateFormat;
        }

        if (TimeSpan.FromSeconds(Math.Abs(DateTime.UtcNow.Subtract(_unixStart).TotalSeconds - timestamp)) > options.Value.AllowedTimeOffset)
        {
            return TelegramAuthorizationResult.TooOld;
        }

        fields.Remove(Field.Hash);
        var dataStringBuilder = new StringBuilder(256);
        foreach (var field in fields)
        {
            if (!string.IsNullOrEmpty(field.Value))
            {
                dataStringBuilder.Append(field.Key);
                dataStringBuilder.Append('=');
                dataStringBuilder.Append(field.Value);
                dataStringBuilder.Append('\n');
            }
        }
        dataStringBuilder.Length -= 1; // Remove the last \n

        var signature = _hmac.ComputeHash(Encoding.UTF8.GetBytes(dataStringBuilder.ToString()));

        // Adapted from: https://stackoverflow.com/a/14333437/6845657
        for (var i = 0; i < signature.Length; i++)
        {
            if (hash[i * 2] != 87 + (signature[i] >> 4) + ((signature[i] >> 4) - 10 >> 31 & -39))
            {
                return TelegramAuthorizationResult.InvalidHash;
            }

            if (hash[i * 2 + 1] != 87 + (signature[i] & 0xF) + ((signature[i] & 0xF) - 10 >> 31 & -39))
            {
                return TelegramAuthorizationResult.InvalidHash;
            }
        }

        return TelegramAuthorizationResult.Valid;
    }

    /// <summary>
    /// Checks whether the authorization data received from the user is valid
    /// </summary>
    /// <param name="fields">A collection containing query string fields as key-value pairs</param>
    /// <returns></returns>
    public TelegramAuthorizationResult CheckAuthorization(Dictionary<string, string> fields)
    {
        ArgumentNullException.ThrowIfNull(fields);
        return CheckAuthorization(new SortedDictionary<string, string>(fields, StringComparer.Ordinal));
    }

    private static class Field
    {
        public const string AuthDate = "auth_date";
        public const string Id = "id";
        public const string Hash = "hash";
    }
}
