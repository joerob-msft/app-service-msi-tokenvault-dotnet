---
services: app-service, token-vault
platforms: dotnet
author: joerob-msft
---

> Note: The App Service Token Vault feature is in Private Preview, which means it will only be available to you after contacting our team, and completing an onboarding process. Private Preview features like Token Vault are meant for evaulation only, and should not be used in production. This feature may be discontinued at any time without prior notice. 

# Use Token Vault from App Service with Managed Service Identity

## Prerequisites
To run and deploy this sample, you need the following:
1. An Azure subscription to create an App Service and a Token Vault.
2. Completing the App Service Token Vault Private Preview onboarding process (Please contact our team for further instructions).
3. [Azure CLI 2.0](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli?view=azure-cli-latest) to run the application on your local development machine.

## Step 1: Create an App Service with a Managed Service Identity (MSI)
<a href="https://portal.azure.com/#create/Microsoft.Template/uri/https%3A%2F%2Fraw.githubusercontent.com%2Fjoerob-msft%2Fapp-service-msi-tokenvault-dotnet%2Fmaster%2Fazuredeploy.json" target="_blank">
    <img src="http://azuredeploy.net/deploybutton.png"/>
</a>

Use the "Deploy to Azure" button to deploy an ARM template to create the following resources:
1. App Service with MSI.
2. Token Vault with a service and token, and an access policy that grants the App Service access to **Get Secrets**.
>Note: When filling out the template you will see a textbox labelled 'Token Vault Service Client Id' and 'Token Vault Service Client Secret'. Enter a client Id and Secret there. A service with the name 'dropbox' and value from what you entered will be created in the Token Vault.

Review the resources created using the Azure portal. You should see an App Service and a Token Vault. View the access policies of the Token Vault to see that the App Service has access to it. 

## Step 2: Grant yourself data plane access to the Token Vault
Using the Azure Portal, go to the Token Vault's access policies, and grant yourself **Secret Management** access to the Token Vault. This will allow you to run the application on your local development machine. 

1.	Search for your Token Vault in “Search Resources dialog box” in Azure Portal.
2.	Select "Overview", and click on Access policies
3.	Click on "Add New", select "Secret Management" from the dropdown for "Configure from template"
4.	Click on "Select Principal", add your account 
5.	Click on "OK" to add the new Access Policy, then click "Save" to save the Access Policies

## Step 3: Clone the repo 
Clone the repo to your development machine. 

The project has the following relevant Nuget packages:
1. Microsoft.Azure.Services.AppAuthentication - makes it easy to fetch access tokens for Service-to-Azure-Service authentication scenarios. 

The relevant code is in WebAppTokenVault/WebAppTokenVault/Controllers/HomeController.cs file. The **AzureServiceTokenProvider** class (which is part of Microsoft.Azure.Services.AppAuthentication) tries the following methods to get an access token:-
1. Managed Service Identity (MSI) - for scenarios where the code is deployed to Azure, and the Azure resource supports MSI. 
2. [Azure CLI](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli?view=azure-cli-latest) (for local development) - Azure CLI version 2.0.12 and above supports the **get-access-token** option. AzureServiceTokenProvider uses this option to get an access token for local development. 
3. Active Directory Integrated Authentication (for local development). To use integrated Windows authentication, your domain’s Active Directory must be federated with Azure Active Directory. Your application must be running on a domain-joined machine under a user’s domain credentials.

```csharp    
public async System.Threading.Tasks.Task<ActionResult> Index()
{
    AzureServiceTokenProvider azureServiceTokenProvider = new AzureServiceTokenProvider();

    try
    {
        using (var client = new HttpClient())
        {                    
            string apiToken = await azureServiceTokenProvider.GetAccessTokenAsync("https://tokenvault.azure-int.net");
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiToken}");

            var response = await client.PostAsync("https://tokenvaultname.brazilsouth.tokenvault.azure-int.net/services/dropbox/tokens/tokenname", null);
            var responseString = await response.Content.ReadAsStringAsync();
            
            ViewBag.Secret = $"Token: {responseString}";
        }
    }
    catch (Exception exp)
    {
        ViewBag.Error = $"Something went wrong: {exp.Message}";
    }

    ViewBag.Principal = azureServiceTokenProvider.PrincipalUsed != null ? $"Principal Used: {azureServiceTokenProvider.PrincipalUsed}" : string.Empty;

    return View();
}
```
## Step 4: Replace the token vault name
In the HomeController.cs file, change the Token Vault name to the one you just created. Replace **TokenVaultName** with the name of your Token Vault. 

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
You do not have to worry about renewing access token either, since Token Vault takes care of that.  

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

The principal used does not have access to the Token Vault. The principal used in show on the web page. Grant that user (in case of developer context) or application "Get secret" access to the Token Vault. 

