using Nop.Core.Configuration;

namespace Nop.Plugin.Feed.GoogleShoppingAdvanced
{
    public class FeedGoogleShoppingAdvancedSettings : ISettings
    {
        /// <summary>
        /// Product picture size
        /// </summary>
        public int ProductPictureSize { get; set; }

        /// <summary>
        /// Store identifier for which feed file(s) will be generated
        /// </summary>
        public int StoreId { get; set; }
        /// <summary>
        /// Currency identifier for which feed file(s) will be generated
        /// </summary>
        public int CurrencyId { get; set; }

        /// <summary>
        /// A value indicating whether we should pass shipping weight
        /// </summary>
        public bool IsIncludeShippingWeight { get; set; }

        /// <summary>
        /// Default Google category
        /// </summary>
        public string DefaultGoogleCategory { get; set; }

        /// <summary>
        /// Use product additional shipping charge for delivery estimate
        /// </summary>
        public bool IsUseAdditionalShippingChargeForDelivery { get; set; }

        /// <summary>
        /// Additional shipping charge country code (ISO 3166)
        /// </summary>
        public int AdditionalShippingChargeCountryId { get; set; }

        /// <summary>
        /// Additional shipping charge service name
        /// </summary>
        public string AdditionalShippingChargeServiceName { get; set; }

        /// <summary>
        /// Override product is included in feed setting
        /// </summary>
        public bool IsProductIncludedOverride { get; set; }
       
        /// <summary>
        /// Static Froogle file name
        /// </summary>
        public string StaticFileName { get; set; }
    }
}