using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PscbApi.Models
{
    internal class PaymentQueryParams
    {
        public string marketPlace;

        public string message;

        public string signature;

        /// <inheritdoc />
        public override string ToString()
        {
            var parts = new Dictionary<string, string>
            {
                {nameof(marketPlace), marketPlace},
                {nameof(message), message},
                {nameof(signature), signature}
            };
            return string.Join("&", parts.Select(kv => $"{kv.Key}={kv.Value}"));
        }
    }
}
