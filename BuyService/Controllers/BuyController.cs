using System.Net.Http.Headers;
using System.Text;
using Microsoft.AspNetCore.Mvc;

namespace BuyService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BuyController : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<BuyController> _logger;
        public BuyController(IHttpClientFactory httpClientFactory, ILogger<BuyController> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        [HttpPost]
        public IActionResult Buy(OrderedProduct model)
        {
            HttpResponseMessage response = null, response1 = null;
            try
            {
                _logger.LogInformation("Start calling Buy function");
                var correlationID = Request.Headers["CorrelationId"].ToString();

                var inventoryHttpClient = _httpClientFactory.CreateClient();
                inventoryHttpClient.BaseAddress = new Uri("https://localhost:7200/api/");
                inventoryHttpClient.DefaultRequestHeaders.Accept.Add(
                    new MediaTypeWithQualityHeaderValue("application/json"));

                string exportJsonRequest = "{\"productId\":\"" + model.ProductId + "\"," +
                                           "\"quantity\":" + model.Quantity + "}";
                var request = new StringContent(exportJsonRequest, Encoding.UTF8, "application/json");
                request.Headers.Add("CorrelationId", correlationID);
                response = inventoryHttpClient.PostAsync("inventory/export", request).Result;

                

                var shippingHttpClient = _httpClientFactory.CreateClient();
                shippingHttpClient.BaseAddress = new Uri("https://localhost:7113/api/");
                shippingHttpClient.DefaultRequestHeaders.Accept.Add(
                    new MediaTypeWithQualityHeaderValue("application/json"));
                string jsonRequest = "{\"address\":\"" + model.Address + "\"," +
                                     "\"sender\":\"" + model.OrderName + "\"," +
                                     "\"receiver\":\"" + model.Receiver + "\"," +
                                     "\"productName\":\"" + model.ProductName + "\"}";
                var request1 = new StringContent(jsonRequest, Encoding.UTF8, "application/json");
                request1.Headers.Add("CorrelationId", correlationID);
                response1 = shippingHttpClient.PostAsync("shipping/ship", request1).Result;

                if (response1.IsSuccessStatusCode && response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("End calling Buy function");
                    return Ok();
                }
            }
            catch
            {
                _logger.LogError("There is an exception");
                return StatusCode(500, new {message = response!.Content.ToString()});
            }
            _logger.LogError("Something wrong");
            return BadRequest();
        }
    }
}