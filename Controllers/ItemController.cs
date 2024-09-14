using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using System.Text.Json;

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
          public async Task<IActionResult> GetAllItems()
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
     }
}