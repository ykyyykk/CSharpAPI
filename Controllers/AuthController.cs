using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using System.Text.Json;
using Newtonsoft.Json.Linq;

namespace CSharpAPI.Controllers
{
     [ApiController]
     [Route("api")]
     public class AuthController : ControllerBase
     {
          private readonly MySqlConnection connection;

          // 使用了builder.Services.AddControllers()
          // 並且在 app.MapControllers() 中啟用了控制器路由
          // 框架應該會自動註冊和調用 ItemController 所以不需要new
          public AuthController(MySqlConnection connection)
          {
               this.connection = connection;
          }

          [HttpPost("checkverification")]
          public async Task<IActionResult> CheckVerification()
          {
               try
               {
                    // 读取请求体
                    using var reader = new StreamReader(Request.Body);
                    var requestBody = await reader.ReadToEndAsync();
                    Console.WriteLine($"requestBody: {requestBody}");

                    // 反序列化 JSON 数据 确保大小写不敏感
                    var request = JsonSerializer.Deserialize<VerificationRequest>(requestBody, new JsonSerializerOptions
                    {
                         PropertyNameCaseInsensitive = true
                    });

                    await connection.OpenAsync();

                    using var command = new MySqlCommand("SELECT * FROM Verification WHERE email = @email AND code = @code", connection);

                    command.Parameters.AddWithValue("@email", request.email);
                    command.Parameters.AddWithValue("@code", request.verificationCode);

                    Console.WriteLine($"request.Email: {request.email}");
                    Console.WriteLine($"request.VerificationCode: {request.verificationCode}");
                    using var dataReader = await command.ExecuteReaderAsync();

                    if (await dataReader.ReadAsync())
                    {
                         var row = new
                         {
                              email = dataReader["email"],
                              code = dataReader["code"],
                         };
                         return Ok(new { success = true, row = row });
                    }
                    return NotFound(new { success = false, message = "驗證碼錯誤" });

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

          [HttpPost("login")]
          public async Task<IActionResult> Login()
          {
               Console.WriteLine("Login");
               try
               {
                    using var reader = new StreamReader(Request.Body);
                    var requestBody = await reader.ReadToEndAsync();

                    //需要安裝dotnet add package Newtonsoft.Json
                    //並且using System.Runtime.InteropServices;
                    var json = JObject.Parse(requestBody);
                    //一定要明確轉型 沒轉型之前是JObject 直接拿去用AddWithValue會報錯
                    var email = (string)json["email"];
                    var password = (string)json["password"];
                    //就算取得一個不存在的值也不會報錯

                    await connection.OpenAsync();

                    using var command = new MySqlCommand("SELECT * FROM User WHERE email = @email AND password = @password", connection);

                    command.Parameters.AddWithValue("@email", email);
                    command.Parameters.AddWithValue("@password", password);
                    using var dataReader = await command.ExecuteReaderAsync();

                    if (await dataReader.ReadAsync())
                    {
                         var user = new
                         {
                              id = dataReader["id"],
                              name = dataReader["name"],
                              phoneNumber = dataReader["phoneNumber"],
                              email = dataReader["email"],
                              password = dataReader["password"],
                         };
                         return Ok(new { success = true, user = user });
                    }
                    return NotFound(new { success = false, message = "User not found" });

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

     public class VerificationRequest
     {
          //一定要get set不然沒辦法Deserialize
          public string email { get; set; }
          public string verificationCode { get; set; }
     }
}