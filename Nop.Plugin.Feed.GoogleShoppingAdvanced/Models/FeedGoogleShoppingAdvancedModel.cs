using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;
using Nop.Web.Framework;
using Nop.Web.Framework.Mvc;

namespace Nop.Plugin.Feed.GoogleShoppingAdvanced.Models
{
    public class FeedGoogleShoppingAdvancedModel
    {
        public FeedGoogleShoppingAdvancedModel()
        {
            AvailableStores = new List<SelectListItem>();
            AvailableCurrencies = new List<SelectListItem>();
            AvailableCountries = new List<SelectListItem>();
            AvailableGoogleCategories = new List<SelectListItem>();
            GeneratedFiles = new List<GeneratedFileModel>();
        }

        [NopResourceDisplayName("Plugins.Feed.GoogleShoppingAdvanced.ProductPictureSize")]
        public int ProductPictureSize { get; set; }

        [NopResourceDisplayName("Plugins.Feed.GoogleShoppingAdvanced.Store")]
        public int StoreId { get; set; }
        public IList<SelectListItem> AvailableStores { get; set; }

        [NopResourceDisplayName("Plugins.Feed.GoogleShoppingAdvanced.Currency")]
        public int CurrencyId { get; set; }
        public IList<SelectListItem> AvailableCurrencies { get; set; }

        [NopResourceDisplayName("Plugins.Feed.GoogleShoppingAdvanced.DefaultGoogleCategory")]
        public string DefaultGoogleCategory { get; set; }
        public IList<SelectListItem> AvailableGoogleCategories { get; set; }

        [NopResourceDisplayName("Plugins.Feed.GoogleShoppingAdvanced.IsIncludeShippingWeight")]
        public bool IsIncludeShippingWeight { get; set; }

        [NopResourceDisplayName("Plugins.Feed.GoogleShoppingAdvanced.IsUseAdditionalShippingChargeForDelivery")]
        public bool IsUseAdditionalShippingChargeForDelivery { get; set; }

        [NopResourceDisplayName("Plugins.Feed.GoogleShoppingAdvanced.AdditionalShippingChargeCountryId")]
        public int AdditionalShippingChargeCountryId { get; set; }
        public IList<SelectListItem> AvailableCountries { get; set; }

        [NopResourceDisplayName("Plugins.Feed.GoogleShoppingAdvanced.AdditionalShippingChargeServiceName")]
        public string AdditionalShippingChargeServiceName { get; set; }

        [NopResourceDisplayName("Plugins.Feed.GoogleShoppingAdvanced.IsProductIncludedOverride")]
        public bool IsProductIncludedOverride { get; set; }

        public string GenerateFeedResult { get; set; }

        [NopResourceDisplayName("Plugins.Feed.GoogleShoppingAdvanced.StaticFilePath")]
        public IList<GeneratedFileModel> GeneratedFiles { get; set; }
        
        public class GeneratedFileModel : BaseNopModel
        {
            public string StoreName { get; set; }
            public string FileUrl { get; set; }
        }

        public class GoogleAdvancedProductModel : BaseNopModel
        {
            //this attribute is required to disable editing
            [ScaffoldColumn(false)]
            public int ProductId { get; set; }

            //this attribute is required to disable editing
            [ReadOnly(true)]
            [ScaffoldColumn(false)]
            [NopResourceDisplayName("Plugins.Feed.GoogleShoppingAdvanced.Products.ProductName")]
            public string ProductName { get; set; }

            [NopResourceDisplayName("Plugins.Feed.GoogleShoppingAdvanced.Products.GoogleCategory")]
            public string GoogleCategory { get; set; }

            [NopResourceDisplayName("Plugins.Feed.GoogleShoppingAdvanced.Products.Gender")]
            public string Gender { get; set; }

            [NopResourceDisplayName("Plugins.Feed.GoogleShoppingAdvanced.Products.AgeGroup")]
            public string AgeGroup { get; set; }

            [NopResourceDisplayName("Plugins.Feed.GoogleShoppingAdvanced.Products.Color")]
            public string Color { get; set; }

            [NopResourceDisplayName("Plugins.Feed.GoogleShoppingAdvanced.Products.Size")]
            public string GoogleSize { get; set; }

            [NopResourceDisplayName("Plugins.Feed.GoogleShoppingAdvanced.Products.IsIncluded")]
            public bool IsIncluded { get; set; }
        }
    }
}