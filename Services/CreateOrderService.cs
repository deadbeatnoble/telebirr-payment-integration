using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net.Http.Headers;
using System.Runtime.InteropServices.JavaScript;
using System.Security.Cryptography;
using System.Text;
using telebirr_payment_integration.Models;

namespace telebirr_payment_integration.Services
{
    public class CreateOrderService
    {
        private readonly PaymentCredentialsModel paymentCredentials;
        private readonly ApplyFabricTokenService applyFabricToken;

        public static string my_private_key = """
            -----BEGIN PRIVATE KEY-----
            MIIEvgIBADANBgkqhkiG9w0BAQEFAASCBKgwggSkAgEAAoIBAQC/ZcoOng1sJZ4CegopQVCw3HYqqVRLEudgT+dDpS8fRVy7zBgqZunju2VRCQuHeWs7yWgc9QGd4/8kRSLY+jlvKNeZ60yWcqEY+eKyQMmcjOz2Sn41fcVNgF+HV3DGiV4b23B6BCMjnpEFIb9d99/TsjsFSc7gCPgfl2yWDxE/Y1B2tVE6op2qd63YsMVFQGdre/CQYvFJENpQaBLMq4hHyBDgluUXlF0uA1X7UM0ZjbFC6ZIB/Hn1+pl5Ua8dKYrkVaecolmJT/s7c/+/1JeN+ja8luBoONsoODt2mTeVJHLF9Y3oh5rI+IY8HukIZJ1U6O7/JcjH3aRJTZagXUS9AgMBAAECggEBALBIBx8JcWFfEDZFwuAWeUQ7+VX3mVx/770kOuNx24HYt718D/HV0avfKETHqOfA7AQnz42EF1Yd7Rux1ZO0e3unSVRJhMO4linT1XjJ9ScMISAColWQHk3wY4va/FLPqG7N4L1w3BBtdjIc0A2zRGLNcFDBlxl/CVDHfcqD3CXdLukm/friX6TvnrbTyfAFicYgu0+UtDvfxTL3pRL3u3WTkDvnFK5YXhoazLctNOFrNiiIpCW6dJ7WRYRXuXhz7C0rENHyBtJ0zura1WD5oDbRZ8ON4v1KV4QofWiTFXJpbDgZdEeJJmFmt5HIi+Ny3P5n31WwZpRMHGeHrV23//0CgYEA+2/gYjYWOW3JgMDLX7r8fGPTo1ljkOUHuH98H/a/lE3wnnKKx+2ngRNZX4RfvNG4LLeWTz9plxR2RAqqOTbX8fj/NA/sS4mru9zvzMY1925FcX3WsWKBgKlLryl0vPScq4ejMLSCmypGz4VgLMYZqT4NYIkU2Lo1G1MiDoLy0CcCgYEAwt77exynUhM7AlyjhAA2wSINXLKsdFFF1u976x9kVhOfmbAutfMJPEQWb2WXaOJQMvMpgg2rU5aVsyEcuHsRH/2zatrxrGqLqgxaiqPz4ELINIh1iYK/hdRpr1vATHoebOv1wt8/9qxITNKtQTgQbqYci3KV1lPsOrBAB5S57nsCgYAvw+cagS/jpQmcngOEoh8I+mXgKEET64517DIGWHe4kr3dO+FFbc5eZPCbhqgxVJ3qUM4LK/7BJq/46RXBXLvVSfohR80Z5INtYuFjQ1xJLveeQcuhUxdK+95W3kdBBi8lHtVPkVsmYvekwK+ukcuaLSGZbzE4otcn47kajKHYDQKBgDbQyIbJ+ZsRw8CXVHu2H7DWJlIUBIS3s+CQ/xeVfgDkhjmSIKGX2to0AOeW+S9MseiTE/L8a1wY+MUppE2UeK26DLUbH24zjlPoI7PqCJjl0DFOzVlACSXZKV1lfsNEeriC61/EstZtgezyOkAlSCIH4fGr6tAeTU349Bnt0RtvAoGBAObgxjeH6JGpdLz1BbMj8xUHuYQkbxNeIPhH29CySn0vfhwg9VxAtIoOhvZeCfnsCRTj9OZjepCeUqDiDSoFznglrKhfeKUndHjvg+9kiae92iI6qJudPCHMNwP8wMSphkxUqnXFR3lr9A765GA980818UWZdrhrjLKtIIZdh+X1
            -----END PRIVATE KEY-----
            """;

        public CreateOrderService(IOptions<PaymentCredentialsModel> _options, ApplyFabricTokenService _applyFabricToken)
        {
            paymentCredentials = _options.Value;
            applyFabricToken = _applyFabricToken;
        }

        public async Task<string> CreateOrder(string title = "foodisgood", string amount = "512")
        {
            
            HttpClientHandler httpClientHandler = new HttpClientHandler();
            httpClientHandler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => { return true; };
            HttpClient client = new HttpClient(httpClientHandler);

            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            
            dynamic data = JObject.Parse(await applyFabricToken.ApplyFabricToken());
            string token = data.token;

            client.DefaultRequestHeaders.Add("X-APP-Key", paymentCredentials.FabricAppId);
            client.DefaultRequestHeaders.Add("Authorization", token);

            var values = createRequestObject(title, amount);
            
            var jsonValues = JsonConvert.SerializeObject(values);
            var content = new StringContent(jsonValues, Encoding.UTF8, "application/json");

            var response = await client.PostAsync(paymentCredentials.BaseUrl + "/payment/v1/merchant/preOrder", content);
            var responseString = await response.Content.ReadAsStringAsync();
            dynamic responseData = JObject.Parse(responseString);
            string prePayId = responseData.biz_content.prepay_id;
            return createRawRequest(prePayId);

        }

        public Dictionary<String, object> createRequestObject(string title, string amount)
        {
            var biz = new Dictionary<string, string>{
                    { "trans_currency","ETB"},
                    { "total_amount",amount},
                    { "merch_order_id", createTimeStamp()},
                    { "appid",paymentCredentials.MerchantAppId},
                    { "merch_code",paymentCredentials.ShortCode},
                    { "timeout_express","120m"},
                    { "trade_type","InApp"},
                    { "notify_url","https://localhost:7170/Home/Privacy"},
                    { "title",title},
                    { "business_type","BuyGoods"},
                    { "payee_identifier_type","04"},
                    { "payee_identifier",paymentCredentials.ShortCode},
                    { "payee_type","5000"}
                };
            var req = new Dictionary<string, object>
            {
                {"nonce_str",createNonceStr()},
                {"biz_content" , biz },
                {"method","payment.preorder"},
                {"version", "1.0"},
                {"timestamp",createTimeStamp() }
        };
            req.Add("sign", sign(req));
            req.Add("sign_type", "SHA256WithRSA");
            return req;
        }

        public string createRawRequest(string prepayId)
        {
            var maps = new Dictionary<string, string>
            {
                {"appid",paymentCredentials.MerchantAppId },
                {"merch_code",paymentCredentials.ShortCode },
                {"nonce_str",createNonceStr() },
                {"prepay_id",prepayId },
                {"timestamp",createTimeStamp() },
                {"sign_type","SHA256withRSA" }

            };
            string[] raw = { };
            Dictionary<string, string>.KeyCollection keys = maps.Keys;
            foreach (string key in keys)
            {
                raw = raw.Append(key + "=" + maps[key]).ToArray();

            }
            Array.Sort(raw);
            string sorted = string.Join("&", raw);

            return sorted;
        }







        //Needs to be in a separet file names tools


        public string createTimeStamp()
        {
            DateTimeOffset now = (DateTimeOffset)DateTime.UtcNow;
            return now.ToUnixTimeSeconds().ToString();
        }
        public static string createNonceStr()
        {
            string[] chars = { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "a", "b", "c", "d", "e", "f", "g", "h", "i", "j", "k", "l", "m", "n", "o", "p", "q", "r", "s", "t", "u", "v", "w", "x", "y", "z" };
            string str = "";
            for (int i = 0; i < 32; i++)
            {
                Random random = new Random();
                int index = random.Next(32);
                str = str + chars[index];
            }
            return str;
        }
        public string sign(Dictionary<string, object> request)
        {
            string private_key = my_private_key;
            string[] exclude_fields = { "sign", "sign_type", "header", "refund_info", "openType", "raw_request" };
            string[] requestJoin = { };
            Dictionary<string, object>.KeyCollection keys = request.Keys;
            foreach (string key in keys)
            {
                if (key.Contains(exclude_fields[0]) || key.Contains(exclude_fields[1]) || key.Contains(exclude_fields[2]) || key.Contains(exclude_fields[3]) || key.Contains(exclude_fields[4]) || key.Contains(exclude_fields[5]))
                {
                    continue;
                }
                if (key == "biz_content")
                {
                    Dictionary<string, string> biz_content = (Dictionary<string, string>)request["biz_content"];
                    Dictionary<string, string>.KeyCollection biz_keys = biz_content.Keys;

                    foreach (string k in biz_keys)
                    {
                        requestJoin = requestJoin.Append(k + "=" + biz_content[k]).ToArray();
                    }
                }
                else
                {
                    requestJoin = requestJoin.Append(key + "=" + request[key]).ToArray();
                }
            }
            Array.Sort(requestJoin);
            int index = 0;
            string[] temp = requestJoin.ToArray();
            foreach (string key in temp)
            {
                Console.WriteLine(key + " index " + index);
                index++;
            }
            if (temp[7] != null && temp[8] != null && temp[7].Contains("payee_identifier_type") && temp[8].Contains("payee_identifier"))
            {
                temp[7] = requestJoin[8];
                temp[8] = requestJoin[7];
            }
            else
            {
                return "payee_identifier _type and payee_identifier error check the position and do the swap accordingly!";
            }
            string sorted = string.Join("&", temp);
            string signed = SignTool(sorted, private_key);
            return signed;


        }
        public string SignTool(string data, string privateKey)
        {
            using (var rsa = RSA.Create())
            {
                //byte[] privateKeyBytes = Convert.FromBase64String(privateKey);
                rsa.ImportFromPem(my_private_key.ToCharArray());
                var dataToSign = Encoding.UTF8.GetBytes(data);
                var signature = rsa.SignData(dataToSign, HashAlgorithmName.SHA256, RSASignaturePadding.Pss);
                return Convert.ToBase64String(signature);
            }
        }
    }
}
