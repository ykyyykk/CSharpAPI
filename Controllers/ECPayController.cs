using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using Newtonsoft.Json.Linq;
using CSharpAPI.Utilities;
using CSharpAPI.Services.Implementations;

namespace CSharpAPI.Controllers
{
   [ApiController]
   [Route("api")]
   public class ECPayController : ControllerBase
   {
      private readonly MySqlConnection connection;
      private readonly ItemService itemService;

      public ECPayController(MySqlConnection connection, ItemService itemService)
      {
         this.connection = connection;
         this.itemService = itemService;
      }

      [HttpPost("return")]
      public async Task<IActionResult> Return([FromForm] IFormCollection ecpayReturn)
      {
         try
         {
            if (ecpayReturn["RtnCode"] != "1")
            {
               Console.WriteLine("付款失敗");
               return Ok(new APIResponse(false, "付款失敗"));
            }

            await connection.OpenAsync();
            using var command = new MySqlCommand(
               @"UPDATE ECPay
                 SET paid = @paid
                 WHERE merchantTradeNo = @merchantTradeNo;",
               connection);

            // 直接使用 ecpayReturn["MerchantTradeNo"] 返回的是 StringValues 來自 Microsoft.Extensions.Primitives.StringValues
            // 而不是純粹的 string
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

            var (success, message) = await itemService.PurchaseItemAsync(itemId, amount);
            if (!success)
            {
               return Ok(new APIResponse(false, message));
            }

            return Ok("1|OK");
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
   }
}