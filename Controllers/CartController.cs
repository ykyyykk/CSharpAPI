using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using System.Text.Json;
using Newtonsoft.Json.Linq;

namespace CSharpAPI.Controllers
{
     [ApiController]
     [Route("api")]
     public class CartController : ControllerBase
     {
          private readonly MySqlConnection connection;

          public CartController(MySqlConnection connection)
          {
               this.connection = connection;
          }

          [HttpPost("deletefromcart")]
          public async Task<IActionResult> DeleteFromCart()
          {
               try
               {
                    using var reader = new StreamReader(Request.Body);
                    var requestBody = await reader.ReadToEndAsync();
                    var json = JObject.Parse(requestBody);
                    string itemID = (string)json["itemID"];
                    string userID = (string)json["userID"];

                    await connection.OpenAsync();

                    using var command = new MySqlCommand("DELETE FROM Cart WHERE itemID = @itemID AND userID = @userID", connection);

                    command.Parameters.AddWithValue("@itemID", itemID);
                    command.Parameters.AddWithValue("@userID", userID);

                    await command.ExecuteNonQueryAsync();

                    return Ok(new { success = true, });
               }
               catch (JsonException)
               {
                    Console.WriteLine("JsonException");
                    return BadRequest(new { success = false, message = "Invalid JSON format" });
               }
               catch (Exception exception)
               {
                    Console.WriteLine("Exception");
                    return StatusCode(500, new { success = false, message = $"錯誤{exception.Message}" });
               }
          }
     }
}