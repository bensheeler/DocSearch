using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Microsoft.Azure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System.IO;

namespace DocSearch.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }

        [HttpPost]
        public ActionResult Upload(HttpPostedFileBase file)
        {
            var storageAccount = CloudStorageAccount.Parse(CloudConfigurationManager.GetSetting("StorageConnectionString"));
            var blobClient = storageAccount.CreateCloudBlobClient();
            var docsContainer = blobClient.GetContainerReference("docs");
            var blockBlob = docsContainer.GetBlockBlobReference(Path.GetFileName(file.FileName));

            using(var stream = file.InputStream)
            {
                blockBlob.UploadFromStream(stream);
            }

            blockBlob.Metadata.Add("tags", "test, docSearch");
            blockBlob.SetMetadata();
            return View("Index");
        }
    }
}