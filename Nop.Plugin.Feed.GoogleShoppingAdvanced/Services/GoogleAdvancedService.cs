using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Nop.Core.Data;
using Nop.Plugin.Feed.GoogleShoppingAdvanced.Domain;

namespace Nop.Plugin.Feed.GoogleShoppingAdvanced.Services
{
    public partial class GoogleAdvancedService : IGoogleAdvancedService
    {
        #region Fields

        private readonly IRepository<GoogleAdvancedProductRecord> _gpRepository;

        #endregion

        #region Ctor

        public GoogleAdvancedService(IRepository<GoogleAdvancedProductRecord> gpRepository)
        {
            this._gpRepository = gpRepository;
        }

        #endregion

        #region Utilties

        private string GetEmbeddedFileContent(string resourceName)
        {
            string fullResourceName = string.Format("Nop.Plugin.Feed.GoogleShoppingAdvanced.Files.{0}", resourceName);
            var assem = this.GetType().Assembly;
            using (var stream = assem.GetManifestResourceStream(fullResourceName))
            using (var reader = new StreamReader(stream))
                return reader.ReadToEnd();
        }

        #endregion

        #region Methods

        public virtual void DeleteGoogleProduct(GoogleAdvancedProductRecord googleAdvancedProductRecord)
        {
            if (googleAdvancedProductRecord == null)
                throw new ArgumentNullException("googleAdvancedProductRecord");

            _gpRepository.Delete(googleAdvancedProductRecord);
        }

        public virtual IList<GoogleAdvancedProductRecord> GetAll()
        {
            var query = from gp in _gpRepository.Table
                        orderby gp.Id
                        select gp;
            var records = query.ToList();
            return records;
        }

        public virtual GoogleAdvancedProductRecord GetById(int googleAdvancedProductRecordId)
        {
            if (googleAdvancedProductRecordId == 0)
                return null;

            return _gpRepository.GetById(googleAdvancedProductRecordId);
        }

        public virtual GoogleAdvancedProductRecord GetByProductId(int productId)
        {
            if (productId == 0)
                return null;

            var query = from gp in _gpRepository.Table
                        where gp.ProductId == productId
                        orderby gp.Id
                        select gp;
            var record = query.FirstOrDefault();
            return record;
        }

        public virtual void InsertGoogleAdvancedProductRecord(GoogleAdvancedProductRecord googleAdvancedProductRecord)
        {
            if (googleAdvancedProductRecord == null)
                throw new ArgumentNullException("googleAdvancedProductRecord");

            _gpRepository.Insert(googleAdvancedProductRecord);
        }

        public virtual void UpdateGoogleAdvancedProductRecord(GoogleAdvancedProductRecord googleAdvancedProductRecord)
        {
            if (googleAdvancedProductRecord == null)
                throw new ArgumentNullException("googleAdvancedProductRecord");

            _gpRepository.Update(googleAdvancedProductRecord);
        }

        public virtual IList<string> GetTaxonomyList()
        {
            var fileContent = GetEmbeddedFileContent("taxonomy.txt");
            if (String.IsNullOrEmpty((fileContent)))
                return new List<string>();

            //parse the file
            var result = fileContent.Split(new string[] {"\n"}, StringSplitOptions.RemoveEmptyEntries)
                .ToList();
            return result;
        }

        #endregion
    }
}
