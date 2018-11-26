using System;
using System.Web.Mvc;
using System.Net.Http;
using System.Net.Http.Headers;
using Microsoft.Azure.Services.AppAuthentication;
using System.Configuration;

namespace WebAppTokenVault.Controllers
{
    public class HomeController : Controller
    {
        const string TokenVaultResource = "https://tokenvault.azure-int.net";
        // static client to have connection pooling
        private static HttpClient client = new HttpClient();

        public async System.Threading.Tasks.Task<ActionResult> Index()
        {
            var azureServiceTokenProvider = new AzureServiceTokenProvider();

            // token Url - e.g. "https://tokenvaultname.brazilsouth.tokenvault.azure-int.net/services/dropbox/tokens/tokenname"
            string tokenResourceUrl = ConfigurationManager.AppSettings["tokenResourceUrl"];
            ViewBag.LoginLink = $"{tokenResourceUrl}/login?PostLoginRedirectUrl={this.Request.Url}";

            try
            {
                string apiToken = await azureServiceTokenProvider.GetAccessTokenAsync(TokenVaultResource);
                var request = new HttpRequestMessage(HttpMethod.Post, tokenResourceUrl);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiToken);

                var response = await client.SendAsync(request);
                var responseString = await response.Content.ReadAsStringAsync();

                ViewBag.Secret = $"Token: {responseString}";
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
    }
}