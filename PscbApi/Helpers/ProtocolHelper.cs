// ReSharper disable IdentifierTypo
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using PscbApi.Models;

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
                handleData(
                    item.Remove(0, ident.Length)
                        .TrimStart(':')
                        .Trim());
        }

        /// <summary>
        /// Parses description string with encoded payments system and processing response codes
        /// </summary>
        /// <param name="description">Encoded description string</param>
        /// <returns>Parsed data</returns>
        public static BankResponseInfo ParseDescriptionString(string description)
        {
            var items = description?.Split(new[] {','}, StringSplitOptions.RemoveEmptyEntries);
            BankResponseInfo result = new BankResponseInfo();
            if (items?.Length > 0)
            {
                items.CheckItem(
                    BankResponseInfo.PaymentsSystemCodeJsonName,
                    value => result.PaymentCode = (PaymentsSystemResponseCode?) (int.TryParse(value, out var parsed) ? parsed : (int?) null));
                items.CheckItem(
                    BankResponseInfo.ProcessingCenterCodeJsonName,
                    value => result.ProcessingCode = (ProcessingCenterResponseCode?) (int.TryParse(value, out var parsed) ? parsed : (int?) null));
                result.Description = items.LastOrDefault();
            }

            return result;
        }

    }
}
