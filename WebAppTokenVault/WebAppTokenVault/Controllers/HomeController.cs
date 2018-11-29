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
using WebAppTokenVault.Models;

namespace WebAppTokenVault.Controllers
{
    public class HomeController : Controller
    {
        const string TokenVaultResource = "https://tokenvault.azure.net";
        // static client to have connection pooling
        private static HttpClient client = new HttpClient();

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

        private async Task<List<string>> GetDocuments(string token)
        {
            var filesList = new List<string>();

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

            return filesList;
        }
    }
}