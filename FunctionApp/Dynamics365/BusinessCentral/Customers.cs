using Grpc.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using Microsoft.Graph.Beta;
using Microsoft.Graph.Beta.Models;
using Plumsail.DataSource.Dynamics365.BusinessCentral.Settings;
using Plumsail.DataSource.Dynamics365.CRM;
using System.Collections.Generic;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace Plumsail.DataSource.Dynamics365.BusinessCentral
{
    public class Customers
    {
        private readonly Settings.Customers _settings;
        private readonly HttpClientProvider _httpClientProvider;
        private readonly ILogger<Customers> _logger;

        public Customers(IOptions<AppSettings> settings, HttpClientProvider httpClientProvider, ILogger<Customers> logger)
        {
            _settings = settings.Value.Customers;
            _httpClientProvider = httpClientProvider;
            _logger = logger;
        }

        [Function("D365-BC-Customers")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "get", Route = "bc/customers/{id?}")] HttpRequest req, Guid? id)
        {
            _logger.LogInformation("D365-BC-Customers is requested.");

            try
            {
                var client = _httpClientProvider.Create();
                var companyId = await client.GetCompanyIdAsync(_settings.Company);
                if (companyId == null)
                {
                    return new NotFoundResult();
                }

                if (!id.HasValue)
                {
                    var customersJson = await client.GetStringAsync($"companies({companyId})/customers");
                    var customers = JsonValue.Parse(customersJson);
                    return new OkObjectResult(customers?["value"]);
                }

                var customerResponse = await client.GetAsync($"companies({companyId})/customers({id})");
                if (!customerResponse.IsSuccessStatusCode)
                {
                    if (customerResponse.StatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        return new NotFoundResult();
                    }

                    // throws Exception
                    customerResponse.EnsureSuccessStatusCode();
                }

                var customerJson = await customerResponse.Content.ReadAsStringAsync();
                return new ContentResult()
                {
                    Content = customerJson,
                    ContentType = "application/json"
                };
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "An error has occured while processing D365-BC-Customers request.");
                return new StatusCodeResult(ex.StatusCode.HasValue ? (int)ex.StatusCode.Value : StatusCodes.Status500InternalServerError);
            }
        }
    }
}
