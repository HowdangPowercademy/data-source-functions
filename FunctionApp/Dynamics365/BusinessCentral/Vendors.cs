using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Plumsail.DataSource.Dynamics365.BusinessCentral.Settings;
using System.Text.Json.Nodes;

namespace Plumsail.DataSource.Dynamics365.BusinessCentral
{
    public class Vendors
    {
        private readonly Settings.Vendors _settings;
        private readonly HttpClientProvider _httpClientProvider;
        private readonly ILogger<Vendors> _logger;

        public Vendors(IOptions<AppSettings> settings, HttpClientProvider httpClientProvider, ILogger<Vendors> logger)
        {
            _settings = settings.Value.Vendors;
            _httpClientProvider = httpClientProvider;
            _logger = logger;
        }

        [Function("D365-BC-Vendors")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "get", Route = "bc/vendors/{id?}")] HttpRequest req, Guid? id)
        {
            _logger.LogInformation("D365-BC-Vendors is requested.");

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
                    var vendorsJson = await client.GetStringAsync($"companies({companyId})/vendors");
                    var vendors = JsonValue.Parse(vendorsJson);
                    return new OkObjectResult(vendors?["value"]);
                }

                var vendorResponse = await client.GetAsync($"companies({companyId})/vendors({id})");
                if (!vendorResponse.IsSuccessStatusCode)
                {
                    if (vendorResponse.StatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        return new NotFoundResult();
                    }

                    // throws Exception
                    vendorResponse.EnsureSuccessStatusCode();
                }

                var vendorJson = await vendorResponse.Content.ReadAsStringAsync();
                return new ContentResult()
                {
                    Content = vendorJson,
                    ContentType = "application/json"
                };
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "An error has occured while processing D365-BC-Vendors request.");
                return new StatusCodeResult(ex.StatusCode.HasValue ? (int)ex.StatusCode.Value : StatusCodes.Status500InternalServerError);
            }
        }
    }
}
