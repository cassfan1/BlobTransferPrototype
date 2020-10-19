using Azure.Storage.Blobs;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.IO;
using System.Threading.Tasks;

namespace BlobTransferPrototype
{
  public static class Function1
  {
    private const string SourceBlobConnString = "";
    private const string DestBlobConnString = "";

    [FunctionName("Function1")]
    public static async Task Run([BlobTrigger("test/{name}",
      Connection = "BlobConnection")] Stream myBlob,
          string name,
          ILogger log)
    {
      await GetBlobAsync(name);

      await MoveBlobAsync(name);
    }

    private static async Task MoveBlobAsync(string blobName)
    {
      var sourceCloudStorageAccount = CloudStorageAccount.Parse(SourceBlobConnString);
      var sourceCloudBlobClient = sourceCloudStorageAccount.CreateCloudBlobClient();
      var sourceCloudBlobContainer = sourceCloudBlobClient.GetContainerReference("test");

      var destCloudStorageAccount = CloudStorageAccount.Parse(DestBlobConnString);
      var destCloudBlobClient = destCloudStorageAccount.CreateCloudBlobClient();
      var destCloudBlobContainer = destCloudBlobClient.GetContainerReference("iotdev");

      var destBlob = destCloudBlobContainer.GetBlockBlobReference(blobName);
      await destBlob.StartCopyAsync(new Uri(GetSharedAccessUri(blobName, sourceCloudBlobContainer)));
    }


    private static async Task GetBlobAsync(string blobName)
    {
      try
      {
        BlobServiceClient blobServiceClient = new BlobServiceClient(SourceBlobConnString);
        BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient("test");

        BlobClient blobClient = containerClient.GetBlobClient(blobName);

        var properties = await blobClient.GetPropertiesAsync();
        var metadata = properties.Value.Metadata;
      }
      catch (Exception ex)
      {
        Console.WriteLine(ex);
        throw;
      }
    }

    private static string GetSharedAccessUri(string blobName, CloudBlobContainer container)
    {
      DateTime toDateTime = DateTime.Now.AddMinutes(60);

      SharedAccessBlobPolicy policy = new SharedAccessBlobPolicy
      {
        Permissions = SharedAccessBlobPermissions.Read,
        SharedAccessStartTime = null,
        SharedAccessExpiryTime = new DateTimeOffset(toDateTime)
      };

      CloudBlockBlob blob = container.GetBlockBlobReference(blobName);
      string sas = blob.GetSharedAccessSignature(policy);

      return blob.Uri.AbsoluteUri + sas;
    }
  }
}
