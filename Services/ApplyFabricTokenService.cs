using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using telebirr_payment_integration.Models;

namespace telebirr_payment_integration.Services
{
    public class ApplyFabricTokenService
    {
        private readonly PaymentCredentialsModel paymentCredentials;

        public ApplyFabricTokenService(IOptions<PaymentCredentialsModel> _paymentCredentials)
        {
            paymentCredentials = _paymentCredentials.Value;
        }

        public async Task<String> ApplyFabricToken()
        {
            HttpClientHandler httpClientHandler = new HttpClientHandler();
            httpClientHandler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => { return true; };
            HttpClient client = new HttpClient(httpClientHandler);

            var values = new Dictionary<string, string>
            {
                { "appSecret", paymentCredentials.AppSecret }
            };
            var content = new FormUrlEncodedContent(values);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.Add("X-APP-Key", paymentCredentials.FabricAppId);

            var response = await client.PostAsync(paymentCredentials.BaseUrl + "/payment/v1/token/", content);

            var responseString = await response.Content.ReadAsStringAsync();
            return responseString;
        }
    }
}
