using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using CSharpAPI.Utilities;
using Newtonsoft.Json.Linq;

// TODO: 這邊都還沒試 在家試會比較方便
namespace CSharpAPI.Controllers
{
   [ApiController]
   [Route("api")]
   public class RevenueController : ControllerBase
   {
      private readonly MySqlConnection connection;

      public RevenueController(MySqlConnection connection)
      {
         this.connection = connection;
      }

      [HttpGet("getallrevenuewithitemcategory")]
      public async Task<IActionResult> GetAllRevenueWithItemCategory()
      {
         try
         {
            await connection.OpenAsync();
            string sql = "SELECT Revenue.*, Item.category FROM Item JOIN Revenue ON Item.id = Revenue.itemID ORDER BY date ASC; ";
            using var command = new MySqlCommand(sql, connection);
            using var dataReader = await command.ExecuteReaderAsync();
            List<dynamic> revenues = new List<dynamic>();

            while (await dataReader.ReadAsync())
            {
               var revenue = new
               {
                  id = dataReader["id"],
                  name = dataReader["name"],
                  phoneNumber = dataReader["phoneNumber"],
                  email = dataReader["email"],
                  password = dataReader["password"],
                  totalPurchaseAmount = dataReader["totalPurchaseAmount"],
                  totalPurchasePrice = dataReader["totalPurchasePrice"],

               };
               revenues.Add(revenue);
            }
            // TODO: 這邊直接給List不知道會不會出錯
            return Ok(new { success = true, revenues });
         }
         catch (Exception exception)
         {
            return ExceptionHandler.HandleException(exception);
         }
      }

      [HttpPost("addrevnue")]
      public async Task<IActionResult> AddRevenue()
      {
         try
         {
            using var reader = new StreamReader(Request.Body);
            var requestBody = await reader.ReadToEndAsync();
            var json = JObject.Parse(requestBody);
            string date = (string)json["date"];
            string value = (string)json["value"];
            string id = (string)json["id"];

            await connection.OpenAsync();

            using var command =
            new MySqlCommand("INSERT INTO Revenue (date,value,itemID) VALUES(@date,value,itemID)"
            , connection);
            command.Parameters.AddWithValue("@date", date);
            command.Parameters.AddWithValue("@value", value);
            command.Parameters.AddWithValue("@itemID", id);

            int rowEffect = await command.ExecuteNonQueryAsync();

            // 返回插入成功的结果
            return Ok(new
            {
               success = true,
               info = new
               {
                  changedRows = rowEffect
               }
            });
         }
         catch (Exception exception)
         {
            return ExceptionHandler.HandleException(exception);
         }
      }
   }
}