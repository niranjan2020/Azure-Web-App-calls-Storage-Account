# Azure App Service calls storage account (Best Practices)

## Scenario ##

A common challenge for developers is the management of secrets and credentials used to secure communication between different components making up a solution.
The challenge is where to store these numerous secrets. Not in the code, certainly. That leaves the config file, which, over the years, has become a mishmash of many settings. And they're settings you don't want to lose, so you end up checking that config file into source control.

## 1. Bad Code ##
```
        private static string ConnectionString = "sample connection string";
        private static string ContainerName = "sample container name";
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

```

Certainly we will not write this code. It is very bad practice to check in secrete keys, connection string into source code. 

## 2. Key Vault ##
We can move our secretes and connection string to azure keyvault and thus we can stop hard coding values and checking into source control.

Lets see how we can store the values in key vault and access them in the code.

First we will go to Azure AD and register our application. 

Go to Menu Bar in Azure Portal and click on App Registration. 

Enter the name of your application. Keep all other values to default and click on Register.

Navigate to Certificates & secrets and create Secrete and note it down

Next we will store value in KeyVault.

Go to KeyVault and Create one if not available

Go to Secretes and click on Generate/Import. Add sample key For Example azurestorageaccountconnstring and pass Azure Storage Account Connection string and save it.

Go to Access Policy and click on Add Access Policy

Please provide below Options 
Configure From Template - Secrete Management
Key Permisions - GET
Secrete Permissions - GET,LIST
Select Principal - Search your azure ad App with name and click on Add.

now Lets see how we can integrate with our .Net Core App

Add below configuration in your AppSettings.json

```
 "KeyVault": {
    "Vault": "", //Name of your app registred in Azure AD
    "ClientId": "", //Client/Application ID of you app registred in Azure AD
    "ClientSecret": "" //Secrete Key of you app registred in Azure AD
  }

```

Modify Program.cs file
```
public static IHostBuilder CreateHostBuilder(string[] args) =>
           Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((context, config) =>
            {

                var root = config.Build();
                config.AddAzureKeyVault($"https://{root["KeyVault:Vault"]}.vault.azure.net/", root["KeyVault:ClientId"], root["KeyVault:ClientSecret"]);
            })
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseStartup<Startup>();
            });
```

Add one method to read blob names in your controller
```
[HttpGet]
        [Route("getblobsfromkeyvault")]
        public async Task<IEnumerable<string>> GetBlobsFromKeyVaultAsync()
        {
            List<string> blobs = new List<string>();
            BlobServiceClient blobServiceClient = new BlobServiceClient(_configuration["azurestorageaccountconnstring"]);
            BlobContainerClient containerClient = new BlobContainerClient(_configuration["azurestorageaccountconnstring"], ContainerName);
            await foreach (BlobItem blobItem in containerClient.GetBlobsAsync())
            {
                Console.WriteLine("\t" + blobItem.Name);
                blobs.Add(blobItem.Name);
            }
            return blobs;
        }
```

In the above code azurestorageaccountconnstring should be same as secrete name you have added in the Azure Key Vault.

In this method we have kept our connection string in more secured manner in key vault and we have not checked in connection string into source code. 

## Problem with KeyVault ##

1. To access KeyVault again we ended up with managing other secretes in our appsettings.json. For example, ClientId, Secrete etc.
2. Other importent thing is whenever we use full connection string in our code, that means we will be giving full admin control over azure storage account. 
3. If someone goes to azure storage account and regenrates key then this key will invalidate and existing connection string stored in KeyVault will not work anymore. We have to again manually add the new connection string in keyvault.


## 3. Using Shared Access Signature ##
We can grant limited access to Azure Storage resources using shared access signatures. A shared access signature is a signed URI that points to one or more storage resources. The URI includes a token that contains a special set of query parameters. The token indicates how the resources may be accessed by the client. This is most commonly used whenever you want to give access to your clients to give limited access to your storage account. Using SAS token clients can intereact with Storage Accounts based on the permissions given to SAS. Example,
```
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

```
In the above code, I have created policy which has Read,Write and Create permission to Container and we have set Expiration time to one day.
If clients wants to only read blobs then we can set permission to only Read. Using SAS we can have more granular access to our storage accounts.

## Problem with SAS ##
1. To generate Shared Access token still we need to have storage account connection string that means even if we use keyvault but we will end up with storing other configs in code.
2.  If a SAS is leaked, it can be used by anyone who obtains it, which can potentially compromise your storage account.
3.  If a SAS provided to a client application expires and the application is unable to retrieve a new SAS from your service, then the application's functionality may be disturbed.
4.  We do have option to invalidate the SAS token in any way because SAS token is not tracked by Azure Storage in any way.

## how to Solve the Problem ##
To solve all of the above problems we can make use of Managed Identities. there are two types of Managed identities available. 1. System Assigned Managed identity 2. User Assigned Managed Identity. We will see how system assigned managed identity works with example.

System assigned managed identities provides a mechanisam for the service in our case Azure App Service to have identity in active directory. Once identity is created in azure active directory we can use this grant access to the target resources which is azure storage account in our case. It is also service principal but this is special kind of service principal. There are benifits compared to service principal.

1.  We do not have to expiry about service principal - Automatic credential rotation
2.  Identity Lifecycle management - whenever we are done using App Service or App service is deleted, Identity associated with App service automatically gets deleted. 

So we ended up with storing no secretes in our code and authentication happens automatically.

Lets see how we can enable this. 

create one app service and deploy the code into app service. 
Let side window,under Under settings enable system Managed identities. 
In the Azure portal, go into your storage account to grant your web app access. Select Access control (IAM) in the left pane, and then select Role assignments. You'll see a list of who has access to the storage account. Now you want to add a role assignment to a robot, the app service that needs access to the storage account. Select Add > Add role assignment.

In Role, select Storage Blob Data Contributor to give your web app access to read storage blobs. In Assign access to, select App Service. In Subscription, select your subscription. Then select the app service you want to provide access to. Select Save.
```
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
```
When the above code deployed to our app service, this code will run and lists the blobs without passing any credentials and secretes. 

The DefaultAzureCredential class is used to get a token credential for your code to authorize requests to Azure Storage. Create an instance of the DefaultAzureCredential class, which uses the managed identity to fetch tokens and attach them to the service client. 

