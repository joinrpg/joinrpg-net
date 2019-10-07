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
            byte[] requestAsJsonUtf8 = requestAsJson.ToUtf8Bytes();
            byte[] requestWithKey = requestAsJsonUtf8.Concat(_keyAsUtf8).ToArray();
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
            message.CheckStringPropertyLength(m => m.Details);
            message.CheckStringPropertyLength(m => m.CustomerComment);
            message.CheckStringPropertyLength(m => m.OrderIdDisplayValue);
            foreach (var receiptItem in message.Data.Receipt.Items)
            {
                receiptItem.CheckStringPropertyLength(r => r.Name);
            }
            message.SuccessUrl = message.SuccessUrl ?? _configuration.DefaultSuccessUrl;
            message.FailUrl = message.FailUrl ?? _configuration.DefaultFailUrl;
            Validator.ValidateObject(message, new ValidationContext(message) { MemberName = nameof(message) });

            message.OrderId = await getOrderId() ?? message.OrderId;
            message.ValidateProperty(m => m.OrderId);

            message.OrderIdDisplayValue = getOrderIdDisplayValue?.Invoke(message.OrderId) ?? message.OrderIdDisplayValue;
            message.ValidateProperty(m => m.OrderIdDisplayValue);

            System.Diagnostics.Debug.WriteLineIf(
                Debug,
                JsonConvert.SerializeObject(message, Formatting.Indented));

            var messageAsJson = JsonConvert.SerializeObject(message, Formatting.None);
            byte[] messageAsJsonUtf8 = messageAsJson.ToUtf8Bytes();
            byte[] messageWithKey = messageAsJsonUtf8.Concat(_keyAsUtf8).ToArray();

            var result = new PaymentRequestDescriptor
            {
                Debug = Debug,
                Url = $"{ActualApiEndpoint}/pay",
                QueryParams = new PaymentQueryParams
                {
                    marketPlace = _configuration.MerchantId,
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
        /// Asks API endpoint for payment information
        /// </summary>
        /// <param name="query">Query object</param>
        /// <returns>Payment information object</returns>
        /// <remarks>
        /// See https://docs.pscb.ru/oos/api.html#api-dopolnitelnyh-vozmozhnostej-zapros-parametrov-platezha for details
        /// </remarks>
        public async Task<PaymentInfo> GetPaymentInfoAsync(PaymentInfoQuery query)
        {
            if (query == null)
                throw new ArgumentNullException(nameof(query));
            query.MerchantId = _configuration.MerchantId;
            Validator.ValidateObject(query, new ValidationContext(query));

            return await ApiRequestAsync<PaymentInfoQuery, PaymentInfo>($"{ActualApiEndpoint}/merchantApi/checkPayment", query);
        }
    }
}
