using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using PscbApi.Models;

namespace PscbApi;

/// <summary>
/// PSCB api: https://docs.pscb.ru/oos/
/// </summary>
public class BankApi
{
    private readonly IHttpClientFactory clientFactory;
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
    public BankApi(IHttpClientFactory clientFactory, ApiConfiguration configuration)
    {
        this.clientFactory = clientFactory;
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
        var signature = Convert.ToHexString(SHA256.HashData(requestWithKey));

        var httpClient = clientFactory.CreateClient();
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

    /// <summary>
    /// Creates url with encoded payment data to redirect to perform payment
    /// </summary>
    /// <param name="message">Payment data</param>
    /// <param name="getOrderId">Callback to retrieve order Id after verifying entire message</param>
    /// <param name="getOrderIdDisplayValue">Callback to retrieve order Id display value</param>
    /// <returns>Url to redirect to</returns>
    public async Task<PaymentRequestDescriptor> BuildPaymentRequestAsync(
        PaymentMessage message,
        Func<Task<string>> getOrderId,
        Func<string, string>? getOrderIdDisplayValue = null)
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
                marketPlace = _configuration.MerchantId,
                message = Convert.ToBase64String(messageAsJsonUtf8),
                signature = Convert.ToHexString(SHA256.HashData(messageWithKey)),
            }
        };
        System.Diagnostics.Debug.WriteLineIf(Debug, result.Url);

        return result;
    }

    /// <summary>
    /// Handles payment response and returns parsed response information
    /// </summary>
    /// <param name="description">Description string passed to <see cref="ApiConfiguration.DefaultSuccessUrl"/> or <see cref="ApiConfiguration.DefaultFailUrl"/></param>
    public static BankResponseInfo HandlePaymentResponse(string description)
        => ProtocolHelper.ParseDescriptionString(description);

    /// <summary>
    /// Returns payment information
    /// </summary>
    /// <param name="orderId">Order to return info for</param>
    /// <param name="getCardData">true to read card data</param>
    /// <param name="getFiscalData">true to read fiscal data</param>
    /// <returns>Payment information object</returns>
    /// <remarks>
    /// See https://docs.pscb.ru/oos/api.html#api-dopolnitelnyh-vozmozhnostej-zapros-parametrov-platezha for details
    /// </remarks>
    public async Task<PaymentInfo> GetPaymentInfoAsync(string orderId, bool getCardData = false, bool getFiscalData = false)
    {
        if (string.IsNullOrWhiteSpace(orderId))
        {
            throw new ArgumentNullException(nameof(orderId));
        }

        var queryParams = new PaymentInfoQueryParams
        {
            OrderId = orderId,
            MerchantId = _configuration.MerchantId,
            GetCardData = getCardData,
            GetFiscalData = getFiscalData,
        };

        return await ApiRequestAsync<PaymentInfoQueryParams, PaymentInfo>(
            $"{ActualApiEndpoint}/merchantApi/checkPayment",
            queryParams);
    }

    /// <summary>
    /// Configures recurrent payments with the Fast Payments System
    /// </summary>
    /// <param name="orderId">Order to use as the parent payment</param>
    /// <param name="token">Token that was returned after the successful FPS payment, identified by the <paramref name="orderId"/></param>
    /// <param name="amount">How much money to pay</param>
    /// <param name="purpose">The purpose of payment</param>
    /// <returns>Recurrent payment information object</returns>
    /// <remarks>
    /// See https://docs.pscb.ru/oos/api.html#api-dopolnitelnyh-vozmozhnostej-rekurrentnye-platezhi-sozdanie-rekurrentnogo-platezha-sbp for details
    /// </remarks>
    public async Task<FastPaymentsSystemRecurrentPaymentInfo> SetupFastPaymentSystemRecurrentPayments(string orderId, string token, int amount, string purpose)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(orderId, nameof(orderId));
        ArgumentException.ThrowIfNullOrWhiteSpace(token, nameof(token));
        ArgumentException.ThrowIfNullOrWhiteSpace(purpose, nameof(purpose));

        var queryParams = new SetupFastPaymentsSystemRecurrentPaymentQueryParams
        {
            Amount = amount,
            PaymentPurpose = purpose,
            OrderId = orderId,
            RecurrencyToken = token,
            MarketplaceId = _configuration.MerchantId,
        };

        return await ApiRequestAsync<SetupFastPaymentsSystemRecurrentPaymentQueryParams, FastPaymentsSystemRecurrentPaymentInfo>(
            $"{ActualApiEndpoint}/merchantApi/createQrCode",
            queryParams);
    }

    public async Task<FastPaymentsSystemRecurrentPaymentInfo> CancelFastPaymentSystemRecurrentPayments(string orderId, string token)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(orderId, nameof(orderId));
        ArgumentException.ThrowIfNullOrWhiteSpace(token, nameof(token));

        var queryParams = new CancelFastPaymentsSystemRecurrentPaymentQueryParams
        {
            OrderId = orderId,
            RecurrencyToken = token,
            MarketplaceId = _configuration.MerchantId,
        };

        return await ApiRequestAsync<CancelFastPaymentsSystemRecurrentPaymentQueryParams, FastPaymentsSystemRecurrentPaymentInfo>(
            $"{ActualApiEndpoint}/merchantApi/cancelRecurrent",
            queryParams);
    }

    /// <summary>
    /// Initiates new recurrent payment.
    /// </summary>
    /// <param name="parentOrderId">Order used as a parent payment</param>
    /// <param name="paymentId">Identifier of a payment to perform</param>
    /// <param name="token">Recurrency token</param>
    /// <param name="additionalToken">Additional token (like Fast Payment System QR-code identifier)</param>
    /// <param name="receipt">Receipt data</param>
    public async Task<PaymentInfoBase> PayRecurrent(string parentOrderId, string paymentId, string token, string additionalToken, Receipt receipt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(parentOrderId, nameof(parentOrderId));
        ArgumentException.ThrowIfNullOrWhiteSpace(paymentId, nameof(paymentId));
        ArgumentException.ThrowIfNullOrWhiteSpace(token, nameof(token));
        ArgumentException.ThrowIfNullOrWhiteSpace(additionalToken, nameof(additionalToken));
        ArgumentNullException.ThrowIfNull(receipt, nameof(receipt));

        var message = new RecurrentPaymentMessage
        {
            MerchantId = _configuration.MerchantId,
            ParentOrderId = parentOrderId,
            OrderId = paymentId,
            RecurrencyToken = token,
            Amount = receipt.Items.Aggregate(0.0M, static (v, item) => v + item.TotalPrice),
            Data = new PaymentMessageData
            {
                AdditionalToken = token,
                Receipt = receipt
            }
        };

        return await ApiRequestAsync<RecurrentPaymentMessage, PaymentInfoBase>(
            $"{ActualApiEndpoint}/merchantApi/payRecurrent",
            message);
    }

    /// <summary>
    /// Initiates new refund.
    /// </summary>
    /// <param name="paymentId">Payment to refund</param>
    /// <param name="partial">true when partial refund is required</param>
    /// <param name="amount">Money to refund in partial refund</param>
    /// <param name="receipt">Receipt data for partial refunds</param>
    /// <returns>Refund information</returns>
    /// <remarks>
    /// See https://docs.pscb.ru/oos/api.html#api-dopolnitelnyh-vozmozhnostej-vozvrat-platezha for details.
    /// </remarks>
    public async Task<RefundInfo> Refund(string paymentId, bool partial, int? amount, Receipt? receipt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(paymentId, nameof(paymentId));
        if (partial)
        {
            ArgumentNullException.ThrowIfNull(amount, nameof(amount));
            ArgumentNullException.ThrowIfNull(receipt, nameof(receipt));
        }

        var message = new RefundMessage
        {
            MerchantId = _configuration.MerchantId,
            OrderId = paymentId,
            PartialRefund = partial,
            Amount = partial ? amount : null,
            Data = new RefundMessageData
            {
                Receipt = partial ? receipt : null,
            }
        };

        return await ApiRequestAsync<RefundMessage, RefundInfo>(
            $"{ActualApiEndpoint}/merchantApi/payRecurrent",
            message);
    }
}
