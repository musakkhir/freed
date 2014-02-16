using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Nop.Core;
using Nop.Core.Domain.Stores;
using Nop.Core.Plugins;
using Nop.Plugin.Feed.GoogleShoppingAdvanced.Domain;
using Nop.Plugin.Feed.GoogleShoppingAdvanced.Models;
using Nop.Plugin.Feed.GoogleShoppingAdvanced.Services;
using Nop.Services.Catalog;
using Nop.Services.Configuration;
using Nop.Services.Directory;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Security;
using Nop.Services.Stores;
using Nop.Web.Framework.Controllers;
using Telerik.Web.Mvc;

namespace Nop.Plugin.Feed.GoogleShoppingAdvanced.Controllers
{
    [AdminAuthorize]
    public class FeedGoogleShoppingAdvancedController : Controller
    {
        private readonly IGoogleAdvancedService _googleAdvancedService;
        private readonly IProductService _productService;
        private readonly ICurrencyService _currencyService;
        private readonly ICountryService _countryService;
        private readonly ILocalizationService _localizationService;
        private readonly IPluginFinder _pluginFinder;
        private readonly ILogger _logger;
        private readonly IWebHelper _webHelper;
        private readonly IStoreService _storeService;
        private readonly FeedGoogleShoppingAdvancedSettings _feedGoogleShoppingAdvancedSettings;
        private readonly ISettingService _settingService;
        private readonly IPermissionService _permissionService;

        public FeedGoogleShoppingAdvancedController(IGoogleAdvancedService googleAdvancedService, 
            IProductService productService, ICurrencyService currencyService, ICountryService countryService,
            ILocalizationService localizationService, IPluginFinder pluginFinder, 
            ILogger logger, IWebHelper webHelper, IStoreService storeService,
            FeedGoogleShoppingAdvancedSettings feedGoogleShoppingAdvancedSettings, ISettingService settingService, 
            IPermissionService permissionService)
        {
            this._googleAdvancedService = googleAdvancedService;
            this._productService = productService;
            this._currencyService = currencyService;
            this._countryService = countryService;
            this._localizationService = localizationService;
            this._pluginFinder = pluginFinder;
            this._logger = logger;
            this._webHelper = webHelper;
            this._storeService = storeService;
            this._feedGoogleShoppingAdvancedSettings = feedGoogleShoppingAdvancedSettings;
            this._settingService = settingService;
            this._permissionService = permissionService;
        }

        [ChildActionOnly]
        public ActionResult Configure()
        {
            var model = new FeedGoogleShoppingAdvancedModel();
            //picture
            model.ProductPictureSize = _feedGoogleShoppingAdvancedSettings.ProductPictureSize;
            //stores
            model.StoreId = _feedGoogleShoppingAdvancedSettings.StoreId;
            model.AvailableStores.Add(new SelectListItem() { Text = _localizationService.GetResource("Admin.Common.All"), Value = "0" });
            foreach (var s in _storeService.GetAllStores())
                model.AvailableStores.Add(new SelectListItem() { Text = s.Name, Value = s.Id.ToString() });
            //currencies
            model.CurrencyId = _feedGoogleShoppingAdvancedSettings.CurrencyId;
            foreach (var c in _currencyService.GetAllCurrencies())
                model.AvailableCurrencies.Add(new SelectListItem() { Text = c.Name, Value = c.Id.ToString() });
            //Include shipping weight?
            model.IsIncludeShippingWeight = _feedGoogleShoppingAdvancedSettings.IsIncludeShippingWeight;
            // Use additional shipping charge for delivery estimate?
            model.IsUseAdditionalShippingChargeForDelivery = _feedGoogleShoppingAdvancedSettings.IsUseAdditionalShippingChargeForDelivery;
            // Additional shipping charge country id
            model.AdditionalShippingChargeCountryId = _feedGoogleShoppingAdvancedSettings.AdditionalShippingChargeCountryId;
            //countries - Set value as -1 to prevent form validation if value type is not int
            model.AvailableCountries.Add(new SelectListItem() { Text = "Select a country", Value = "-1" });
            foreach (var ct in _countryService.GetAllCountries())
                model.AvailableCountries.Add(new SelectListItem() { Text = ct.Name, Value = ct.Id.ToString() });
            // Additional shipping charge service name
            model.AdditionalShippingChargeServiceName = _feedGoogleShoppingAdvancedSettings.AdditionalShippingChargeServiceName;
            // Show all products?
            model.IsProductIncludedOverride = _feedGoogleShoppingAdvancedSettings.IsProductIncludedOverride;
            //Google categories
            model.DefaultGoogleCategory = _feedGoogleShoppingAdvancedSettings.DefaultGoogleCategory;
            model.AvailableGoogleCategories.Add(new SelectListItem() {Text = "Select a category", Value = ""});
            foreach (var gc in _googleAdvancedService.GetTaxonomyList())
                model.AvailableGoogleCategories.Add(new SelectListItem() {Text = gc, Value = gc});

            //file paths
            foreach (var store in _storeService.GetAllStores())
            {
                var localFilePath = System.IO.Path.Combine(HttpRuntime.AppDomainAppPath, "content\\files\\exportimport", store.Id + "-" + _feedGoogleShoppingAdvancedSettings.StaticFileName);
                if (System.IO.File.Exists(localFilePath))
                    model.GeneratedFiles.Add(new FeedGoogleShoppingAdvancedModel.GeneratedFileModel()
                    {
                        StoreName = store.Name,
                        FileUrl = string.Format("{0}content/files/exportimport/{1}-{2}", _webHelper.GetStoreLocation(false), store.Id, _feedGoogleShoppingAdvancedSettings.StaticFileName)
                    });
            }
            
            return View("Nop.Plugin.Feed.GoogleShoppingAdvanced.Views.FeedGoogleShoppingAdvanced.Configure", model);
        }

        [HttpPost]
        [ChildActionOnly]
        [FormValueRequired("save")]
        public ActionResult Configure(FeedGoogleShoppingAdvancedModel model)
        {
            if (!ModelState.IsValid)
            {
                return Configure();
            }

            //save settings
            _feedGoogleShoppingAdvancedSettings.ProductPictureSize = model.ProductPictureSize;
            _feedGoogleShoppingAdvancedSettings.CurrencyId = model.CurrencyId;
            _feedGoogleShoppingAdvancedSettings.StoreId = model.StoreId;
            _feedGoogleShoppingAdvancedSettings.DefaultGoogleCategory = model.DefaultGoogleCategory;
            _feedGoogleShoppingAdvancedSettings.IsProductIncludedOverride = model.IsProductIncludedOverride;
            _feedGoogleShoppingAdvancedSettings.IsIncludeShippingWeight = model.IsIncludeShippingWeight;
            _feedGoogleShoppingAdvancedSettings.IsUseAdditionalShippingChargeForDelivery = model.IsUseAdditionalShippingChargeForDelivery;
            _feedGoogleShoppingAdvancedSettings.AdditionalShippingChargeCountryId = model.AdditionalShippingChargeCountryId;
            _feedGoogleShoppingAdvancedSettings.AdditionalShippingChargeServiceName = model.AdditionalShippingChargeServiceName;

            _settingService.SaveSetting(_feedGoogleShoppingAdvancedSettings);
            
            //redisplay the form
            return Configure();
        }

        [HttpPost, ActionName("Configure")]
        [ChildActionOnly]
        [FormValueRequired("generate")]
        public ActionResult GenerateFeed(FeedGoogleShoppingAdvancedModel model)
        {
            try
            {
                var pluginDescriptor = _pluginFinder.GetPluginDescriptorBySystemName("Feed.GoogleShoppingAdvanced");
                if (pluginDescriptor == null)
                    throw new Exception("Cannot load the plugin");

                //plugin
                var plugin = pluginDescriptor.Instance() as FeedGoogleShoppingAdvancedService;
                if (plugin == null)
                    throw new Exception("Cannot load the plugin");

                var stores = new List<Store>();
                var storeById = _storeService.GetStoreById(_feedGoogleShoppingAdvancedSettings.StoreId);
                if (storeById != null)
                    stores.Add(storeById);
                else
                    stores.AddRange(_storeService.GetAllStores());

                foreach (var store in stores)
                    plugin.GenerateStaticFile(store);

                model.GenerateFeedResult = _localizationService.GetResource("Plugins.Feed.GoogleShoppingAdvanced.SuccessResult");
            }
            catch (Exception exc)
            {
                model.GenerateFeedResult = exc.Message;
                _logger.Error(exc.Message, exc);
            }

            //stores
            model.AvailableStores.Add(new SelectListItem() { Text = _localizationService.GetResource("Admin.Common.All"), Value = "0" });
            foreach (var s in _storeService.GetAllStores())
                model.AvailableStores.Add(new SelectListItem() { Text = s.Name, Value = s.Id.ToString() });
            //currencies
            foreach (var c in _currencyService.GetAllCurrencies())
                model.AvailableCurrencies.Add(new SelectListItem() { Text = c.Name, Value = c.Id.ToString() });
            //countries - Set value as -1 to prevent form validation if value type is not int
            model.AvailableCountries.Add(new SelectListItem() { Text = "Select a country", Value = "-1" });
            foreach (var ct in _countryService.GetAllCountries())
                model.AvailableCountries.Add(new SelectListItem() { Text = ct.Name, Value = ct.Id.ToString() });
            //Google categories
            model.AvailableGoogleCategories.Add(new SelectListItem() { Text = "Select a category", Value = "" });
            foreach (var gc in _googleAdvancedService.GetTaxonomyList())
                model.AvailableGoogleCategories.Add(new SelectListItem() { Text = gc, Value = gc });

            //file paths
            foreach (var store in _storeService.GetAllStores())
            {
                var localFilePath = System.IO.Path.Combine(HttpRuntime.AppDomainAppPath, "content\\files\\exportimport", store.Id + "-" + _feedGoogleShoppingAdvancedSettings.StaticFileName);
                if (System.IO.File.Exists(localFilePath))
                    model.GeneratedFiles.Add(new FeedGoogleShoppingAdvancedModel.GeneratedFileModel()
                    {
                        StoreName = store.Name,
                        FileUrl = string.Format("{0}content/files/exportimport/{1}-{2}", _webHelper.GetStoreLocation(false), store.Id, _feedGoogleShoppingAdvancedSettings.StaticFileName)
                    });
            }

            return View("Nop.Plugin.Feed.GoogleShoppingAdvanced.Views.FeedGoogleShoppingAdvanced.Configure", model);
        }

        [HttpPost, GridAction(EnableCustomBinding = true)]
        public ActionResult GoogleProductList(GridCommand command)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManagePlugins))
                return Content("Access denied");

            var products = _productService.SearchProducts(pageIndex: command.Page - 1,
                pageSize: command.PageSize, showHidden: true);
            var productsModel = products
                .Select(x =>
                            {
                                var gModel = new FeedGoogleShoppingAdvancedModel.GoogleAdvancedProductModel()
                                {
                                    ProductId = x.Id,
                                    ProductName = x.Name

                                };
                                var googleAdvancedProduct = _googleAdvancedService.GetByProductId(x.Id);
                                if (googleAdvancedProduct != null)
                                {
                                    gModel.GoogleCategory = googleAdvancedProduct.Taxonomy;
                                    gModel.Gender = googleAdvancedProduct.Gender;
                                    gModel.AgeGroup = googleAdvancedProduct.AgeGroup;
                                    gModel.Color = googleAdvancedProduct.Color;
                                    gModel.GoogleSize = googleAdvancedProduct.Size;
                                    gModel.IsIncluded = googleAdvancedProduct.IsIncluded;
                                }

                                return gModel;
                            })
                .ToList();

            var model = new GridModel<FeedGoogleShoppingAdvancedModel.GoogleAdvancedProductModel>
            {
                Data = productsModel,
                Total = products.TotalCount
            };

            return new JsonResult
            {
                Data = model
            };
        }

        [GridAction(EnableCustomBinding = true)]
        public ActionResult GoogleProductUpdate(GridCommand command, FeedGoogleShoppingAdvancedModel.GoogleAdvancedProductModel model)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManagePlugins))
                return Content("Access denied");

            var googleAdvancedProduct = _googleAdvancedService.GetByProductId(model.ProductId);
            if (googleAdvancedProduct != null)
            {

                googleAdvancedProduct.Taxonomy = model.GoogleCategory;
                googleAdvancedProduct.Gender = model.Gender;
                googleAdvancedProduct.AgeGroup = model.AgeGroup;
                googleAdvancedProduct.Color = model.Color;
                googleAdvancedProduct.Size = model.GoogleSize;
                googleAdvancedProduct.IsIncluded = model.IsIncluded;
                _googleAdvancedService.UpdateGoogleAdvancedProductRecord(googleAdvancedProduct);
            }
            else
            {
                //insert
                googleAdvancedProduct = new GoogleAdvancedProductRecord()
                {
                    ProductId = model.ProductId,
                    Taxonomy = model.GoogleCategory,
                    Gender = model.Gender,
                    AgeGroup = model.AgeGroup,
                    Color = model.Color,
                    Size = model.GoogleSize,
                    IsIncluded = model.IsIncluded
                };
                _googleAdvancedService.InsertGoogleAdvancedProductRecord(googleAdvancedProduct);
            }
            
            return GoogleProductList(command);
        }
    }
}
