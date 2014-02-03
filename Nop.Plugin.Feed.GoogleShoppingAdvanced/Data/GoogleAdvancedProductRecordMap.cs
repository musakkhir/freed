using System.Data.Entity.ModelConfiguration;
using Nop.Plugin.Feed.GoogleShoppingAdvanced.Domain;

namespace Nop.Plugin.Feed.GoogleShoppingAdvanced.Data
{
    public partial class GoogleAdvancedProductRecordMap : EntityTypeConfiguration<GoogleAdvancedProductRecord>
    {
        public GoogleAdvancedProductRecordMap()
        {
            this.ToTable("GoogleAdvancedProduct");
            //Map the primary key
            this.HasKey(x => x.Id);
        }
    }
}