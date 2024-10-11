using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using Newtonsoft.Json.Linq;
using CSharpAPI.Utilities;
using CSharpAPI.Services;
using System.Text;
using Newtonsoft.Json;
using System.Security.Cryptography;
using System.Web;
using Microsoft.Extensions.Options;

namespace CSharpAPI.Controllers
{
   [ApiController]
   [Route("api")]
   public class ECPayController : ControllerBase
   {
      private readonly HttpClient httpClient;
      private const string apiUrl = "https://einvoice-stage.ecpay.com.tw/B2CInvoice/Issue";
      private readonly MySqlConnection connection;
      private readonly ItemService itemService;
      private readonly IOptions<InvoiceSettings> invoiceSettings;
      public ECPayController(IOptions<InvoiceSettings> invoiceSettings, MySqlConnection connection, ItemService itemService, HttpClient httpClient)
      {
         this.invoiceSettings = invoiceSettings;
         this.connection = connection;
         this.itemService = itemService;
         this.httpClient = httpClient;
      }

      [HttpPost("return")]
      public async Task<IActionResult> Return([FromForm] IFormCollection ecpayReturn)
      {
         try
         {
            // Console.WriteLine($"ecpayReturn.ToString(): {ecpayReturn.ToString()}====================================");
            if (ecpayReturn["RtnCode"] != "1")
            {
               Console.WriteLine("付款失敗");
               return Ok(new APIResponse(false, "付款失敗"));
            }

            // 更改ECPay訂單狀態
            await connection.OpenAsync();
            using var command = new MySqlCommand(
               @"UPDATE ECPay
                 SET paid = @paid
                 WHERE merchantTradeNo = @merchantTradeNo;",
               connection);
            // 不能直接使用 ecpayReturn["MerchantTradeNo"] 
            // 返回的是 StringValues 來自 Microsoft.Extensions.Primitives.StringValues 而不是純粹的 string
            string merchantTradeNo = ecpayReturn["MerchantTradeNo"];
            command.Parameters.AddWithValue("@paid", 1);
            command.Parameters.AddWithValue("@merchantTradeNo", merchantTradeNo);
            int rowEffect = await command.ExecuteNonQueryAsync();
            if (rowEffect <= 0)
            {
               return NotFound(new APIResponse(false, "找不到訂單"));
            }

            string itemId = ecpayReturn["CustomField1"];
            int amount = int.Parse(ecpayReturn["CustomField2"]);

            // 減少商品庫存
            var (success, message) = await itemService.PurchaseItemAsync(itemId, amount);
            if (!success)
            {
               return Ok(new APIResponse(false, message));
            }

            //取真的資料太麻煩了 下一次做應該直接把CustomField1裡面直接塞資料庫的 Item Table
            var item = new
            {
               ItemName = $"item: {itemId}",
               ItemCount = amount, // 物品數量
               ItemWord = "個",
               ItemPrice = 1, // 物品價格
               ItemAmount = int.Parse(ecpayReturn["TradeAmt"]), // itemPrice * itemCount
            };
            Invoice invoice = new Invoice(this.invoiceSettings, "customerName", new List<dynamic>() { item });

            // Invoice.data urlencoode AESEncrypt 並 轉json
            // Console.WriteLine(invoice.Data);

            string json = JsonConvert.SerializeObject(invoice, Formatting.Indented);
            // Console.WriteLine(json);

            // 開立發票
            var invoiceResult = await Issue(json);

            return Ok("1|OK");
         }
         catch (Exception exception)
         {
            return ExceptionHandler.HandleException(exception);
         }
      }

      [HttpPost("issue")]
      public async Task<IActionResult> Issue([FromBody] object requestData)
      {
         try
         {
            Console.WriteLine($"Issue=====================");
            Console.WriteLine($"requestData: {requestData}");

            var content = new StringContent(
               requestData.ToString(),
               Encoding.UTF8,
               "application/json"
            );

            var apiResponse = await httpClient.PostAsync(apiUrl, content);
            string responseContent = await apiResponse.Content.ReadAsStringAsync();

            // TODO: RtnMsg 成功的 開立發票成功 解密比對資料失敗
            // string data = ExtractDataFromResponse(responseContent);
            // Console.WriteLine(data);
            // var decryptedData = DecryptData(data);
            // dynamic invoice = null;
            // try
            // {
            //    invoice = JsonConvert.DeserializeObject<dynamic>(responseContent);
            // }
            // catch (Exception ex)
            // {
            //    Console.WriteLine($"無法轉換 responseContent: {ex.Message}");
            // }
            // var decryptedData = DecryptData(invoice.Data);
            // Console.WriteLine($"解密後的資料為: {decryptedData}");

            Console.WriteLine($"回傳數值: {responseContent}");
            Console.WriteLine($"IsSuccess: {apiResponse.IsSuccessStatusCode}");

            if (!apiResponse.IsSuccessStatusCode)
            {
               return StatusCode((int)apiResponse.StatusCode, responseContent);
            }
            // return OK 在Postman會是text 不好看 因為responseContent 是string
            return Content(responseContent, "application/json");
         }
         catch (Exception exception)
         {
            return ExceptionHandler.HandleException(exception);
         }
      }

      [HttpPost("addorder")]
      public async Task<IActionResult> AddOrder()
      {
         try
         {
            using var reader = new StreamReader(Request.Body);
            var requestBody = await reader.ReadToEndAsync();
            var json = JObject.Parse(requestBody);
            var paymentType = (string)json["paymentType"];
            var tradeDate = (string)json["tradeDate"];
            var totalAmount = (string)json["totalAmount"];
            var itemName = (string)json["itemName"];
            var checkMacValue = (string)json["checkMacValue"];
            var merchantTradeNo = (string)json["merchantTradeNo"];

            await connection.OpenAsync();
            using var command = new MySqlCommand(
               @"INSERT INTO ECPay
                (paymentType, tradeDate, totalAmount, itemName, checkMacValue, merchantTradeNo)
               VALUES(?,?,?,?,?,?)",
               connection);
            command.Parameters.AddWithValue("?", paymentType);
            command.Parameters.AddWithValue("?", tradeDate);
            command.Parameters.AddWithValue("?", totalAmount);
            command.Parameters.AddWithValue("?", itemName);
            command.Parameters.AddWithValue("?", checkMacValue);
            command.Parameters.AddWithValue("?", merchantTradeNo);
            await command.ExecuteNonQueryAsync();

            return Ok(new APIResponse(true, ""));
         }
         catch (Exception exception)
         {
            return ExceptionHandler.HandleException(exception);
         }
      }

      public string DecryptData(string encryptedData)
      {
         byte[] keyBytes = Encoding.UTF8.GetBytes("ejCk326UnaZWKisg");
         byte[] ivBytes = Encoding.UTF8.GetBytes("q9jcZX8Ib9LM8wYk");
         byte[] encryptedBytes = Convert.FromBase64String(encryptedData);

         using (Aes aes = Aes.Create())
         {
            aes.Key = keyBytes;
            aes.IV = ivBytes;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using (ICryptoTransform decryptor = aes.CreateDecryptor())
            {
               byte[] decryptedBytes = decryptor.TransformFinalBlock(encryptedBytes, 0, encryptedBytes.Length);
               string decryptedString = Encoding.UTF8.GetString(decryptedBytes);

               // 解碼 URL
               return HttpUtility.UrlDecode(decryptedString);
            }
         }
      }
   }
}