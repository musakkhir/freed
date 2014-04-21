using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Routing;
using System.Xml;
using Nop.Core;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Directory;
using Nop.Core.Domain.Stores;
using Nop.Core.Domain.Tasks;
using Nop.Core.Plugins;
using Nop.Plugin.Feed.GoogleShoppingAdvanced.Data;
using Nop.Plugin.Feed.GoogleShoppingAdvanced.Services;
using Nop.Services.Catalog;
using Nop.Services.Common;
using Nop.Services.Configuration;
using Nop.Services.Directory;
using Nop.Services.Localization;
using Nop.Services.Media;
using Nop.Services.Seo;
using Nop.Services.Shipping;
using Nop.Services.Tasks;

namespace Nop.Plugin.Feed.GoogleShoppingAdvanced
{
    public class FeedGoogleShoppingAdvancedService : BasePlugin, IMiscPlugin
    {
        #region Fields

        private readonly CurrencySettings _currencySettings;
        private readonly FeedGoogleShoppingAdvancedSettings _feedGoogleShoppingAdvancedSettings;
        private readonly GoogleAdvancedProductObjectContext _objectContext;
        private readonly ICategoryService _categoryService;
        private readonly ICountryService _countryService;
        private readonly ICurrencyService _currencyService;
        private readonly IGoogleAdvancedService _googleAdvancedService;
        private readonly IManufacturerService _manufacturerService;
        private readonly IMeasureService _measureService;
        private readonly IPictureService _pictureService;
        private readonly IProductService _productService;
        private readonly IScheduleTaskService _scheduleTaskService;
        private readonly ISettingService _settingService;
        private readonly IShippingService _shippingService;
        private readonly IWorkContext _workContext;
        private readonly MeasureSettings _measureSettings;

        #endregion

        #region Ctor
        public FeedGoogleShoppingAdvancedService(CurrencySettings currencySettings,
            FeedGoogleShoppingAdvancedSettings feedGoogleShoppingAdvancedSettings,
            GoogleAdvancedProductObjectContext objectContext,
            ICategoryService categoryService,
            ICountryService countryService,
            ICurrencyService currencyService,
            IGoogleAdvancedService googleAdvancedService,
            IManufacturerService manufacturerService,
            IMeasureService measureService,
            IPictureService pictureService,
            IProductService productService,
            IScheduleTaskService scheduleTaskService,
            ISettingService settingService,
            IShippingService shippingService,
            IWorkContext workContext,
            MeasureSettings measureSettings)
        {
            this._currencySettings = currencySettings;
            this._feedGoogleShoppingAdvancedSettings = feedGoogleShoppingAdvancedSettings;
            this._objectContext = objectContext;
            this._categoryService = categoryService;
            this._countryService = countryService;
            this._currencyService = currencyService;
            this._googleAdvancedService = googleAdvancedService;
            this._manufacturerService = manufacturerService;
            this._measureSettings = measureSettings;
            this._pictureService = pictureService;
            this._productService = productService;
            this._scheduleTaskService = scheduleTaskService;
            this._settingService = settingService;
            this._shippingService = shippingService;
            this._workContext = workContext;
            this._measureService = measureService;
        }

        #endregion

        #region Utilities
        /// <summary>
        /// Removes invalid characters
        /// </summary>
        /// <param name="input">Input string</param>
        /// <param name="isHtmlEncoded">A value indicating whether input string is HTML encoded</param>
        /// <returns>Valid string</returns>
        private string StripInvalidChars(string input, bool isHtmlEncoded)
        {
            if (String.IsNullOrWhiteSpace(input))
                return input;

            //Microsoft uses a proprietary encoding (called CP-1252) for the bullet symbol and some other special characters, 
            //whereas most websites and data feeds use UTF-8. When you copy-paste from a Microsoft product into a website, 
            //some characters may appear as junk. Our system generates data feeds in the UTF-8 character encoding, 
            //which many shopping engines now require.

            //http://www.atensoftware.com/p90.php?q=182

            if (isHtmlEncoded)
                input = HttpUtility.HtmlDecode(input);

            input = input.Replace("¼", "");
            input = input.Replace("½", "");
            input = input.Replace("¾", "");
            //input = input.Replace("•", "");
            //input = input.Replace("”", "");
            //input = input.Replace("“", "");
            //input = input.Replace("’", "");
            //input = input.Replace("‘", "");
            //input = input.Replace("™", "");
            //input = input.Replace("®", "");
            //input = input.Replace("°", "");
            
            if (isHtmlEncoded)
            {
                //Strip out HTML characters
                //SOURCE: http://stackoverflow.com/questions/19523913/remove-html-tags-from-string-including-nbsp-in-c-sharp
                input = Regex.Replace(Regex.Replace(input, @"<[^>]+>|&nbsp;", "").Trim(), @"\s{2,}", " ");
            }

            return input;
        }
        private Currency GetUsedCurrency()
        {
            var currency = _currencyService.GetCurrencyById(_feedGoogleShoppingAdvancedSettings.CurrencyId);
            if (currency == null || !currency.Published)
                currency = _currencyService.GetCurrencyById(_currencySettings.PrimaryStoreCurrencyId);
            return currency;
        }
        private Country GetUsedCountry()
        {
            var country = _countryService.GetCountryById(_feedGoogleShoppingAdvancedSettings.AdditionalShippingChargeCountryId);
            //TODO (LFW): Handle null/empty country
            //if (country == null || !country.Published)
            //    country = _countryService.GetCountryById();
            return country;
        }

        
        #endregion

        #region Methods

        /// <summary>
        /// Gets a route for provider configuration
        /// </summary>
        /// <param name="actionName">Action name</param>
        /// <param name="controllerName">Controller name</param>
        /// <param name="routeValues">Route values</param>
        public void GetConfigurationRoute(out string actionName, out string controllerName, out RouteValueDictionary routeValues)
        {
            actionName = "Configure";
            controllerName = "FeedGoogleShoppingAdvanced";
            routeValues = new RouteValueDictionary() { { "Namespaces", "Nop.Plugin.Feed.GoogleShoppingAdvanced.Controllers" }, { "area", null } };
        }

        /// <summary>
        /// Generate a feed
        /// </summary>
        /// <param name="stream">Stream</param>
        /// <param name="store">Store</param>
        /// <returns>Generated feed</returns>
        public void GenerateFeed(Stream stream, Store store)
        {
            if (stream == null)
                throw new ArgumentNullException("stream");

            if (store == null)
                throw new ArgumentNullException("store");

            const string googleBaseNamespace = "http://base.google.com/ns/1.0";

            var settings = new XmlWriterSettings
            {
                Encoding = Encoding.UTF8
            };
            using (var writer = XmlWriter.Create(stream, settings))
            {
                //Generate feed according to the following specs: http://www.google.com/support/merchants/bin/answer.py?answer=188494&expand=GB
                writer.WriteStartDocument();
                writer.WriteStartElement("rss");
                writer.WriteAttributeString("version", "2.0");
                writer.WriteAttributeString("xmlns", "g", null, googleBaseNamespace);
                writer.WriteStartElement("channel");
                writer.WriteElementString("title", "Google Base feed");
                writer.WriteElementString("link", "http://base.google.com/base/");
                writer.WriteElementString("description", "Information about products");

                var products1 = _productService.SearchProducts(storeId: store.Id,
                visibleIndividuallyOnly: true);
                foreach (var product1 in products1)
                {
                    var productsToProcess = new List<Product>();
                    switch (product1.ProductType)
                    {
                        case ProductType.SimpleProduct:
                            {
                                //simple product doesn't have child products
                                productsToProcess.Add(product1);
                            }
                            break;
                        case ProductType.GroupedProduct:
                            {
                                //grouped products could have several child products
                                var associatedProducts = _productService.SearchProducts(
                                    storeId: store.Id,
                                    visibleIndividuallyOnly: false,
                                    parentGroupedProductId: product1.Id
                                    );
                                productsToProcess.AddRange(associatedProducts);
                            }
                            break;
                        default:
                            continue;
                    }
                    //REM: Only products with 'Product type': 'Simple product' will be included
                    foreach (var product in productsToProcess)
                    {
                        var googleProduct = _googleAdvancedService.GetByProductId(product.Id);

                        if (googleProduct != null && googleProduct.IsIncluded || _feedGoogleShoppingAdvancedSettings.IsProductIncludedOverride)
                        {
                            //Set price values to use 2 decimal places rather than 4 to comply with feed specification
                            NumberFormatInfo nfi = new CultureInfo("en-US", false).NumberFormat;
                            nfi.CurrencyDecimalDigits = 2;

                            writer.WriteStartElement("item");

                            #region Basic Product Information

                            //id [id]- An identifier of the item
                            writer.WriteElementString("g", "id", googleBaseNamespace, product.Id.ToString());

                            //title [title] - Title of the item
                            writer.WriteStartElement("title");
                            var title = product.Name;
                            //title should be not longer than 70 characters
                            if (title.Length > 70)
                                title = title.Substring(0, 70);
                            writer.WriteCData(title);
                            writer.WriteEndElement(); // title

                            //description [description] - Description of the item
                            writer.WriteStartElement("description");
                            string description = string.Empty;
                            description = product.FullDescription;
                            if (String.IsNullOrEmpty(description))
                                description = product.ShortDescription;
                            if (String.IsNullOrEmpty(description))
                                description = string.Empty;

                            if(product.ParentGroupedProductId != 0)
                            {
                                if (_feedGoogleShoppingAdvancedSettings.IsUseParentGroupedProductDescription || description.Length <= _feedGoogleShoppingAdvancedSettings.MinProductDescriptionCharLimit)
                                {
                                    var parentGroupedProduct = _productService.GetProductById(product.ParentGroupedProductId);
                                    description = parentGroupedProduct.FullDescription;
                                    if (String.IsNullOrEmpty(description))
                                        description = parentGroupedProduct.ShortDescription;
                                }
                            }

                            if (String.IsNullOrEmpty(description))
                                description = product.Name; //description is required

                            //resolving character encoding issues in your data feed
                            description = StripInvalidChars(description, true);
                            writer.WriteCData(description);
                            writer.WriteEndElement(); // description



                            //google product category [google_product_category] - Google's category of the item
                            //the category of the product according to Google’s product taxonomy. http://www.google.com/support/merchants/bin/answer.py?answer=160081
                            string googleProductCategory = "";
                            if (googleProduct != null)
                                googleProductCategory = googleProduct.Taxonomy;
                            if (String.IsNullOrEmpty(googleProductCategory))
                                googleProductCategory = _feedGoogleShoppingAdvancedSettings.DefaultGoogleCategory;
                            // TODO (LFW): Add form field validation for Default Google category to save & generate buttons
                            if (String.IsNullOrEmpty(googleProductCategory))
                                throw new NopException("Default Google category is not set");
                            writer.WriteStartElement("g", "google_product_category", googleBaseNamespace);
                            writer.WriteCData(googleProductCategory);
                            writer.WriteFullEndElement(); // g:google_product_category

                            //product type [product_type] - Your category of the item
                            var defaultProductCategory = _categoryService.GetProductCategoriesByProductId(product.Id).FirstOrDefault();
                            if (defaultProductCategory != null)
                            {
                                var category = defaultProductCategory.Category.GetFormattedBreadCrumb(_categoryService, separator: ">");
                                if (!String.IsNullOrEmpty((category)))
                                {
                                    writer.WriteStartElement("g", "product_type", googleBaseNamespace);
                                    writer.WriteCData(category);
                                    writer.WriteFullEndElement(); // g:product_type
                                }
                            }

                            //link [link] - URL directly linking to your item's page on your website
                            var productUrl = string.Format("{0}{1}", store.Url, product.GetSeName(_workContext.WorkingLanguage.Id));
                            writer.WriteElementString("link", productUrl);

                            //image link [image_link] - URL of an image of the item
                            string imageUrl;
                            var picture = _pictureService.GetPicturesByProductId(product.Id, 1).FirstOrDefault();

                            if (picture != null)
                                imageUrl = _pictureService.GetPictureUrl(picture, _feedGoogleShoppingAdvancedSettings.ProductPictureSize, storeLocation: store.Url);
                            else
                                imageUrl = _pictureService.GetDefaultPictureUrl(_feedGoogleShoppingAdvancedSettings.ProductPictureSize, storeLocation: store.Url);

                            writer.WriteElementString("g", "image_link", googleBaseNamespace, imageUrl);

                            //condition [condition] - Condition or state of the item
                            writer.WriteElementString("g", "condition", googleBaseNamespace, "new");

                            writer.WriteElementString("g", "expiration_date", googleBaseNamespace, DateTime.Now.AddDays(28).ToString("yyyy-MM-dd"));

                            #endregion

                            #region Availability & Price

                            //availability [availability] - Availability status of the item
                            string availability = "in stock"; //in stock by default
                            if (product.ManageInventoryMethod == ManageInventoryMethod.ManageStock
                                && product.StockQuantity <= 0)
                            {
                                switch (product.BackorderMode)
                                {
                                    case BackorderMode.NoBackorders:
                                        {
                                            availability = "out of stock";
                                        }
                                        break;
                                    case BackorderMode.AllowQtyBelow0:
                                    case BackorderMode.AllowQtyBelow0AndNotifyCustomer:
                                        {
                                            availability = "available for order";
                                            //availability = "preorder";
                                        }
                                        break;
                                    default:
                                        break;
                                }
                            }
                            writer.WriteElementString("g", "availability", googleBaseNamespace, availability);

                            //price [price] - Price of the item
                            var currency = GetUsedCurrency();
                            decimal price = _currencyService.ConvertFromPrimaryStoreCurrency(product.Price, currency);
                            writer.WriteElementString("g", "price", googleBaseNamespace,
                                                      price.ToString(nfi) + " " +
                                                      currency.CurrencyCode);

                            #endregion

                            #region Unique Product Identifiers

                            /* Unique product identifiers such as UPC, EAN, JAN or ISBN allow us to show your listing on the appropriate product page. If you don't provide the required unique product identifiers, your store may not appear on product pages, and all your items may be removed from Product Search.
                             * We require unique product identifiers for all products - except for custom made goods. For apparel, you must submit the 'brand' attribute. For media (such as books, movies, music and video games), you must submit the 'gtin' attribute. In all cases, we recommend you submit all three attributes.
                             * You need to submit at least two attributes of 'brand', 'gtin' and 'mpn', but we recommend that you submit all three if available. For media (such as books, movies, music and video games), you must submit the 'gtin' attribute, but we recommend that you include 'brand' and 'mpn' if available.
                            */

                            //GTIN [gtin] - GTIN
                            var gtin = product.Gtin;
                            if (!String.IsNullOrEmpty(gtin))
                            {
                                writer.WriteStartElement("g", "gtin", googleBaseNamespace);
                                writer.WriteCData(gtin);
                                writer.WriteFullEndElement(); // g:gtin
                            }

                            //brand [brand] - Brand of the item
                            var defaultManufacturer =
                                _manufacturerService.GetProductManufacturersByProductId((product.Id)).FirstOrDefault();
                            if (defaultManufacturer != null)
                            {
                                writer.WriteStartElement("g", "brand", googleBaseNamespace);
                                writer.WriteCData(defaultManufacturer.Manufacturer.Name);
                                writer.WriteFullEndElement(); // g:brand
                            }


                            //mpn [mpn] - Manufacturer Part Number (MPN) of the item
                            var mpn = product.ManufacturerPartNumber;
                            if (!String.IsNullOrEmpty(mpn))
                            {
                                writer.WriteStartElement("g", "mpn", googleBaseNamespace);
                                writer.WriteCData(mpn);
                                writer.WriteFullEndElement(); // g:mpn
                            }

                            #endregion

                            #region Apparel Products

                            /* Apparel includes all products that fall under 'Apparel & Accessories' (including all sub-categories)
                             * in Google’s product taxonomy.
                            */

                            //gender [gender] - Gender of the item
                            if (googleProduct != null && !String.IsNullOrEmpty(googleProduct.Gender))
                            {
                                writer.WriteStartElement("g", "gender", googleBaseNamespace);
                                writer.WriteCData(googleProduct.Gender);
                                writer.WriteFullEndElement(); // g:gender
                            }

                            //age group [age_group] - Target age group of the item
                            if (googleProduct != null && !String.IsNullOrEmpty(googleProduct.AgeGroup))
                            {
                                writer.WriteStartElement("g", "age_group", googleBaseNamespace);
                                writer.WriteCData(googleProduct.AgeGroup);
                                writer.WriteFullEndElement(); // g:age_group
                            }

                            //color [color] - Color of the item
                            if (googleProduct != null && !String.IsNullOrEmpty(googleProduct.Color))
                            {
                                writer.WriteStartElement("g", "color", googleBaseNamespace);
                                writer.WriteCData(googleProduct.Color);
                                writer.WriteFullEndElement(); // g:color
                            }

                            //size [size] - Size of the item
                            if (googleProduct != null && !String.IsNullOrEmpty(googleProduct.Size))
                            {
                                writer.WriteStartElement("g", "size", googleBaseNamespace);
                                writer.WriteCData(googleProduct.Size);
                                writer.WriteFullEndElement(); // g:size
                            }

                            #endregion

                            #region Tax & Shipping

                            //tax [tax]
                            //The tax attribute is an item-level override for merchant-level tax settings as defined in your Google Merchant Center account. This attribute is only accepted in the US, if your feed targets a country outside of the US, please do not use this attribute.
                            //IMPORTANT NOTE: Set tax in your Google Merchant Center account settings

                            //IMPORTANT NOTE: Set shipping in your Google Merchant Center account settings

                            if (_feedGoogleShoppingAdvancedSettings.IsUseAdditionalShippingChargeForDelivery)
                            {
                                //Additional shipping/delivery charge
                                var country = GetUsedCountry();
                                string countryISOCode = country.TwoLetterIsoCode;

                                //Use AdditionalShippingChargeServiceName as default if product delivery date value is null or empty
                                string additionalShippingChargeServiceName = _feedGoogleShoppingAdvancedSettings.AdditionalShippingChargeServiceName;
                                var productDeliveryDate = _shippingService.GetDeliveryDateById(product.DeliveryDateId);
                                if (productDeliveryDate != null)
                                {
                                    if (!String.IsNullOrEmpty((productDeliveryDate.Name)))
                                    {
                                        additionalShippingChargeServiceName = productDeliveryDate.Name;
                                    }
                                }
                                    
                                decimal additionalShippingCharge = product.AdditionalShippingCharge;
                                writer.WriteStartElement("g", "shipping", googleBaseNamespace);
                                writer.WriteElementString("g", "country", googleBaseNamespace, country.TwoLetterIsoCode);
                                writer.WriteElementString("g", "service", googleBaseNamespace, additionalShippingChargeServiceName);
                                writer.WriteElementString("g", "price", googleBaseNamespace,
                                                          additionalShippingCharge.ToString(nfi) + " " +
                                                          currency.CurrencyCode); ;
                                writer.WriteFullEndElement(); // g:shipping
                            }

                            //shipping weight [shipping_weight] - Weight of the item for shipping
                            //We accept only the following units of weight: lb, oz, g, kg.
                            if (_feedGoogleShoppingAdvancedSettings.IsIncludeShippingWeight)
                            {
                                var weightName = "kg";
                                var shippingWeight = product.Weight;
                                switch (_measureService.GetMeasureWeightById(_measureSettings.BaseWeightId).SystemKeyword)
                                {
                                    case "ounce":
                                        weightName = "oz";
                                        break;
                                    case "lb":
                                        weightName = "lb";
                                        break;
                                    case "grams":
                                        weightName = "g";
                                        break;
                                    case "kg":
                                        weightName = "kg";
                                        break;
                                    default:
                                        //unknown weight 
                                        weightName = "kg";
                                        break;
                                }
                                writer.WriteElementString("g", "shipping_weight", googleBaseNamespace, string.Format(CultureInfo.InvariantCulture, "{0} {1}", shippingWeight.ToString(nfi), weightName));
                            }

                            writer.WriteEndElement(); // item

                            #endregion
                        }
                    }
                }

                writer.WriteEndElement(); // channel
                writer.WriteEndElement(); // rss
                writer.WriteEndDocument();
            }
        }

        /// <summary>
        /// Installs the scheduled task.
        /// Generate the Google Shopping Advanced Feed
        /// INSERT [dbo].[ScheduleTask] ([Id], [Name], [Seconds], [Type], [Enabled], [StopOnError], [LastStartUtc], [LastEndUtc], [LastSuccessUtc]) VALUES (5, N'Generate Google Shopping Advanced Feed', 900, N'Nop.Plugin.Feed.GoogleShoppingAdvanced.FeedGoogleShoppingAdvancedTask,  Nop.Plugin.Feed.GoogleShoppingAdvanced', 1, 0, NULL, NULL, NULL)
        /// </summary>
        private void InstallScheduleTask()
        {
            //Check the database for the task
            var task = FindScheduledTask();

            if (task == null)
            {
                task = new ScheduleTask
                {
                    Name = "Generate Google Shopping Advanced Feed",
                    //each 60 minutes
                    Seconds = 3600,
                    Type = "Nop.Plugin.Feed.GoogleShoppingAdvanced.FeedGoogleShoppingAdvancedTask, Nop.Plugin.Feed.GoogleShoppingAdvanced",
                    Enabled = false,
                    StopOnError = false,
                };
                _scheduleTaskService.InsertTask(task);
            }
        }

        private ScheduleTask FindScheduledTask()
        {
            return _scheduleTaskService.GetTaskByType("Nop.Plugin.Feed.GoogleShoppingAdvanced.FeedGoogleShoppingAdvancedTask, Nop.Plugin.Feed.GoogleShoppingAdvanced");
        }

        /// <summary>
        /// Install plugin
        /// </summary>
        public override void Install()
        {
            //settings
            var settings = new FeedGoogleShoppingAdvancedSettings()
            {
                IsUseParentGroupedProductDescription = false,
                MinProductDescriptionCharLimit = 0,
                ProductPictureSize = 125,
                StaticFileName = string.Format("GoogleShoppingAdvancedProductFeed_{0}.xml", CommonHelper.GenerateRandomDigitCode(10)),
            };
            _settingService.SaveSetting(settings);
            
            //data
            _objectContext.Install();

            //locales
            this.AddOrUpdatePluginLocaleResource("Plugins.Feed.GoogleShoppingAdvanced.Store", "Store");
            this.AddOrUpdatePluginLocaleResource("Plugins.Feed.GoogleShoppingAdvanced.Store.Hint", "Select the store that will be used to generate the feed.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Feed.GoogleShoppingAdvanced.Currency", "Currency");
            this.AddOrUpdatePluginLocaleResource("Plugins.Feed.GoogleShoppingAdvanced.Currency.Hint", "Select the default currency that will be used to generate the feed.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Feed.GoogleShoppingAdvanced.DefaultGoogleCategory", "Default Google category");
            this.AddOrUpdatePluginLocaleResource("Plugins.Feed.GoogleShoppingAdvanced.DefaultGoogleCategory.Hint", "The default Google category to use if one is not specified.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Feed.GoogleShoppingAdvanced.IsProductIncludedOverride", "Include all products?");
            this.AddOrUpdatePluginLocaleResource("Plugins.Feed.GoogleShoppingAdvanced.IsProductIncludedOverride.Hint", "Do you wish to ignore the feed inclusion setting per product?");
            this.AddOrUpdatePluginLocaleResource("Plugins.Feed.GoogleShoppingAdvanced.General", "General");
            this.AddOrUpdatePluginLocaleResource("Plugins.Feed.GoogleShoppingAdvanced.Generate", "Generate feed");
            this.AddOrUpdatePluginLocaleResource("Plugins.Feed.GoogleShoppingAdvanced.Override", "Override product settings");
            this.AddOrUpdatePluginLocaleResource("Plugins.Feed.GoogleShoppingAdvanced.ProductPictureSize", "Product thumbnail image size");
            this.AddOrUpdatePluginLocaleResource("Plugins.Feed.GoogleShoppingAdvanced.ProductPictureSize.Hint", "The default size (pixels) for product thumbnail images.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Feed.GoogleShoppingAdvanced.Products.ProductName", "Product");
            this.AddOrUpdatePluginLocaleResource("Plugins.Feed.GoogleShoppingAdvanced.Products.GoogleCategory", "Google Category");
            this.AddOrUpdatePluginLocaleResource("Plugins.Feed.GoogleShoppingAdvanced.Products.Gender", "Gender");
            this.AddOrUpdatePluginLocaleResource("Plugins.Feed.GoogleShoppingAdvanced.Products.AgeGroup", "Age group");
            this.AddOrUpdatePluginLocaleResource("Plugins.Feed.GoogleShoppingAdvanced.Products.Color", "Color");
            this.AddOrUpdatePluginLocaleResource("Plugins.Feed.GoogleShoppingAdvanced.Products.Size", "Size");
            this.AddOrUpdatePluginLocaleResource("Plugins.Feed.GoogleShoppingAdvanced.Products.IsIncluded", "Include?");
            this.AddOrUpdatePluginLocaleResource("Plugins.Feed.GoogleShoppingAdvanced.SuccessResult", "Google Shopping feed has been successfully generated.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Feed.GoogleShoppingAdvanced.StaticFilePath", "Generated file path (static)");
            this.AddOrUpdatePluginLocaleResource("Plugins.Feed.GoogleShoppingAdvanced.StaticFilePath.Hint", "A file path of the generated Google Shopping file. It's static for your store and can be shared with the Google Shopping service.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Feed.GoogleShoppingAdvanced.IsIncludeShippingWeight", "Include product shipping weight?");
            this.AddOrUpdatePluginLocaleResource("Plugins.Feed.GoogleShoppingAdvanced.IsIncludeShippingWeight.Hint", "If you have specified a global delivery rule that is dependent on delivery weight, this attribute will be used to calculate the delivery cost of the item automatically.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Feed.GoogleShoppingAdvanced.IsUseAdditionalShippingChargeForDelivery", "Use product additional shipping charge for delivery estimate?");
            this.AddOrUpdatePluginLocaleResource("Plugins.Feed.GoogleShoppingAdvanced.IsUseAdditionalShippingChargeForDelivery.Hint", "Check this box to use the additional shipping charge set per product and override the global delivery settings you defined in Google Merchant Center.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Feed.GoogleShoppingAdvanced.AdditionalShippingChargeCountryId", "Additional shipping charge applies to");
            this.AddOrUpdatePluginLocaleResource("Plugins.Feed.GoogleShoppingAdvanced.AdditionalShippingChargeCountryId.Hint", "The default value for this sub-attribute is your feed's target country.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Feed.GoogleShoppingAdvanced.AdditionalShippingChargeServiceName", "Additional shipping charge service name");
            this.AddOrUpdatePluginLocaleResource("Plugins.Feed.GoogleShoppingAdvanced.AdditionalShippingChargeServiceName.Hint", "The name of the delivery method, service class or delivery speed: e.g. Courier, Royal Mail 1st Class, or Next Day Delivery");
            this.AddOrUpdatePluginLocaleResource("Plugins.Feed.GoogleShoppingAdvanced.IsUseParentGroupedProductDescription", "Use description of parent grouped product?");
            this.AddOrUpdatePluginLocaleResource("Plugins.Feed.GoogleShoppingAdvanced.IsUseParentGroupedProductDescription.Hint", "If simple products which belong to grouped products do not have their own description, choose to use the description of the parent grouped product instead.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Feed.GoogleShoppingAdvanced.MinProductDescriptionCharLimit", "Minimum number of characters acceptable for Product description");
            this.AddOrUpdatePluginLocaleResource("Plugins.Feed.GoogleShoppingAdvanced.MinProductDescriptionCharLimit.Hint", "If the product description is equal to or less than this figure, the parent grouped product description if available, will be used instead.");

            //Install scheduled task
            InstallScheduleTask();

            base.Install();
        }

        /// <summary>
        /// Uninstall plugin
        /// </summary>
        public override void Uninstall()
        {
            //settings
            _settingService.DeleteSetting<FeedGoogleShoppingAdvancedSettings>();

            //data
            _objectContext.Uninstall();

            //locales
            this.DeletePluginLocaleResource("Plugins.Feed.GoogleShoppingAdvanced.Store");
            this.DeletePluginLocaleResource("Plugins.Feed.GoogleShoppingAdvanced.Store.Hint");
            this.DeletePluginLocaleResource("Plugins.Feed.GoogleShoppingAdvanced.Currency");
            this.DeletePluginLocaleResource("Plugins.Feed.GoogleShoppingAdvanced.Currency.Hint");
            this.DeletePluginLocaleResource("Plugins.Feed.GoogleShoppingAdvanced.DefaultGoogleCategory");
            this.DeletePluginLocaleResource("Plugins.Feed.GoogleShoppingAdvanced.DefaultGoogleCategory.Hint");
            this.DeletePluginLocaleResource("Plugins.Feed.GoogleShoppingAdvanced.IsProductIncludedOverride");
            this.DeletePluginLocaleResource("Plugins.Feed.GoogleShoppingAdvanced.IsProductIncludedOverride.Hint");
            this.DeletePluginLocaleResource("Plugins.Feed.GoogleShoppingAdvanced.General");
            this.DeletePluginLocaleResource("Plugins.Feed.GoogleShoppingAdvanced.Generate");
            this.DeletePluginLocaleResource("Plugins.Feed.GoogleShoppingAdvanced.Override");
            this.DeletePluginLocaleResource("Plugins.Feed.GoogleShoppingAdvanced.ProductPictureSize");
            this.DeletePluginLocaleResource("Plugins.Feed.GoogleShoppingAdvanced.ProductPictureSize.Hint");
            this.DeletePluginLocaleResource("Plugins.Feed.GoogleShoppingAdvanced.Products.ProductName");
            this.DeletePluginLocaleResource("Plugins.Feed.GoogleShoppingAdvanced.Products.GoogleCategory");
            this.DeletePluginLocaleResource("Plugins.Feed.GoogleShoppingAdvanced.Products.Gender");
            this.DeletePluginLocaleResource("Plugins.Feed.GoogleShoppingAdvanced.Products.AgeGroup");
            this.DeletePluginLocaleResource("Plugins.Feed.GoogleShoppingAdvanced.Products.Color");
            this.DeletePluginLocaleResource("Plugins.Feed.GoogleShoppingAdvanced.Products.Size");
            this.DeletePluginLocaleResource("Plugins.Feed.GoogleShoppingAdvanced.Products.IsIncluded");
            this.DeletePluginLocaleResource("Plugins.Feed.GoogleShoppingAdvanced.SuccessResult");
            this.DeletePluginLocaleResource("Plugins.Feed.GoogleShoppingAdvanced.StaticFilePath");
            this.DeletePluginLocaleResource("Plugins.Feed.GoogleShoppingAdvanced.StaticFilePath.Hint");
            this.DeletePluginLocaleResource("Plugins.Feed.GoogleShoppingAdvanced.IsIncludeShippingWeight");
            this.DeletePluginLocaleResource("Plugins.Feed.GoogleShoppingAdvanced.IsIncludeShippingWeight.Hint");
            this.DeletePluginLocaleResource("Plugins.Feed.GoogleShoppingAdvanced.IsUseAdditionalShippingChargeForDelivery");
            this.DeletePluginLocaleResource("Plugins.Feed.GoogleShoppingAdvanced.IsUseAdditionalShippingChargeForDelivery.Hint");
            this.DeletePluginLocaleResource("Plugins.Feed.GoogleShoppingAdvanced.AdditionalShippingChargeCountryId");
            this.DeletePluginLocaleResource("Plugins.Feed.GoogleShoppingAdvanced.AdditionalShippingChargeCountryId.Hint");
            this.DeletePluginLocaleResource("Plugins.Feed.GoogleShoppingAdvanced.AdditionalShippingChargeServiceName");
            this.DeletePluginLocaleResource("Plugins.Feed.GoogleShoppingAdvanced.AdditionalShippingChargeServiceName.Hint");
            this.DeletePluginLocaleResource("Plugins.Feed.GoogleShoppingAdvanced.IsUseParentGroupedProductDescription");
            this.DeletePluginLocaleResource("Plugins.Feed.GoogleShoppingAdvanced.IsUseParentGroupedProductDescription.Hint");
            this.DeletePluginLocaleResource("Plugins.Feed.GoogleShoppingAdvanced.MinProductDescriptionCharLimit");
            this.DeletePluginLocaleResource("Plugins.Feed.GoogleShoppingAdvanced.MinProductDescriptionCharLimit.Hint");

            //Remove scheduled task
            var task = FindScheduledTask();
            if (task != null)
                _scheduleTaskService.DeleteTask(task);

            base.Uninstall();
        }
        
        /// <summary>
        /// Generate a static file for Google Shopping
        /// </summary>
        /// <param name="store">Store</param>
        public virtual void GenerateStaticFile(Store store)
        {
            if (store == null)
                throw new ArgumentNullException("store");
            string filePath = System.IO.Path.Combine(HttpRuntime.AppDomainAppPath, "content\\files\\exportimport", store.Id + "-" + _feedGoogleShoppingAdvancedSettings.StaticFileName);
            using (var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
            {
                GenerateFeed(fs, store);
            }
        }

        #endregion
    }
}
