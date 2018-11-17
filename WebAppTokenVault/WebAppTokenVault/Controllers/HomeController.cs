using System;
using System.Web.Mvc;
using System.Net.Http;
using System.Net.Http.Headers;
using Microsoft.Azure.Services.AppAuthentication;

namespace WebAppTokenVault.Controllers
{
    public class HomeController : Controller
    {
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