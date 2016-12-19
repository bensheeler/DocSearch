using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Microsoft.Azure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System.IO;
using Microsoft.Azure.Search;
using Newtonsoft.Json;
using System.Configuration;

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
        public ActionResult Upload(HttpPostedFileBase file, string tags, string containerName)
        {
            var storageAccount = CloudStorageAccount.Parse(CloudConfigurationManager.GetSetting("StorageConnectionString"));
            var blobClient = storageAccount.CreateCloudBlobClient();
            var docsContainer = blobClient.GetContainerReference(containerName);
            var blockBlob = docsContainer.GetBlockBlobReference(Path.GetFileName(file.FileName));

            using(var stream = file.InputStream)
            {
                blockBlob.UploadFromStream(stream);
            }

            blockBlob.Metadata.Add("uri", blockBlob.Uri.AbsoluteUri);
            blockBlob.Metadata.Add("tags", tags);
            blockBlob.Metadata.Add("contentType", file.ContentType);
            blockBlob.SetMetadata();
            return View("Index");
        }

        [HttpGet]
        public ActionResult Search(string search, string containerName)
        {
            var searchServiceName = ConfigurationManager.AppSettings["SearchApiName"];
            var adminApiKey = ConfigurationManager.AppSettings["SearchApiKey"];

            var serviceClient = new SearchServiceClient(searchServiceName, new SearchCredentials(adminApiKey));
            var docsClient = serviceClient.Indexes.GetClient(containerName);            
            var results = docsClient.Documents.Search<Document>(search);
            ViewBag.TotalResults = results.Results.Count;
            return View("SearchResults", results.Results);            
        }    

        [Route("{projectName}")]
        public ActionResult Project(string projectName)
        {
            ViewBag.ProjectName = projectName;
            return View();
        }
    }

    public class Document
    {
        [JsonProperty("tags")]
        public string Tags { get; set; }

        [JsonProperty("metadata_storage_size")]
        public int? Bytes { get; set; }

        [JsonProperty("metadata_storage_name")]
        public string FileName { get; set; }

        [JsonProperty("uri")]
        public string Uri { get; set; }

        [JsonProperty("contentType")]
        public string ContentType { get; set; }
    }
}