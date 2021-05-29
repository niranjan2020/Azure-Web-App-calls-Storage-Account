using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Azure.Core;
using Azure.Identity;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace BlobProjectWithConnString.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StorageAccountActions : ControllerBase
    {
        private readonly IConfiguration _configuration;
        public StorageAccountActions(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        private static string ConnectionString = "";
        private static string ContainerName = "azure204container";
        private static string AccountName = "azure204";
        private static string BlobName = "ERA5_MesquiteSky.txt";
        private static string containerUrl = "https://azure204.blob.core.windows.net/azure204container";
        // GET: api/<StorageAccountActions>
        [HttpGet]
        [Route("getblobs")]
        public async Task<IEnumerable<string>> GetBlobsAsync()
        {
            List<string> blobs = new List<string>();
            BlobServiceClient blobServiceClient = new BlobServiceClient(ConnectionString);
            BlobContainerClient containerClient = new BlobContainerClient(ConnectionString, ContainerName);
            await foreach (BlobItem blobItem in containerClient.GetBlobsAsync())
            {
                Console.WriteLine("\t" + blobItem.Name);
                blobs.Add(blobItem.Name);
            }
            return blobs;
        }

        [HttpGet]
        [Route("getblobsfromkeyvault")]
        public async Task<IEnumerable<string>> GetBlobsFromKeyVaultAsync()
        {
            List<string> blobs = new List<string>();
            BlobServiceClient blobServiceClient = new BlobServiceClient(_configuration["azure204storageaccountconnstring"]);
            BlobContainerClient containerClient = new BlobContainerClient(ConnectionString, ContainerName);
            await foreach (BlobItem blobItem in containerClient.GetBlobsAsync())
            {
                Console.WriteLine("\t" + blobItem.Name);
                blobs.Add(blobItem.Name);
            }
            return blobs;
        }

        [HttpGet]
        [Route("getblobsusingsastoken")]
        public async Task<string> GetBlobsUsingSAStokenAsync(string storedPolicyName = null)
        {
            string sasBlobToken;
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(_configuration["azure204storageaccountconnstring"]);

            CloudBlobClient serviceClient = storageAccount.CreateCloudBlobClient();

            CloudBlobContainer container = serviceClient.GetContainerReference($"{ContainerName}");

            CloudBlockBlob blob = container.GetBlockBlobReference(BlobName);

           
                SharedAccessBlobPolicy adHocSAS = new SharedAccessBlobPolicy()
                {
                    SharedAccessExpiryTime = DateTime.UtcNow.AddHours(24),
                    Permissions = SharedAccessBlobPermissions.Read | SharedAccessBlobPermissions.Write | SharedAccessBlobPermissions.Create
                };
                sasBlobToken = blob.GetSharedAccessSignature(adHocSAS);
           
            return blob.Uri + sasBlobToken;
        }

        [HttpGet]
        [Route("getblobsusingmanagedidentities")]
        public async Task<List<string>> GetBlobsUsingManagedidentities()
        {
            TokenCredential __credential = new DefaultAzureCredential();
            Uri blob_uri = new Uri(containerUrl);
            BlobContainerClient containerClient = new BlobContainerClient(new Uri(containerUrl),
                                                                   new DefaultAzureCredential());
            Console.WriteLine("Listing blobs...");
            List<string> blobs = new List<string>();
            // List all blobs in the container
            await foreach (BlobItem blobItem in containerClient.GetBlobsAsync())
            {
                blobs.Add(blobItem.Name);
            }
            return blobs;
        }
    }
}
