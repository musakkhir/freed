using System.Net;
using Nop.Core;
using Nop.Services.Tasks;
using Nop.Plugin.Feed.GoogleShoppingAdvanced.Controllers;
using Nop.Plugin.Feed.GoogleShoppingAdvanced.Services;
using Nop.Plugin.Feed.GoogleShoppingAdvanced.Models;
using Nop.Services.Catalog;
using Nop.Services.Directory;
using Nop.Services.Localization;
using Nop.Core.Plugins;
using Nop.Services.Logging;
using Nop.Services.Stores;
using Nop.Services.Configuration;
using Nop.Services.Security;


namespace Nop.Plugin.Feed.GoogleShoppingAdvanced
{
    class FeedGoogleShoppingAdvancedTask : ITask
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

            public FeedGoogleShoppingAdvancedTask(IGoogleAdvancedService googleAdvancedService,
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

            /// <summary>
            /// Execute task
            /// TODO (LFW): Validate/check plugin status configuration
            /// </summary>
            public void Execute()
            {
                //Is plugin installed?
                var pluginDescriptor = _pluginFinder.GetPluginDescriptorBySystemName("Feed.GoogleShoppingAdvanced");
                if (pluginDescriptor == null)
                    return;

                //Is plugin configured?
                var plugin = pluginDescriptor.Instance() as FeedGoogleShoppingAdvancedService;
                if (plugin == null)
                    return;

                FeedGoogleShoppingAdvancedController feedGoogleShoppingAdvancedController = new FeedGoogleShoppingAdvancedController(_googleAdvancedService, _productService, _currencyService,
                    _countryService, _localizationService, _pluginFinder, _logger, _webHelper, _storeService, _feedGoogleShoppingAdvancedSettings, _settingService, _permissionService);

                var model = new FeedGoogleShoppingAdvancedModel();

                feedGoogleShoppingAdvancedController.GenerateFeed(model);
            }
    }
}
