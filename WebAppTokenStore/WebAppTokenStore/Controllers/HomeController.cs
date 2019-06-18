using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using System.Net.Http;
using System.Net.Http.Headers;
using Microsoft.Azure.Services.AppAuthentication;
using Dropbox.Api;
using Newtonsoft.Json;
using WebAppTokenStore.Models;

namespace WebAppTokenStore.Controllers
{
    public class HomeController : Controller
    {
        const string TokenStoreResource = "https://tokenstore.azure.net";
        // static client to have connection pooling
        private static HttpClient client = new HttpClient();

        public async System.Threading.Tasks.Task<ActionResult> Index()
        {
            var azureServiceTokenProvider = new AzureServiceTokenProvider();

            // token Url - e.g. "https://tokenstorename.westcentralus.tokenstore.azure.net/services/dropbox/tokens/sampleToken"
            string tokenResourceUrl = ConfigurationManager.AppSettings["tokenResourceUrl"];
            ViewBag.LoginLink = $"{tokenResourceUrl}/login?PostLoginRedirectUrl={this.Request.Url}";

            try
            {
                // Get a token to access Token Store
                string tokenStoreApiToken = await azureServiceTokenProvider.GetAccessTokenAsync(TokenStoreResource);

                // Get Dropbox token from Token Store
                var request = new HttpRequestMessage(HttpMethod.Post, $"{tokenResourceUrl}");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenStoreApiToken);
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

        private async Task<List<string>> ListDropboxFolderContents(string token)
        {
            var filesList = new List<string>();

            if (!string.IsNullOrEmpty(token))
            {
                using (var dbx = new DropboxClient(token))
                {
                    var list = await dbx.Files.ListFolderAsync(string.Empty);

                    // show folders then files
                    foreach (var item in list.Entries.Where(i => i.IsFolder))
                    {
                        filesList.Add($"D  {item.Name}/");
                    }

                    foreach (var item in list.Entries.Where(i => i.IsFile))
                    {
                        filesList.Add($"F  {item.Name}");
                    }
                }
            }

            return filesList;
        }
    }
}