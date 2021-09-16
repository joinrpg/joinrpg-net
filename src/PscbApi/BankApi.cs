using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Newtonsoft.Json;
using PscbApi.Models;

namespace PscbApi
{
    /// <summary>
    /// PSCB api: https://docs.pscb.ru/oos/
    /// </summary>
    public class BankApi
    {
        private readonly ApiConfiguration _configuration;

        private readonly byte[] _keyAsUtf8;

        /// <summary>
        /// true if API is configured to use debug endpoint
        /// </summary>
        public bool Debug => _configuration.Debug;

        /// <summary>
        /// Returns actual API endpoint
        /// </summary>
        public string ActualApiEndpoint => Debug ? _configuration.ApiDebugEndpoint : _configuration.ApiEndpoint;

        /// <summary>
        /// Returns actual API key
        /// </summary>
        public string ActualApiKey => Debug ? _configuration.ApiDebugKey : _configuration.ApiKey;


        /// <summary>
        /// Creates new instance of PSCB API object
        /// </summary>
        public BankApi(ApiConfiguration configuration)
        {
            _configuration = configuration;
            _keyAsUtf8 = ActualApiKey.ToUtf8Bytes();
        }

        /// <summary>
        /// Performs generic api request
        /// </summary>
        /// <typeparam name="TRequest">Type of request object</typeparam>
        /// <typeparam name="TResponse">Type of response object</typeparam>
        /// <param name="url">URL to send request to</param>
        /// <param name="request">Request object</param>
        /// <returns>Deserialized response</returns>
        protected async Task<TResponse> ApiRequestAsync<TRequest, TResponse>(string url, TRequest request)
            where TRequest : class
            where TResponse : class, new()
        {
            System.Diagnostics.Debug.WriteLineIf(Debug, url);
            System.Diagnostics.Debug.WriteLineIf(Debug, JsonConvert.SerializeObject(request, Formatting.Indented));

            var requestAsJson = JsonConvert.SerializeObject(request, Formatting.None);
            var requestAsJsonUtf8 = requestAsJson.ToUtf8Bytes();
            var requestWithKey = requestAsJsonUtf8.Concat(_keyAsUtf8).ToArray();
            var signature = requestWithKey.Sha256Encode().ToHexString();

            using (var httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Add("Signature", signature);
                HttpResponseMessage httpResponse = await httpClient.PostAsync(
                    url,
                    new StringContent(Encoding.UTF8.GetString(requestAsJsonUtf8), Encoding.UTF8));
                if (httpResponse.StatusCode == HttpStatusCode.OK)
                {
                    var responseJson = await httpResponse.Content.ReadAsStringAsync();
                    System.Diagnostics.Debug.WriteLineIf(Debug, responseJson);
                    return JsonConvert.DeserializeObject<TResponse>(responseJson);
                }

                throw new PscbApiRequestException<TRequest>(url, request, signature, $"{httpResponse.StatusCode} {httpResponse.ReasonPhrase}");
            }
        }

        /// <summary>
        /// Creates url with encoded payment data to redirect to perform payment
        /// </summary>
        /// <param name="message">Payment data</param>
        /// <param name="getOrderId">Callback to retrieve order Id after verifying entire message</param>
        /// <param name="getOrderIdDisplayValue">Callback to retrieve order Id display value</param>
        /// <returns>Url to redirect to</returns>
        public async Task<PaymentRequestDescriptor> BuildPaymentRequestAsync(
            PaymentMessage message,
            [NotNull] Func<Task<string>> getOrderId,
            Func<string, string> getOrderIdDisplayValue = null)
        {
            if (Debug)
            {
                message.Details = "[Debug mode] " + message.Details;
            }

            if (message.PaymentMethod.HasValue)
            {
                message.Details = ProtocolHelper.PreparePaymentPurposeString(
                    message.Details,
                    message.PaymentMethod.Value);
            }
            message.CheckStringPropertyLength(m => m.Details);
            message.CheckStringPropertyLength(m => m.CustomerComment);
            message.CheckStringPropertyLength(m => m.OrderIdDisplayValue);
            foreach (var receiptItem in message.Data.Receipt.Items)
            {
                if (message.PaymentMethod.HasValue)
                {
                    receiptItem.Name = ProtocolHelper.PreparePaymentPurposeString(
                        receiptItem.Name,
                        message.PaymentMethod.Value);
                }
                receiptItem.CheckStringPropertyLength(r => r.Name);
            }
            message.SuccessUrl ??= _configuration.DefaultSuccessUrl;
            message.FailUrl ??= _configuration.DefaultFailUrl;
            Validator.ValidateObject(message, new ValidationContext(message) { MemberName = nameof(message) });

            message.OrderId = await getOrderId() ?? message.OrderId;
            message.ValidateProperty(m => m.OrderId);

            message.OrderIdDisplayValue = getOrderIdDisplayValue?.Invoke(message.OrderId) ?? message.OrderIdDisplayValue;
            message.ValidateProperty(m => m.OrderIdDisplayValue);

            System.Diagnostics.Debug.WriteLineIf(
                Debug,
                JsonConvert.SerializeObject(message, Formatting.Indented));

            var messageAsJson = JsonConvert.SerializeObject(message, Formatting.None);
            var messageAsJsonUtf8 = messageAsJson.ToUtf8Bytes();
            var messageWithKey = messageAsJsonUtf8.Concat(_keyAsUtf8).ToArray();

            var result = new PaymentRequestDescriptor
            {
                Debug = Debug,
                Url = $"{ActualApiEndpoint}/pay",
                Request = new PaymentRequest
                {
                    marketPlace = message.PaymentMethod == PscbPaymentMethod.FastPaymentsSystem
                        ? _configuration.MerchantIdFastPayments ?? _configuration.MerchantId
                        : _configuration.MerchantId,
                    message = Convert.ToBase64String(messageAsJsonUtf8),
                    signature = messageWithKey.Sha256Encode().ToHexString()
                }
            };
            System.Diagnostics.Debug.WriteLineIf(Debug, result.Url);

            return result;
        }

        /// <summary>
        /// Handles payment response and returns parsed response information
        /// </summary>
        /// <param name="description">Description string passed to <see cref="ApiConfiguration.DefaultSuccessUrl"/> or <see cref="ApiConfiguration.DefaultFailUrl"/></param>
        public BankResponseInfo HandlePaymentResponse(string description)
            => ProtocolHelper.ParseDescriptionString(description);

        /// <summary>
        /// Returns payment information
        /// </summary>
        /// <param name="paymentMethod">Payment method was used for this payment</param>
        /// <param name="orderId">Order to return info for</param>
        /// <param name="getCardData">true to read card data</param>
        /// <param name="getFiscalData">true to read fiscal data</param>
        /// <returns>Payment information object</returns>
        /// <remarks>
        /// See https://docs.pscb.ru/oos/api.html#api-dopolnitelnyh-vozmozhnostej-zapros-parametrov-platezha for details
        /// </remarks>
        public async Task<PaymentInfo> GetPaymentInfoAsync(PscbPaymentMethod paymentMethod, string orderId, bool getCardData = false, bool getFiscalData = false)
        {
            if (string.IsNullOrWhiteSpace(orderId))
            {
                throw new ArgumentNullException(nameof(orderId));
            }

            var queryParams = new PaymentInfoQueryParams
            {
                OrderId = orderId,
                MerchantId = paymentMethod == PscbPaymentMethod.FastPaymentsSystem
                    ? _configuration.MerchantIdFastPayments ?? _configuration.MerchantId
                    : _configuration.MerchantId,
                GetCardData = getCardData,
                GetFiscalData = getFiscalData,
            };

            return await ApiRequestAsync<PaymentInfoQueryParams, PaymentInfo>(
                $"{ActualApiEndpoint}/merchantApi/checkPayment",
                queryParams);
        }
    }
}
