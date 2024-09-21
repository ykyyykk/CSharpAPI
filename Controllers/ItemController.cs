using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using System.Text.Json;
using Newtonsoft.Json.Linq;
using CSharpAPI.Utilities;

namespace CSharpAPI.Controllers
{
   [ApiController]
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

      [HttpPost("addnewitem")]
      public async Task<IActionResult> AddNewItem()
      {
         try
         {
            using var reader = new StreamReader(Request.Body);
            var requestBody = await reader.ReadToEndAsync();
            var json = JObject.Parse(requestBody);
            string id = (string)json["id"];
            string name = (string)json["name"];
            string detail = (string)json["detail"];
            string category = (string)json["category"];
            string price = (string)json["price"];
            string stock = (string)json["stock"];
            string status = (string)json["status"];
            string thumbnail = (string)json["thumbnail"];

            await connection.OpenAsync();

            using var command =
            new MySqlCommand("INSERT INTO Item (id,name, detail, category, price, stock, status, thumbnail) VALUES(@id,@name,@detail,@category,@price,@stock,@status,@thumbnail)"
            , connection);
            command.Parameters.AddWithValue("@id", id);
            command.Parameters.AddWithValue("@name", name);
            command.Parameters.AddWithValue("@detail", detail);
            command.Parameters.AddWithValue("@category", category);
            command.Parameters.AddWithValue("@price", price);
            command.Parameters.AddWithValue("@stock", stock);
            command.Parameters.AddWithValue("@status", status);
            command.Parameters.AddWithValue("@thumbnail", thumbnail);

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

      [HttpPost("insertmultipleimages")]
      public async Task<IActionResult> InsertMultipleImages()
      {
         Console.WriteLine("InsertMultipleImages");
         try
         {
            using var reader = new StreamReader(Request.Body);
            var requestBody = await reader.ReadToEndAsync();
            var json = JObject.Parse(requestBody);
            string itemID = (string)json["itemID"];
            var imageUrls = json["imageUrls"].ToObject<List<string>>();

            await connection.OpenAsync();

            // 插入每个 imageUrl
            foreach (var imageUrl in imageUrls)
            {
               using var command = new MySqlCommand("INSERT INTO Image (itemID, imageUrl) VALUES(@itemID, @imageUrl)", connection);
               command.Parameters.AddWithValue("@itemID", itemID);
               command.Parameters.AddWithValue("@imageUrl", imageUrl);
               await command.ExecuteNonQueryAsync(); // 执行插入操作
            }

            // 返回插入成功的结果
            return Ok(new { success = true });
         }
         catch (Exception exception)
         {
            return ExceptionHandler.HandleException(exception);
         }
      }

      [HttpGet("item/{id}")]
      public async Task<IActionResult> Item(string id)
      {
         try
         {
            await connection.OpenAsync();
            using var command = new MySqlCommand("SELECT * FROM Item WHERE id = ?", connection);
            command.Parameters.AddWithValue("@itemID", id);

            using var dataReader = await command.ExecuteReaderAsync();

            if (await dataReader.ReadAsync())
            {
               var row = new
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
               return Ok(new { success = true, item = row });
            }
            return Ok(new APIResponse(false, "no item found"));
         }
         catch (Exception exception)
         {
            return ExceptionHandler.HandleException(exception);
         }
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
         catch (Exception exception)
         {
            return ExceptionHandler.HandleException(exception);
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
                  imageUrl = dataReader["imageUrl"],
                  itemID = dataReader["itemID"],
               };
               rows.Add(row);
            }
            if (rows.Count <= 0)
            {
               return Ok(new APIResponse(false, "no items found"));
            }
            return Ok(new { success = true, items = rows });
         }
         catch (Exception exception)
         {
            return ExceptionHandler.HandleException(exception);
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
         catch (Exception exception)
         {
            return ExceptionHandler.HandleException(exception);
         }
         finally
         {
            await connection.CloseAsync();
         }
      }

      [HttpDelete("deletefromdatabase/{itemID}/{userID}")]
      public async Task<IActionResult> DeleteFromDatabase(string itemID, string userID)
      {
         try
         {
            if (userID != "6")
            {
               return Ok(new APIResponse(false, "不是管理這不能下架"));
            }

            await connection.OpenAsync();

            using var command =
            new MySqlCommand("DELETE FROM Item WHERE id = @id", connection);

            command.Parameters.AddWithValue("@id", itemID);

            int rowEffect = await command.ExecuteNonQueryAsync();

            if (rowEffect <= 0)
            {
               return Ok(new APIResponse(false, "找不到物品"));
            }
            return Ok(new
            {
               success = true,
               affectedRows = rowEffect
            });
         }
         catch (Exception exception)
         {
            return ExceptionHandler.HandleException(exception);
         }
      }
      //TODO: /uploadimage
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