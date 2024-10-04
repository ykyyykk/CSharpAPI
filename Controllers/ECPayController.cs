using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using Newtonsoft.Json.Linq;
using CSharpAPI.Utilities;

namespace CSharpAPI.Controllers
{
   [ApiController]
   [Route("api")]
   public class ECPayController : ControllerBase
   {
      private readonly MySqlConnection connection;

      public ECPayController(MySqlConnection connection)
      {
         this.connection = connection;
      }

      [HttpPost("return")]
      public async Task<IActionResult> Return()
      {
         Console.WriteLine("Return=============");
         try
         {
            using var reader = new StreamReader(Request.Body);
            var requestBody = await reader.ReadToEndAsync();
            Console.WriteLine($"Request Body Content: {requestBody}");
            // MerchantID = 3002607 &
            // MerchantTradeNo = od20241004140451036 &
            // PaymentDate = 2024 % 2F10 % 2F04 + 14 % 3A05 % 3A01 &
            // PaymentType = WebATM_TAISHIN & PaymentTypeChargeFee = 2 &
            // RtnCode = 1 &
            // RtnMsg = Succeeded &
            // SimulatePaid = 0 &
            // StoreID = aaa &
            // TradeAmt = 199 &
            // TradeDate = 2024 % 2F10 % 2F04 + 14 % 3A04 % 3A51 &
            // TradeNo = 2410041404515154 &
            // CheckMacValue = 42EE1FAA8177C0B950FBA4EF6CEF6D0C12E7E14EB57F113C72BF779EB111B504

            // 解析 form-urlencoded 格式的資料
            var formData = System.Web.HttpUtility.ParseQueryString(requestBody);
            var merchantID = formData["MerchantID"];
            var merchantTradeNo = formData["MerchantTradeNo"];
            var checkMacValue = formData["CheckMacValue"];

            await connection.OpenAsync();
            using var command = new MySqlCommand("SELECT * FROM ECPay WHERE checkMacValue = ?", connection);
            command.Parameters.AddWithValue("?", formData["CheckMacValue"]);
            using var dataReader = await command.ExecuteReaderAsync();

            if (await dataReader.ReadAsync())
            {
               return new ContentResult
               {
                  Content = "1|OK",
                  ContentType = "text/plain",
                  StatusCode = 200
               };
            }

            //這樣寫會Error
            // return Ok("1|OK", "text/plain");
            // 會顯示1|OK但不知道 之後要幹嘛
            // return Ok("1|OK");
            return Ok(new APIResponse(false, "找不到相同的MacValue"));
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

            await connection.OpenAsync();
            using var command = new MySqlCommand(
               @"INSERT INTO ECPay
                (paymentType, tradeDate, totalAmount, itemName, checkMacValue)
               VALUES(?,?,?,?,?)",
               connection);
            command.Parameters.AddWithValue("?", paymentType);
            command.Parameters.AddWithValue("?", tradeDate);
            command.Parameters.AddWithValue("?", totalAmount);
            command.Parameters.AddWithValue("?", itemName);
            command.Parameters.AddWithValue("?", checkMacValue);
            await command.ExecuteNonQueryAsync();

            return Ok(new APIResponse(true, ""));
         }
         catch (Exception exception)
         {
            return ExceptionHandler.HandleException(exception);
         }
      }
   }
}