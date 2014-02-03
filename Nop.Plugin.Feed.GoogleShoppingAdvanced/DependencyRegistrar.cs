using Autofac;
using Autofac.Core;
using Autofac.Integration.Mvc;
using Nop.Core.Data;
using Nop.Core.Infrastructure;
using Nop.Core.Infrastructure.DependencyManagement;
using Nop.Data;
using Nop.Plugin.Feed.GoogleShoppingAdvanced.Data;
using Nop.Plugin.Feed.GoogleShoppingAdvanced.Domain;
using Nop.Plugin.Feed.GoogleShoppingAdvanced.Services;

namespace Nop.Plugin.Feed.GoogleShoppingAdvanced
{
    public class DependencyRegistrar : IDependencyRegistrar
    {
        public virtual void Register(ContainerBuilder builder, ITypeFinder typeFinder)
        {
            builder.RegisterType<GoogleAdvancedService>().As<IGoogleAdvancedService>().InstancePerHttpRequest();

            //data layer
            var dataSettingsManager = new DataSettingsManager();
            var dataProviderSettings = dataSettingsManager.LoadSettings();

            if (dataProviderSettings != null && dataProviderSettings.IsValid())
            {
                //register named context
                builder.Register<IDbContext>(c => new GoogleAdvancedProductObjectContext(dataProviderSettings.DataConnectionString))
                    .Named<IDbContext>("nop_object_context_google_advanced_product")
                    .InstancePerHttpRequest();

                builder.Register<GoogleAdvancedProductObjectContext>(c => new GoogleAdvancedProductObjectContext(dataProviderSettings.DataConnectionString))
                    .InstancePerHttpRequest();
            }
            else
            {
                //register named context
                builder.Register<IDbContext>(c => new GoogleAdvancedProductObjectContext(c.Resolve<DataSettings>().DataConnectionString))
                    .Named<IDbContext>("nop_object_context_google_advanced_product")
                    .InstancePerHttpRequest();

                builder.Register<GoogleAdvancedProductObjectContext>(c => new GoogleAdvancedProductObjectContext(c.Resolve<DataSettings>().DataConnectionString))
                    .InstancePerHttpRequest();
            }

            //override required repository with our custom context
            builder.RegisterType<EfRepository<GoogleAdvancedProductRecord>>()
                .As<IRepository<GoogleAdvancedProductRecord>>()
                .WithParameter(ResolvedParameter.ForNamed<IDbContext>("nop_object_context_google_advanced_product"))
                .InstancePerHttpRequest();
        }

        public int Order
        {
            get { return 1; }
        }
    }
}
