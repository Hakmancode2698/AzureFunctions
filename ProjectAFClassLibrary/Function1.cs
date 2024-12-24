using System.Text.Json;
using ClassLibrary;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace ProjectAFClassLibrary
{
    public class Function1
    {
        private readonly ILogger<Function1> _logger;

        public Function1(ILogger<Function1> logger)
        {
            _logger = logger;
        }

        [Function("FACalculator")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req)
        {
            _logger.LogInformation("C# HTTP trigger function received a request.");

            // Initialize request body reader
            string requestBody;
            using (var reader = new StreamReader(req.Body))
            {
                requestBody = await reader.ReadToEndAsync();
            }

            // Deserialize JSON
            CalculationRequest? data;
            try
            {
                data = JsonSerializer.Deserialize<CalculationRequest>(requestBody, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true // Handles case-insensitive JSON
                });
            }
            catch (JsonException ex)
            {
                _logger.LogError($"Invalid JSON format: {ex.Message}");
                var errorResponse = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
                await errorResponse.WriteAsJsonAsync(new { Error = "Invalid JSON format in the request body" });
                return errorResponse;
            }

            // Validate data
            if (data == null || double.IsNaN(data.Number1) || double.IsNaN(data.Number2))
            {
                _logger.LogWarning("Invalid numbers provided in the request.");
                var badRequestResponse = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
                await badRequestResponse.WriteAsJsonAsync(new { Error = "Please provide valid numbers in the request body." });
                return badRequestResponse;
            }

            // Perform calculation using the NuGet package
            try
            {
                double result = Calculator.subtract(data.Number1, data.Number2); // Fixed to use class name

                _logger.LogInformation($"Calculation successful: {data.Number1} - {data.Number2} = {result}");

                var successResponse = req.CreateResponse(System.Net.HttpStatusCode.OK);
                await successResponse.WriteAsJsonAsync(new { Message = "Calculation Successful", Result = result });
                return successResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error during calculation: {ex.Message}");
                var errorResponse = req.CreateResponse(System.Net.HttpStatusCode.InternalServerError);
                await errorResponse.WriteAsJsonAsync(new { Error = "Internal Server Error" });
                return errorResponse;
            }
        }
    }

    public class CalculationRequest
    {
        public double Number1 { get; set; }
        public double Number2 { get; set; }
    }
}
