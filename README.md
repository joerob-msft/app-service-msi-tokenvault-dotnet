---
services: app-service, token-store
platforms: dotnet
author: joerob-msft
---

> Note: "Token Store" for App Service is in Private Preview, and can be accessed by contacting our team, and completing the onboarding process under NDA. Private Preview features like "Token Store" are primarily meant to gather feedback, and should not be used in production. Support may be discontinued in the future.

# Using "Token Store" from an App Service Site (with a Managed Service Identity)

## Prerequisites
To deploy this sample, you need the following:
1. An Azure subscription to create App Service and Token Store resources.
2. Completion of the App Service "Token Store" Private Preview onboarding process (Please contact our team for further instructions).

## Step 1: Create a Dropbox developer app

1. Go to the [Dropbox developer portal](https://www.dropbox.com/developers).
2. **Sign in** using the link on top-right of the web site. **[Sign up](https://www.dropbox.com/register)** if you do not have an account already.
3. [Create a new app](https://www.dropbox.com/developers/apps/create), choose **Dropbox API**, **Full Dropbox** access, and create a unique name for your app.
4. Record the **App key** and **App secret** values for future use.
5. Set the redirect URI to `https://[token-store-name].tokenstore.azure.net/redirect` where `[token-store-name]` is the name of your token store, that you will create in the next step. 

## Step 2: Create an App Service with a Managed Service Identity (MSI)
<a href="https://portal.azure.com/#create/Microsoft.Template/uri/https%3A%2F%2Fraw.githubusercontent.com%2Fjoerob-msft%2Fapp-service-msi-tokenvault-dotnet%2Fmaster%2Fazuredeploy.json" target="_blank">
    <img src="http://azuredeploy.net/deploybutton.png"/>
</a>
<a href="http://armviz.io/#/?load=https%3A%2F%2Fraw.githubusercontent.com%2Fjoerob-msft%2Fapp-service-msi-tokenvault-dotnet%2Fmaster%2Fazuredeploy.json" target="_blank">
    <img src="http://armviz.io/visualizebutton.png"/>
</a>

1. Use the **"Deploy to Azure"** button to deploy an ARM template, creating the following resources:
    1. App Service web app [with Managed Service Identity](https://docs.microsoft.com/en-us/azure/app-service/app-service-managed-service-identity).
    2. "Token Store" containing service and token resources, as well as an access policy resource that grants the App Service access to **Get Secrets**.
1. Select the **Subscription** that has been enrolled in the private preview.
1. Enter a new or existing **Resource group** name.
1. Enter a unique **Web Site Name**.
1. Enter a unique **Token Store Name**.
1. **Dropbox App Key** and **Dropbox App Secret** fields require values obtained in previous steps under **App key** and **App secret**.
1. Other defaulted template fields (no changes needed):
    1. Under BASICS, the resource group **Location** can be defaulted to *West Central US*. This location can be different from the Web site or Token Store locations.
    1. **Sku name** will determine how much you get charged for the web app, and is defaulted to **F1** for free hosting. Select **D1** or greater if your limit for free instances is met or exceeded.
    1. **Token Store Location** is defaulted to *West Central US*. The service is only available in this region.
    1. **Web App Location** is defaulted to *West US 2*. Feel free to choose from available options.
1. Review terms and conditions, if you agree, click **Purchase** to deploy the template.
1. Click **deployment in progress** under the **Notifications** tab to monitor progress. It usually takes under 2 minutes to complete.
1. Once deployment is completed successfully, go to target **Resource group** in Azure Portal and review the created resources. You should see an App Service resource and a Token Store resource (Click **Show Hidden Types** to see Token Store resource).

At this point you have a running Web App and an integrated "Token Store" that can hold an access token for DropBox.
1. Browse the deployed web site.
1. Click Login to authenticate the token and see the DropBox List Folder API call result.

### Common issues

|Name| Error| Resolution|
|-----|------|------------|
|Dropbox error (400)| Invalid redirect_uri: "https://[token-store-name].tokenstore.azure.net/redirect": It must exactly match one of the redirect URIs you've pre-configured for your app (including the path).| Add the above URI as a **Redirect URI** on your Dropbox app registration |
|Redirect error from Dropbox or other service| See [this issue](https://github.com/Azure/azure-tokens/issues/1) for details.| Multiple root causes: Issue #1 - Incorrect app secret - After redeploying with the correct value everything worked.|

# Local debugging of web site that accesses "Token Store"

## Prerequisites
To run this sample locally, you need the following:
1. Same prerequisite items as above, an Azure subscription and going through the "Token Store" onboarding process.
1. An updated version of Visual Studio 2017
1. Git commandline tool
1. [Azure CLI 2.0](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli?view=azure-cli-latest) to run the application on your local development machine.

The steps below are to modify the web app and/or run the web app locally.

## Step 0: Deploy "Token Store" and App Service as above
If you have not already done so as shown above, use the "Deploy to Azure" button to deploy an ARM template to create the App Service and Token Store resources. Otherwise feel free to use the same `[token-store-name]` for local debugging.

## Step 1: Clone the repo 
Clone the repo to your development machine, by going to the appropriate root folder and running the following command:

```
git clone https://github.com/joerob-msft/app-service-msi-tokenvault-dotnet.git
```

## Step 2: Open the Web Project in VS and update settings 
In the Web.config file, change the tokenstorename in the key tokenResourceUrl to the one you just created. Replace **TokenStoreName** with the name of your Token Store, where `[token-store-name]` is the name of your token store, that you creatde in the previous step.

```xml   
<add key="tokenResourceUrl" value="https://[token-store-name].tokenstore.azure.net/services/dropbox/tokens/sampleToken" />
```
The project has the following relevant Nuget packages:
1. `Microsoft.Azure.Services.AppAuthentication` - makes it easy to fetch access tokens for Service-to-Azure-Service authentication scenarios. 
1. `Dropbox.Api` - makes it easy make calls to Dropbox API. 

The relevant code is in `WebAppTokenVault/WebAppTokenVault/Controllers/HomeController.cs` file. The **AzureServiceTokenProvider** class (which is part of Microsoft.Azure.Services.AppAuthentication) tries the following methods to get an access token:
1. Managed Service Identity (MSI) - for scenarios where the code is deployed to Azure, and the Azure resource supports MSI. 
2. [Azure CLI](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli?view=azure-cli-latest) (for local development) - Azure CLI version `2.0.12` and above supports the **get-access-token** option. AzureServiceTokenProvider uses this option to get an access token for local development. 
3. Active Directory Integrated Authentication (for local development). To use integrated Windows authentication, your domain’s Active Directory must be federated with Azure Active Directory. Your application must be running on a domain-joined machine under a user’s domain credentials.

```csharp    
        public async System.Threading.Tasks.Task<ActionResult> Index()
        {
            var azureServiceTokenProvider = new AzureServiceTokenProvider();

            // token Url - e.g. "https://tokenstorename.tokenstore.azure.net/services/dropbox/tokens/sampleToken"
            var storeUrl = $"{ConfigurationManager.AppSettings["tokenResourceUrl"]}";
            var tokenResourceUrl = $"{storeUrl}/services/dropbox/tokens/sampleToken";

            ViewBag.LoginLink = $"{tokenResourceUrl}/login?PostLoginRedirectUrl={this.Request.Url}";

            try
            {
                string apiToken = await azureServiceTokenProvider.GetAccessTokenAsync(storeUrl);
                var request = new HttpRequestMessage(HttpMethod.Post, $"{tokenResourceUrl}");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiToken);

                var response = await client.SendAsync(request);
                var responseString = await response.Content.ReadAsStringAsync();

                var token = JsonConvert.DeserializeObject<Token>(responseString);

                ViewBag.Secret = $"Token: {token.Value?.AccessToken}";

                ViewBag.FileList = response.IsSuccessStatusCode ? await this.ListDropboxFolderContents(token.Value?.AccessToken) : new List<string>();
            }
            catch (Exception exp)
            {
                ViewBag.Error = $"Something went wrong: {exp.InnerException?.Message}";
                ViewBag.FileList = new List<string>();
            }

            ViewBag.Principal = azureServiceTokenProvider.PrincipalUsed != null ? $"Principal Used: {azureServiceTokenProvider.PrincipalUsed}" : string.Empty;

            return View();
        }
```

## Step 3: Run the application on your local development machine
`AzureServiceTokenProvider` will use the developer's security context to get a token to authenticate to Token Store. This removes the need to create a service principal, and share it with the development team. It also prevents credentials from being checked in to source code. 
`AzureServiceTokenProvider` will use **Azure CLI** or **Active Directory Integrated Authentication** to authenticate to Azure AD to get a token. That token will be used to fetch the secret from Azure Token Store. 

Azure CLI will work if the following conditions are met:
 1. You have [Azure CLI 2.0](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli?view=azure-cli-latest) installed. Version 2.0.12 supports the get-access-token option used by AzureServiceTokenProvider. If you have an earlier version, please upgrade. 
 2. You are logged into Azure CLI. You can login using **az login** command.
 
Azure Active Directory Authentication will only work if the following conditions are met:
 1. Your on-premise active directory is synced with Azure AD. 
 2. You are running this code on a domain joined machine.   

Since your developer account has access to the Token Store, you should see the secret on the web page. Principal Used will show type "User" and your user account. 

At this point you have a running Web App and an integrated "Token Store" that can hold an access token for DropBox.
1. Browse the deployed site
1. Click Login to authenticate the token and see the DropBox List Folder API call result.

>Note: You can also use a service principal to run the application on your local development machine. See the section "Running the application using a service principal" later in the tutorial on how to do this. 

### Common issues

|Name| Error| Resolution|
|-----|------|------------|
|Web Project NuGet package restore| Build error `Could not restore NuGet packages`| Select **Restore NuGet packages** after right clicking the solution. |
|`System.IO.DirectoryNotFoundException` runtime error| Runtime error page: `Exception Details: System.IO.DirectoryNotFoundException: Could not find a part of the path 'C:\[project-path]\bin\roslyn\csc.exe'.`| **Clean solution** after right clicking the solution, and press **F5** again.|


## Step 4: Deploy the Web App to Azure
Use any of the methods outlined on [Deploy your app to Azure App Service](https://docs.microsoft.com/en-us/azure/app-service-web/web-sites-deploy) to publish the Web App to Azure. 
After you deploy it, browse to the web app. You should see the secret on the web page, and this time the Principal Used will show "App", since it ran under the context of the App Service. The AppId of the MSI will be displayed. 

# Troubleshooting

## Common issues during local development:

1. Azure CLI is not installed, or you are not logged in, or you do not have the latest version. 
Run **az account get-access-token** to see if Azure CLI shows a token for you. If it says no such program found, please install Azure CLI 2.0. If you have installed it, you may be prompted to login. 

2. AzureServiceTokenProvider cannot find the path for Azure CLI.
AzureServiceTokenProvider finds Azure CLI at its default install locations. If it cannot find Azure CLI, please set environment variable **AzureCLIPath** to the Azure CLI installation folder. AzureServiceTokenProvider will add the environment variable to the Path environment variable.

3. You are logged into Azure CLI using multiple accounts, or the same account has access to subscriptions in multiple tenants. You get an Access Denied error when trying to fetch secret from Token Store during local development. 
Using Azure CLI, set the default subscription to one which has the account you want use, and is in the same tenant as your Token Store: **az account set --subscription [subscription-id]**. If no output is seen, then it succeeded. Verify the right account is now the default using **az account list**.

## Common issues when deployed to Azure App Service:

1. MSI is not setup on the App Service. 

Check the environment variables MSI_ENDPOINT and MSI_SECRET exist using [Kudu debug console](https://azure.microsoft.com/en-us/resources/videos/super-secret-kudu-debug-console-for-azure-web-sites/). If these environment variables do not exist, MSI is not enabled on the App Service. 

## Common issues across environments:

1. Access denied

The principal or user used does not have access to the Token Store. The principal used in shown on the web page. Grant that user (in case of developer context) or application "Get secret" access to the Token Store. The MSI identity of the web app is added to the Token Store at deployment time, and the user that is running the deployment is also added. To add another identity you should modify the arm template and deploy again to the same store.

## Running the application using a service principal in local development environment
>Note: It is recommended to use your developer context for local development, since you do not need to create or share a service principal for that. If that does not work for you, you can use a service principal, but do not check in the certificate or secret in source repos. Instead share them using a secure mechanism like Key Store.

To run the application using a service principal in the local development environment, follow these steps

Service principal using a certificate:
1. Create a service principal certificate. Follow steps [here](https://docs.microsoft.com/en-us/azure/key-vault/key-vault-use-from-web-application) to create a service principal and grant it permissions to the Token Store. 
2. Set an environment variable named **AzureServicesAuthConnectionString** to **RunAs=App;AppId=AppId;TenantId=TenantId;CertificateThumbprint=Thumbprint;CertificateStoreLocation=CurrentUser**. 
You need to replace AppId, TenantId, and Thumbprint with actual values from step #1.
3. Run the application in your local development environment. No code change is required. AzureServiceTokenProvider will use this environment variable and use the certificate to authenticate to Azure AD. 

Service principal using a password:
1. Create a service principal with a password. Follow steps [here](https://docs.microsoft.com/en-us/azure/key-vault/key-vault-use-from-web-application) to create a service principal and grant it permissions to the Token Store. 
2. Set an environment variable named **AzureServicesAuthConnectionString** to **RunAs=App;AppId=AppId;TenantId=TenantId;AppKey=Secret**. You need to replace AppId, TenantId, and Secret with actual values from step #1. 
3. Run the application in your local development environment. No code change is required. AzureServiceTokenProvider will use this environment variable and use the service principal to authenticate to Azure AD. 
