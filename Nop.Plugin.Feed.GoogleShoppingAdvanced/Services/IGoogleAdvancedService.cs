using System.Collections.Generic;
using Nop.Plugin.Feed.GoogleShoppingAdvanced.Domain;

namespace Nop.Plugin.Feed.GoogleShoppingAdvanced.Services
{
    public partial interface IGoogleAdvancedService
    {
        void DeleteGoogleProduct(GoogleAdvancedProductRecord googleAdvnacedProductRecord);

        IList<GoogleAdvancedProductRecord> GetAll();

        GoogleAdvancedProductRecord GetById(int googleAdvancedProductRecordId);

        GoogleAdvancedProductRecord GetByProductId(int productId);

        void InsertGoogleAdvancedProductRecord(GoogleAdvancedProductRecord googleAdvancedProductRecord);

        void UpdateGoogleAdvancedProductRecord(GoogleAdvancedProductRecord googleAdvancedProductRecord);

        IList<string> GetTaxonomyList();
    }
}
