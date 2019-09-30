using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using PscbApi.Helpers;
using PscbApi.Models;

namespace PscbApi
{
    /// <summary>
    /// PSCB api: https://docs.pscb.ru/oos/
    /// </summary>
    public class PscbApi
    {

        private const string ApiEndpoint = "https://oos.pscb.ru";

        private const string ApiDebugEndpoint = "https://oosdemo.pscb.ru";

        private readonly ApiConfiguration _configuration;

        private readonly byte[] _keyAsUtf8;

        /// <summary>
        /// true if API is configured to use debug endpoint
        /// </summary>
        public bool Debug => _configuration.Debug;

        /// <summary>
        /// Returns actual API endpoint
        /// </summary>
        public string ActualApiEndpoint => Debug ? ApiDebugEndpoint : ApiEndpoint;

        /// <summary>
        /// Returns actual API key
        /// </summary>
        public string ActualApiKey => Debug ? _configuration.ApiDebugKey : _configuration.ApiKey;


        /// <summary>
        /// Creates new instance of PSCB API object
        /// </summary>
        public PscbApi(ApiConfiguration configuration)
        {
            _configuration = configuration;
            _keyAsUtf8 = Encoding.Convert(
                Encoding.Default,
                Encoding.UTF8,
                Encoding.Default.GetBytes(_configuration.ApiKey));
        }

        /// <summary>
        /// Creates url with encoded payment data to redirect to perform payment
        /// </summary>
        /// <param name="message">Payment data</param>
        /// <returns>Url to redirect to</returns>
        public string BuildPaymentUrl(PaymentMessage message)
        {
            message.SuccessUrl = _configuration.DefaultSuccessUrl ?? message.SuccessUrl;
            message.FailUrl = _configuration.DefaultFailUrl ?? message.FailUrl;
            Validator.ValidateObject(message, new ValidationContext(message) { MemberName = nameof(message) });
            byte[] messageAsJsonUtf8 = Encoding.Convert(
                Encoding.Default,
                Encoding.UTF8,
                Encoding.Default.GetBytes(JsonConvert.SerializeObject(
                    message,
                    Debug ? Formatting.Indented : Formatting.None)));
            var queryParams = new PaymentQueryParams
            {
                marketPlace = _configuration.MerchantId,
                message = Convert.ToBase64String(messageAsJsonUtf8),
                signature = messageAsJsonUtf8.Union(_keyAsUtf8).Sha256Encode().ToHexString()
            };

            return $"{ActualApiEndpoint}/pay?{queryParams}";
        }

        /// <summary>
        /// Handles payment response and returns parsed response information
        /// </summary>
        /// <param name="description">Description string passed to <see cref="ApiConfiguration.DefaultSuccessUrl"/> or <see cref="ApiConfiguration.DefaultFailUrl"/></param>
        public ResponseInfo HandlePaymentResponse(string description)
            => ProtocolHelper.ParseDescriptionString(description);
    }

    /// <summary>
    /// Configuration for <see cref="PscbApi"/>
    /// </summary>
    public struct ApiConfiguration
    {
        /// <summary>
        /// Set to true to use debug api endpoint. <see cref="ApiKey"/> must contain debug key
        /// </summary>
        public bool Debug;

        /// <summary>
        /// Api key (issued by PSCB)
        /// </summary>
        public string ApiKey;

        /// <summary>
        /// Api key to access debug endpoint (issued by PSCB)
        /// </summary>
        public string ApiDebugKey;

        /// <summary>
        /// Merchant Id (issued by PSCB)
        /// </summary>
        public string MerchantId;

        /// <summary>
        /// Default url for successful payments
        /// </summary>
        public string DefaultSuccessUrl;

        /// <summary>
        /// Default url for failed payments
        /// </summary>
        public string DefaultFailUrl;
    }
}
