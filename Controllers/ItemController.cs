using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using System.Text.Json;
using Newtonsoft.Json.Linq;

namespace CSharpAPI.Controllers
{
   [ApiController]
   // 因為這一串必須將呼叫的api改成
   // http://localhost:5000/api/Item/getallitem
   [Route("api")]
   public class ItemController : ControllerBase
   {
      private readonly MySqlConnection connection;

      // 使用了builder.Services.AddControllers()
      // 並且在 app.MapControllers() 中啟用了控制器路由
      // 框架應該會自動註冊和調用 ItemController 所以不需要new
      public ItemController(MySqlConnection connection)
      {
         this.connection = connection;
      }

      //或者說 直接這樣 然後取消上面的[Route("api")]
      // [HttpGet("/api/getallitem")]
      [HttpGet("getallitem")]
      public async Task<IActionResult> GetAllItem()
      {
         try
         {
            await connection.OpenAsync();
            using var command = new MySqlCommand("select * from Item", connection);
            using var dataReader = await command.ExecuteReaderAsync();
            List<dynamic> rows = new List<dynamic>();

            while (await dataReader.ReadAsync())
            {
               var item = new
               {
                  id = dataReader["id"],
                  name = dataReader["name"],
                  detail = dataReader["detail"],
                  price = dataReader["price"],
                  stock = dataReader["stock"],
                  category = dataReader["category"],
                  status = dataReader["status"],
                  saleAmount = dataReader["saleAmount"],
                  thumbnail = dataReader["thumbnail"],
               };
               rows.Add(item);
            }
            // return Ok(new APIResponse(true, "成功取得所有物品", rows));
            return Ok(new { success = true, items = rows });
         }
         catch (JsonException)
         {
            return BadRequest(new { success = false, message = "Invalid JSON format" });
         }
         catch (Exception exception)
         {
            return BadRequest(new { success = false, message = $"錯誤{exception.Message}" });
         }
      }

      [HttpPost("getitemimage")]
      public async Task<IActionResult> GetItemImage()
      {
         try
         {
            using var reader = new StreamReader(Request.Body);
            var requestBody = await reader.ReadToEndAsync();
            var json = JObject.Parse(requestBody);
            string itemID = (string)json["itemID"];

            await connection.OpenAsync();

            using var command = new MySqlCommand("SELECT * FROM Image WHERE itemID = @itemID", connection);
            command.Parameters.AddWithValue("@itemID", itemID);
            using var dataReader = await command.ExecuteReaderAsync();

            List<dynamic> rows = new List<dynamic>();

            while (await dataReader.ReadAsync())
            {
               var row = new
               {
                  itemID = dataReader["itemID"],
                  imageUrl = dataReader["imageUrl"],
               };
               rows.Add(row);
            }
            return Ok(new { success = true, items = rows });
         }
         catch (JsonException)
         {
            return BadRequest(new { success = false, message = "Invalid JSON format" });
         }
         catch (Exception exception)
         {
            return BadRequest(new { success = false, message = $"錯誤{exception.Message}" });
         }
      }

      [HttpPost("purchaseitem")]
      public async Task<IActionResult> PurchaseItem()
      {
         try
         {
            using var reader = new StreamReader(Request.Body);
            var requestBody = await reader.ReadToEndAsync();
            var json = JObject.Parse(requestBody);
            var id = (string)json["id"];
            int amount = (int)json["amount"];

            await connection.OpenAsync();

            // 查詢商品
            Item item = null;
            //為了在同一個Method內執行兩次MySqlCommand
            using (var command = new MySqlCommand("SELECT * FROM Item WHERE id = @id", connection))
            {
               command.Parameters.AddWithValue("@id", id);
               using var dataReader = await command.ExecuteReaderAsync();

               if (await dataReader.ReadAsync())
               {
                  item = new Item()
                  {
                     id = (string)dataReader["id"],
                     name = (string)dataReader["name"],
                     detail = (string)dataReader["detail"],
                     price = Convert.ToInt32(dataReader["price"]),
                     stock = Convert.ToInt32(dataReader["stock"]),
                     category = (string)dataReader["category"],
                     status = (string)dataReader["status"],
                     saleAmount = Convert.ToInt32(dataReader["saleAmount"]),
                     thumbnail = (string)dataReader["thumbnail"],
                  };
               }
            }

            if (item == null)
            {
               return NotFound(new APIResponse(false, "找不到物品"));
            }

            if (item.stock < amount)
            {
               return Ok(new APIResponse(false, "庫存不足"));
            }

            // 更新商品
            item.stock -= amount;
            item.saleAmount += amount;
            using (var updateCommand = new MySqlCommand(
               "UPDATE Item SET stock = @stock, saleAmount = @saleAmount WHERE id = @id", connection))
            {
               updateCommand.Parameters.AddWithValue("@stock", item.stock);
               updateCommand.Parameters.AddWithValue("@saleAmount", item.saleAmount);
               updateCommand.Parameters.AddWithValue("@id", item.id);
               await updateCommand.ExecuteNonQueryAsync();
            }

            return Ok(new APIResponse(true, "減少物品庫存 並 增加銷量"));
         }
         catch (JsonException)
         {
            return BadRequest(new APIResponse(false, "Invalid JSON format"));
         }
         catch (Exception exception)
         {
            return BadRequest(new APIResponse(false, $"錯誤: {exception.Message}"));
         }
         finally
         {
            await connection.CloseAsync();
         }
      }
      //TODO: /getitemimage
      //TODO: /item/:id
      //TODO: /insertmultipleimages
      //TODO: /addnewitem
   }
}

public class Item
{
   public string id;
   public string name;
   public string detail;
   public int price;
   public int stock;
   public string category;
   public string status;
   public int saleAmount;
   public string thumbnail;
}