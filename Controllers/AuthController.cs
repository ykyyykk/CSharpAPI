using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using System.Text.Json;
using Newtonsoft.Json.Linq;

using System;
using System.IO;
using System.Threading.Tasks;
using System.Net.Mail;
using System.Net;
using System.Reflection;


namespace CSharpAPI.Controllers
{
     [ApiController]
     [Route("api")]
     public class AuthController : ControllerBase
     {
          private readonly MySqlConnection connection;
          private readonly SmtpClient smtpClient;

          // 使用了builder.Services.AddControllers()
          // 並且在 app.MapControllers() 中啟用了控制器路由
          // 框架應該會自動註冊和調用 ItemController 所以不需要new
          public AuthController(MySqlConnection connection)
          {
               this.connection = connection;

               smtpClient = new SmtpClient("smtp.gmail.com")
               {
                    Port = 587,
                    Credentials = new NetworkCredential("louise87276@gmail.com", "hssp gwtv aftv otkb"),
                    EnableSsl = true,
               };
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
               // connection 是透過 依賴注入 所以不需要在每個api後面都加上finally
               // finally
               // {
               //      // 确保数据库连接关闭
               //      if (connection.State == System.Data.ConnectionState.Open)
               //      {
               //           await connection.CloseAsync();
               //      }
               // }
          }

          [HttpPost("login")]
          public async Task<IActionResult> Login()
          {
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

          [HttpPost("register")]
          public async Task<IActionResult> Register()
          {
               try
               {
                    using var reader = new StreamReader(Request.Body);
                    var requestBody = await reader.ReadToEndAsync();

                    var json = JObject.Parse(requestBody);
                    var name = (string)json["name"];
                    var phoneNumber = (string)json["phoneNumber"];
                    var email = (string)json["email"];
                    var password = (string)json["password"];

                    await connection.OpenAsync();

                    using var command = new MySqlCommand("INSERT INTO User (name, phoneNumber, email, password) VALUES (@name, @phoneNumber, @email, @password)", connection);

                    command.Parameters.AddWithValue("@name", name);
                    command.Parameters.AddWithValue("@phoneNumber", phoneNumber);
                    command.Parameters.AddWithValue("@email", email);
                    command.Parameters.AddWithValue("@password", password);

                    // 插入時改使用ExecuteNonQueryAsync
                    await command.ExecuteNonQueryAsync();

                    // 取得插入的ID
                    var userID = command.LastInsertedId;
                    return Ok(new { success = true, userID = userID });
               }
               catch (JsonException)
               {
                    return BadRequest(new { success = false, message = "Invalid JSON format" });
               }
               catch (Exception exception)
               {
                    return StatusCode(500, new { success = false, message = $"錯誤{exception.Message}" });
               }
          }

          [HttpPost("sendverification")]
          public async Task<IActionResult> SendVerification()
          {
               try
               {
                    using var reader = new StreamReader(Request.Body);
                    var requestBody = await reader.ReadToEndAsync();

                    var json = JObject.Parse(requestBody);
                    var email = (string)json["email"];

                    await connection.OpenAsync();

                    using var checkCommand = new MySqlCommand("SELECT email FROM User WHERE email = @email", connection);
                    checkCommand.Parameters.AddWithValue("@email", email);

                    // 插入時改使用ExecuteNonQueryAsync
                    using var dataReader = await checkCommand.ExecuteReaderAsync();

                    if (await dataReader.ReadAsync())
                    {
                         return Ok(new { success = false, message = "此信箱已經註冊過了" });
                    }
                    await dataReader.CloseAsync();

                    var random = new Random();
                    int verificationCode = random.Next(100000, 999999);

                    using var insertCommand = new MySqlCommand("INSERT INTO Verification (email, code, createdAt, expiresAt) VALUES(@email, @code, @createdAt, expiresAt)", connection);
                    var createdAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                    var expiresAt = createdAt + 300000;
                    insertCommand.Parameters.AddWithValue("@email", email);
                    insertCommand.Parameters.AddWithValue("@code", verificationCode.ToString());
                    insertCommand.Parameters.AddWithValue("@createdAt", createdAt);
                    insertCommand.Parameters.AddWithValue("@expiresAt", expiresAt);

                    await insertCommand.ExecuteNonQueryAsync();

                    // sendMail不知道怎麼做
                    var mailMessage = new MailMessage
                    {
                         From = new MailAddress("louise872726@gmail.com"),
                         Subject = "你的驗證碼",
                         Body = $"你的驗證碼是: {verificationCode}",
                         IsBodyHtml = false
                    };
                    mailMessage.To.Add(email);

                    await smtpClient.SendMailAsync(mailMessage);

                    return Ok(new { success = true, message = "驗證碼已發送" });
               }
               catch (JsonException)
               {
                    return BadRequest(new { success = false, message = "Invalid JSON format" });
               }
               catch (Exception exception)
               {
                    return StatusCode(500, new { success = false, message = $"錯誤{exception.Message}" });
               }
          }
     }
}

public class VerificationRequest
{
     //一定要get set不然沒辦法Deserialize
     public string email { get; set; }
     public string verificationCode { get; set; }
}