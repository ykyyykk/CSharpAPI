using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using CSharpAPI.Utilities;
using Newtonsoft.Json.Linq;

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
                  // TODOWarning: 這裡讀取的時間跟NodeJs不一樣 
                  date = dataReader["date"],
                  value = dataReader["value"],
                  itemID = dataReader["itemID"],
                  category = dataReader["category"],
               };
               revenues.Add(revenue);
            }
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
            return Ok(new APIResponse(true, "成功新增營收"));
         }
         catch (Exception exception)
         {
            return ExceptionHandler.HandleException(exception);
         }
      }

      [HttpGet("itemssoldamount")]
      public async Task<IActionResult> ItemsSoldAmount()
      {
         var salesMap = new Dictionary<long, int>();
         try
         {
            await connection.OpenAsync();
            using var command =
            new MySqlCommand(@"SELECT itemID, COUNT(*) as soldAmount
                               FROM Revenue
                               GROUP BY itemID",
                               connection);
            using var dataReader = await command.ExecuteReaderAsync();
            while (await dataReader.ReadAsync())
            {
               var itemId = dataReader.GetInt64("itemID");
               var soldAmount = dataReader.GetInt32("soldAmount");
               salesMap[itemId] = soldAmount;
            }
            return Ok(new { success = true, sales = salesMap });
         }
         catch (Exception exception)
         {
            return ExceptionHandler.HandleException(exception);
         }
      }
   }
}