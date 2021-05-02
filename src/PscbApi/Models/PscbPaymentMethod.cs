// ReSharper disable IdentifierTypo
// ReSharper disable InconsistentNaming
using Newtonsoft.Json;

namespace PscbApi.Models
{

    /// <summary>
    /// PSCB payment methods
    /// </summary>
    [JsonConverter(typeof(IdentifiableEnumConverter))]
    public enum PscbPaymentMethod
    {
        /// <summary>
        /// Bank cards
        /// </summary>
        [Identifier("ac")]
        BankCards,

        /// <summary>
        /// Bank cards (WTF?)
        /// </summary>
        [Identifier("ac-shpa")]
        BankCardsShpa,

        /// <summary>
        /// Yandex money
        /// </summary>
        [Identifier("ym")]
        YandexMoney,

        /// <summary>
        /// Qiwi wallet
        /// </summary>
        [Identifier("qiwi")]
        Qiwi,

        /// <summary>
        /// Web money
        /// </summary>
        [Identifier("wm")]
        WebMoney,

        /// <summary>
        /// Alfa click
        /// </summary>
        [Identifier("alfa")]
        AlfaClick,

        /// <summary>
        /// PSCB Web wallet
        /// </summary>
        [Identifier("ww")]
        PscbWebWallet,

        /// <summary>
        /// PSCB payment terminal
        /// </summary>
        [Identifier("pscb_terminal")]
        PscbTerminal,

        /// <summary>
        /// Payment using mobile phone
        /// </summary>
        [Identifier("mobi-money")]
        Mobile,

        /// <summary>
        /// Fast payments system
        /// </summary>
        [Identifier("sbp")]
        FastPaymentsSystem,
    }
}
