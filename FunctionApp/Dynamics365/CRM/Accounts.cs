using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;

namespace Plumsail.DataSource.Dynamics365.CRM
{
    public class Accounts
    {
        private readonly HttpClientProvider _httpClientProvider;

        public Accounts(HttpClientProvider httpClientProvider)
        {
            _httpClientProvider = httpClientProvider;
        }

        [FunctionName("D365-CRM-Accounts")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("Dynamics365-CRM-Accounts is requested.");

            var client = _httpClientProvider.Create();
            var contactsJson = await client.GetStringAsync("accounts");
            var contacts = JObject.Parse(contactsJson);

            return new OkObjectResult(contacts["value"]);
        }
    }
}
