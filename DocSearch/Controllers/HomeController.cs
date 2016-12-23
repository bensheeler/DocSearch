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
using System.Security.Claims;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Documents;
using DocSearch.Models;

namespace DocSearch.Controllers
{
    [Authorize]    
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
            var results = docsClient.Documents.Search<DocumentBlob>(search);
            ViewBag.TotalResults = results.Results.Count;
            return View("SearchResults", results.Results);            
        }    

        [Route("{projectName}")]
        public ActionResult Project(string projectName)
        {
            ViewBag.ProjectName = projectName;
            return View();
        }

        [AllowAnonymous]
        public ActionResult _Menu()
        {
            var menuItems = new List<string>();
            var emailClaim = ClaimsPrincipal.Current?.FindFirst("email");

            if (emailClaim == null)
            {
                return new EmptyResult();
            }

            var email = emailClaim.Value;
            ViewBag.Email = email;

            var uri = new Uri(ConfigurationManager.AppSettings["docDb:Endpoint"]);
            var client = new DocumentClient(uri, ConfigurationManager.AppSettings["DocDb:AuthKey"]);
            var databaseId = ConfigurationManager.AppSettings["docDb:Database"];
            var collectionId = ConfigurationManager.AppSettings["docDb:Collection"];

            Document document = client.ReadDocumentAsync(
                UriFactory.CreateDocumentUri(databaseId, collectionId, email))
                .Result;

            var userFolder = (Models.User)(dynamic)document;
            userFolder.Folders.Sort();
            ViewBag.MenuItems = userFolder.Folders;
            return PartialView();
        }
    }
}