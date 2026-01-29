using Microsoft.Extensions.Configuration;

namespace PscbApi;

/// <summary>
/// Read configuration
/// </summary>
public class ConfigurationAdapter(IConfiguration configuration) : IBankSecretsProvider
{
    bool IBankSecretsProvider.Debug => configuration.GetValue<bool>("PaymentProviders:Pscb:Debug");
    bool IBankSecretsProvider.DebugOutput => configuration.GetValue<bool>("PaymentProviders:Pscb:DebugOutput");
    string IBankSecretsProvider.ApiEndpoint => configuration.GetValue<string>("PaymentProviders:Pscb:BankApiEndpoint")!;
    string IBankSecretsProvider.ApiDebugEndpoint => configuration.GetValue<string>("PaymentProviders:Pscb:BankApiDebugEndpoint")!;
    string IBankSecretsProvider.MerchantId => configuration.GetValue<string>("PaymentProviders:Pscb:BankMerchantId")!;
    string IBankSecretsProvider.ApiKey => configuration.GetValue<string>("PaymentProviders:Pscb:BankApiKey")!;
    string IBankSecretsProvider.ApiDebugKey => configuration.GetValue<string>("PaymentProviders:Pscb:BankApiDebugKey")!;
    string IBankSecretsProvider.BankSystemPaymentUrl => configuration.GetValue<string>("PaymentProviders:Pscb:BankSystemPaymentUrl")!;
    string IBankSecretsProvider.BankSystemDebugPaymentUrl => configuration.GetValue<string>("PaymentProviders:Pscb:BankSystemDebugPaymentUrl")!;
}
