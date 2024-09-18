using System.Net;
using FirebaseAdmin;
using FirebaseAdmin.Auth;
using Google.Apis.Auth.OAuth2;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace LeadsManagementApiFunction
{
    public class Function1
    {
        private readonly ILogger _logger;

        public Function1(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<Function1>();

            // Initialize Firebase Admin SDK
            FirebaseApp.Create(new AppOptions()
            {
                Credential = GoogleCredential.FromFile("path/to/serviceAccountKey.json")
            });
        }

        [Function("Function1")]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");

            // Extract the token from the Authorization header
            if (!req.Headers.TryGetValues("Authorization", out var authHeaders) || authHeaders == null)
            {
                return CreateUnauthorizedResponse(req);
            }

            var token = authHeaders.FirstOrDefault()?.Split(" ").Last();

            if (string.IsNullOrEmpty(token))
            {
                return CreateUnauthorizedResponse(req);
            }

            try
            {
                // Verify the token
                var decodedToken = await FirebaseAuth.DefaultInstance.VerifyIdTokenAsync(token);
                _logger.LogInformation($"Token verified. UID: {decodedToken.Uid}");

                // Extract userId from query parameters
                var queryParams = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
                var userId = queryParams["userId"];

                if (string.IsNullOrEmpty(userId))
                {
                    return CreateBadRequestResponse(req, "Missing userId parameter.");
                }

                var response = req.CreateResponse(HttpStatusCode.OK);
                response.Headers.Add("Content-Type", "text/plain; charset=utf-8");
                response.WriteString($"Welcome to Azure Functions, user {userId}!");

                return response;
            }
            catch (FirebaseAuthException ex)
            {
                _logger.LogError($"Token verification failed: {ex.Message}");
                return CreateUnauthorizedResponse(req);
            }
        }

        private HttpResponseData CreateUnauthorizedResponse(HttpRequestData req)
        {
            var response = req.CreateResponse(HttpStatusCode.Unauthorized);
            response.Headers.Add("Content-Type", "text/plain; charset=utf-8");
            response.WriteString("Unauthorized request.");
            return response;
        }

        private HttpResponseData CreateBadRequestResponse(HttpRequestData req, string message)
        {
            var response = req.CreateResponse(HttpStatusCode.BadRequest);
            response.Headers.Add("Content-Type", "text/plain; charset=utf-8");
            response.WriteString(message);
            return response;
        }
    }
}
