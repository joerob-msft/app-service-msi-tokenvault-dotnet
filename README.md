---
services: app-service, token-vault
platforms: dotnet
author: joerob-msft
---

> Note: The App Service "Token Vault" feature is in Private Preview, which means it will only be available to you after contacting our team, and completing an onboarding process. Private Preview features like "Token Vault" are only meant for gathering feedback from users, and should not be used in production. This feature may be discontinued at any time without prior notice. 

# Using "Token Vault" from an App Service Website (with a Managed Service Identity)

## Prerequisites
To deploy this sample, you need the following:
1. An Azure subscription to create App Service and Token Vault resources.
2. Completing the App Service "Token Vault" Private Preview onboarding process (Please contact our team for further instructions).
3. [Azure CLI 2.0](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli?view=azure-cli-latest) to run the application on your local development machine.

## Step 1: Create a Dropbox developer app

1. Go to the [Dropbox developer portal](https://www.dropbox.com/developers).
2. *Sign in* using the link on top of the page. **[Sign up](https://www.dropbox.com/register)** if you do not have an account already.
3. [Create a new app](https://www.dropbox.com/developers/apps/create), choose **Dropbox API**, **Full Dropbox** access, and create a unique name for your app.
4. Record the **App key** and **App secret** vaules for future use.
5. Set the redirect URI to `https://[token-vault-name].brazilsouth.tokenvault.azure-int.net/redirect` where `[token-vault-name] is the name of your token vault that you will create in the name step.

## Step 2: Create an App Service with a Managed Service Identity (MSI)
<a href="https://portal.azure.com/#create/Microsoft.Template/uri/https%3A%2F%2Fraw.githubusercontent.com%2Fjoerob-msft%2Fapp-service-msi-tokenvault-dotnet%2Fmaster%2Fazuredeploy.json" target="_blank">
    <img src="http://azuredeploy.net/deploybutton.png"/>
</a>
<a href="http://armviz.io/#/?load=https%3A%2F%2Fraw.githubusercontent.com%2Fjoerob-msft%2Fapp-service-msi-tokenvault-dotnet%2Fmaster%2Fazuredeploy.json" target="_blank">
    <img src="http://armviz.io/visualizebutton.png"/>
</a>


1. Use the "Deploy to Azure" button to deploy an ARM template to create the following resources:
- [App Service with Managed Service Identity](https://docs.microsoft.com/en-us/azure/app-service/app-service-managed-service-identity).
- Token Vault with a service and token, and an access policy that grants the App Service access to **Get Secrets**.
2. When filling out the template you will see a textbox labelled **tokenVaultDropboxServiceClientId** and **tokenVaultDropboxServiceClientId**. Enter the values **App key** and **App secret** from previous step.
[Open Issue] The Token Vault resource is not visible in the UI. 3. Review the resources created using the Azure portal. You should see App Service and Token Vault resources. View the access policies of the Token Vault to see that the App Service has access to it. 

# Advanced - Locally debugging the website accessing "Token Vault"

## Prerequisites
To run this sample locally, you need the following:
1. Same as above, an Azure subscription and going through the "Token Vault" onboarding process.
3. [Azure CLI 2.0](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli?view=azure-cli-latest) to run the application on your local development machine.

Use the "Deploy to Azure" button to deploy an ARM template to create the following resources:
1. App Service with MSI and the sample web app from this repository deployed.
2. Token Vault with a service and token, and an access policy that grants the App Service access to **Get Secrets**.
>Note: When filling out the template you will see a textbox labelled 'Dropbox App Key' and 'Dropbox App Secret'. Enter a the App Key and App Secret from the Dropbox App created in Step 1 above. A service with the name 'dropbox' will be created in the Token Vault.

Review the resources created using the Azure portal. You should see an App Service and a Token Vault (Click Show Hidden Types to see Token Vault resource).


At this point you have a running Web App and an integrated Token Vault that can hold an access token for DropBox. Click Login to authenticate the token and see the DropBox List Folder API call result.

The steps below are to modify the web app and/or run the web app locally.

## Step 3: Clone the repo 
Clone the repo to your development machine. 

The project has the following relevant Nuget packages:
1. Microsoft.Azure.Services.AppAuthentication - makes it easy to fetch access tokens for Service-to-Azure-Service authentication scenarios. 
1. Dropbox.Api - makes it easy make calls to Dropbox API. 

The relevant code is in WebAppTokenVault/WebAppTokenVault/Controllers/HomeController.cs file. The **AzureServiceTokenProvider** class (which is part of Microsoft.Azure.Services.AppAuthentication) tries the following methods to get an access token:-
1. Managed Service Identity (MSI) - for scenarios where the code is deployed to Azure, and the Azure resource supports MSI. 
2. [Azure CLI](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli?view=azure-cli-latest) (for local development) - Azure CLI version 2.0.12 and above supports the **get-access-token** option. AzureServiceTokenProvider uses this option to get an access token for local development. 
3. Active Directory Integrated Authentication (for local development). To use integrated Windows authentication, your domain’s Active Directory must be federated with Azure Active Directory. Your application must be running on a domain-joined machine under a user’s domain credentials.

```csharp    
public async System.Threading.Tasks.Task<ActionResult> Index()
    {
        var azureServiceTokenProvider = new AzureServiceTokenProvider();

        // token Url - e.g. "https://tokenvaultname.westcentralus.tokenvault.azure.net/services/dropbox/tokens/sampleToken"
        string tokenResourceUrl = ConfigurationManager.AppSettings["tokenResourceUrl"];
        ViewBag.LoginLink = $"{tokenResourceUrl}/login?PostLoginRedirectUrl={this.Request.Url}";

        try
        {
            string apiToken = await azureServiceTokenProvider.GetAccessTokenAsync(TokenVaultResource);
            var request = new HttpRequestMessage(HttpMethod.Post, tokenResourceUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiToken);

            var response = await client.SendAsync(request);
            var responseString = await response.Content.ReadAsStringAsync();

            var token = JsonConvert.DeserializeObject<Token>(responseString);

            ViewBag.Secret = $"Token: {token.Value?.AccessToken}";

            ViewBag.FileList = await this.GetDocuments(token.Value?.AccessToken);
        }
        catch (Exception exp)
        {
            ViewBag.Error = $"Something went wrong: {exp.InnerException?.Message}";
        }

        ViewBag.Principal = azureServiceTokenProvider.PrincipalUsed != null ? $"Principal Used: {azureServiceTokenProvider.PrincipalUsed}" : string.Empty;

        return View();
    }
```
## Step 4: Replace the token vault name
In the Web.config file, change the tokenvaultname in the key tokenResourceUrl to the one you just created. Replace **TokenVaultName** with the name of your Token Vault. 

```xml   
<add key="tokenResourceUrl" value="https://tokenvaultname.westcentralus.tokenvault.azure.net/services/dropbox/tokens/sampleToken" />
```
## Step 5: Run the application on your local development machine
AzureServiceTokenProvider will use the developer's security context to get a token to authenticate to Token Vault. This removes the need to create a service principal, and share it with the development team. It also prevents credentials from being checked in to source code. 
AzureServiceTokenProvider will use **Azure CLI** or **Active Directory Integrated Authentication** to authenticate to Azure AD to get a token. That token will be used to fetch the secret from Azure Token Vault. 

Azure CLI will work if the following conditions are met:
 1. You have [Azure CLI 2.0](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli?view=azure-cli-latest) installed. Version 2.0.12 supports the get-access-token option used by AzureServiceTokenProvider. If you have an earlier version, please upgrade. 
 2. You are logged into Azure CLI. You can login using **az login** command.
 
Azure Active Directory Authentication will only work if the following conditions are met:
 1. Your on-premise active directory is synced with Azure AD. 
 2. You are running this code on a domain joined machine.   

Since your developer account has access to the Token Vault, you should see the secret on the web page. Principal Used will show type "User" and your user account. 

You can also use a service principal to run the application on your local development machine. See the section "Running the application using a service principal" later in the tutorial on how to do this. 

## Step 6: Deploy the Web App to Azure
Use any of the methods outlined on [Deploy your app to Azure App Service](https://docs.microsoft.com/en-us/azure/app-service-web/web-sites-deploy) to publish the Web App to Azure. 
After you deploy it, browse to the web app. You should see the secret on the web page, and this time the Principal Used will show "App", since it ran under the context of the App Service. 
The AppId of the MSI will be displayed. 

## Summary
The web app was successfully able to get a token at runtime from Azure Token Vault using your developer account during development, and using MSI when deployed to Azure, without any code change between local development environment and Azure. 
You do not have to worry about renewing access token before using it to call dropbox since Token Vault takes care of that.  

## Running the application using a service principal in local development environment
>Note: It is recommended to use your developer context for local development, since you do not need to create or share a service principal for that. If that does not work for you, you can use a service principal, but do not check in the certificate or secret in source repos, and share them securely.

To run the application using a service principal in the local development environment, follow these steps

Service principal using a certificate:
1. Create a service principal certificate. Follow steps [here](https://docs.microsoft.com/en-us/azure/key-vault/key-vault-use-from-web-application) to create a service principal and grant it permissions to the Token Vault. 
2. Set an environment variable named **AzureServicesAuthConnectionString** to **RunAs=App;AppId=AppId;TenantId=TenantId;CertificateThumbprint=Thumbprint;CertificateStoreLocation=CurrentUser**. 
You need to replace AppId, TenantId, and Thumbprint with actual values from step #1.
3. Run the application in your local development environment. No code change is required. AzureServiceTokenProvider will use this environment variable and use the certificate to authenticate to Azure AD. 

Service principal using a password:
1. Create a service principal with a password. Follow steps [here](https://docs.microsoft.com/en-us/azure/key-vault/key-vault-use-from-web-application) to create a service principal and grant it permissions to the Token Vault. 
2. Set an environment variable named **AzureServicesAuthConnectionString** to **RunAs=App;AppId=AppId;TenantId=TenantId;AppKey=Secret**. You need to replace AppId, TenantId, and Secret with actual values from step #1. 
3. Run the application in your local development environment. No code change is required. AzureServiceTokenProvider will use this environment variable and use the service principal to authenticate to Azure AD. 

## Troubleshooting

### Common issues during local development:

1. Azure CLI is not installed, or you are not logged in, or you do not have the latest version. 
Run **az account get-access-token** to see if Azure CLI shows a token for you. If it says no such program found, please install Azure CLI 2.0. If you have installed it, you may be prompted to login. 

2. AzureServiceTokenProvider cannot find the path for Azure CLI.
AzureServiceTokenProvider finds Azure CLI at its default install locations. If it cannot find Azure CLI, please set environment variable **AzureCLIPath** to the Azure CLI installation folder. AzureServiceTokenProvider will add the environment variable to the Path environment variable.

3. You are logged into Azure CLI using multiple accounts, or the same account has access to subscriptions in multiple tenants. You get an Access Denied error when trying to fetch secret from Token Vault during local development. 
Using Azure CLI, set the default subscription to one which has the account you want use, and is in the same tenant as your Token Vault: **az account set --subscription [subscription-id]**. If no output is seen, then it succeeded. Verify the right account is now the default using **az account list**.

### Common issues when deployed to Azure App Service:

1. MSI is not setup on the App Service. 

Check the environment variables MSI_ENDPOINT and MSI_SECRET exist using [Kudu debug console](https://azure.microsoft.com/en-us/resources/videos/super-secret-kudu-debug-console-for-azure-web-sites/). If these environment variables do not exist, MSI is not enabled on the App Service. 

### Common issues across environments:

1. Access denied

The principal or user used does not have access to the Token Vault. The principal used in shown on the web page. Grant that user (in case of developer context) or application "Get secret" access to the Token Vault. The MSI identity of the web app is added to the Token Vault at deployment time, and the user that is running the deployment is also added. To add another identity you should modify the arm template and deploy again to the same vault.

