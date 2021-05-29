# Azure App Service calls storage account

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

Go to Secretes and click on Generate/Import. Add sample key and pass Azure Storage Account Connection string and save it.

Go to Access Policy and click on Add Access Policy

Please provide below Options 
Configure From Template - Secrete Management
Key Permisions - GET
Secrete Permissions - GET,LIST
Select Principal - Search your azure ad App with name and click on Add.





