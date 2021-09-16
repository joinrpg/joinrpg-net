// ReSharper disable IdentifierTypo
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using PscbApi.Models;

// ReSharper disable once CheckNamespace
namespace PscbApi
{
    /// <summary>
    /// Helper functions for protocol objects and data
    /// </summary>
    public static class ProtocolHelper
    {

        internal static void CheckItem(this ICollection<string> items, string ident, Action<string> handleData)
        {
            var item = items.FirstOrDefault(
                i => i.StartsWith(ident, true, CultureInfo.InvariantCulture));
            if (!string.IsNullOrWhiteSpace(item))
            {
                handleData(
                    item.Remove(0, ident.Length)
                        .TrimStart(':')
                        .Trim());
            }
        }

        /// <summary>
        /// Parses description string with encoded payments system and processing response codes
        /// </summary>
        /// <param name="description">Encoded description string</param>
        /// <returns>Parsed data</returns>
        public static BankResponseInfo ParseDescriptionString(string description)
        {
            var items = description?.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            var result = new BankResponseInfo();
            if (items?.Length > 0)
            {
                items.CheckItem(
                    BankResponseInfo.PaymentsSystemCodeJsonName,
                    value => result.PaymentCode = (PaymentsSystemResponseCode?)(int.TryParse(value, out var parsed) ? parsed : (int?)null));
                items.CheckItem(
                    BankResponseInfo.ProcessingCenterCodeJsonName,
                    value => result.ProcessingCode = (ProcessingCenterResponseCode?)(int.TryParse(value, out var parsed) ? parsed : (int?)null));
                result.Description = items.LastOrDefault();
            }

            return result;
        }

        private static class FastPaymentsSystem
        {
            public const int MaxPurposeLength = 140;

            public static bool IsPurposeCharAllowed(char ch)
            {
                var code = (int)ch;
                return code is >= 32 and <= 126 or >= 1040 and <= 1103 or 8470;
            }
        }

        /// <summary>
        /// Strips chars not supported by payment method
        /// </summary>
        public static string PreparePaymentPurposeString(string s, PscbPaymentMethod pm)
        {
            if (pm != PscbPaymentMethod.FastPaymentsSystem)
                return s;

            s = new string(s.Where(FastPaymentsSystem.IsPurposeCharAllowed).ToArray());
            return s.Length > FastPaymentsSystem.MaxPurposeLength
                ? s.Substring(0, FastPaymentsSystem.MaxPurposeLength)
                : s;
        }
    }
}
