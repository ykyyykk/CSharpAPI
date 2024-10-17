using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using Newtonsoft.Json.Linq;
using System.Net.Mail;
using System.Net;
using CSharpAPI.Utilities;

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
			using var reader = new StreamReader(Request.Body);
			var requestBody = await reader.ReadToEndAsync();
			var json = JObject.Parse(requestBody);
			var email = (string)json["email"];
			var verificationCode = (string)json["verificationCode"];

			const string sql = "SELECT * FROM Verification WHERE email = ? AND code = ?";
			const string deleteSql = "DELETE FROM Verification WHERE email = ?";

			try
			{
				await connection.OpenAsync();

				// Check verification
				using var command = new MySqlCommand(sql, connection);
				command.Parameters.AddWithValue("?", email);
				command.Parameters.AddWithValue("?", verificationCode);

				using var dataReader = await command.ExecuteReaderAsync();
				if (!await dataReader.ReadAsync())
				{
					return Ok(new { success = false, message = "驗證碼錯誤" });
				}
				dataReader.Close();

				// Delete verification
				command.CommandText = deleteSql;
				command.Parameters.Clear();
				command.Parameters.AddWithValue("?", email);
				await command.ExecuteNonQueryAsync();

				return Ok(new { success = true });
			}
			catch (Exception exception)
			{
				return ExceptionHandler.HandleException(exception);
			}
		}


		[HttpPost("checkforgotpassword")]
		public async Task<IActionResult> CheckForgotPassword()
		{
			using var reader = new StreamReader(Request.Body);
			var requestBody = await reader.ReadToEndAsync();
			var json = JObject.Parse(requestBody);
			var email = (string)json["email"];
			var verificationCode = (string)json["password"];

			const string sql = "SELECT * FROM Verification WHERE email = @email AND code = @code";
			const string selectSql = "SELECT password FROM User WHERE email = @email";
			const string deleteSql = "DELETE FROM Verification WHERE email = @email";

			try
			{
				await connection.OpenAsync();

				// Check verification
				using var command = new MySqlCommand(sql, connection);
				command.Parameters.AddWithValue("@email", email);
				command.Parameters.AddWithValue("@code", verificationCode);

				using var dataReader = await command.ExecuteReaderAsync();
				if (!await dataReader.ReadAsync())
				{
					return Ok(new { success = false, message = "驗證碼錯誤" });
				}
				dataReader.Close();

				// Check user
				command.CommandText = selectSql;
				command.Parameters.Clear();
				command.Parameters.AddWithValue("@email", email);

				using var selectReader = await command.ExecuteReaderAsync();
				if (!await selectReader.ReadAsync())
				{
					return Ok(new { success = false, message = "找不到信箱" });
				}

				var password = selectReader["password"].ToString();
				selectReader.Close();

				// Delete verification
				command.CommandText = deleteSql;
				command.Parameters.Clear();
				command.Parameters.AddWithValue("@email", email);
				await command.ExecuteNonQueryAsync();

				return Ok(new { success = true, row = new { password } });
			}
			catch (Exception exception)
			{
				return ExceptionHandler.HandleException(exception);
			}
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
					// return Ok(new APIResponse(true, "登入成功", user));
				}
				return NotFound(new { success = false, message = "User not found" });

			}
			catch (Exception exception)
			{
				return ExceptionHandler.HandleException(exception);
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
			catch (Exception exception)
			{
				return ExceptionHandler.HandleException(exception);
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

				using var insertCommand =
				new MySqlCommand(@"INSERT INTO Verification
										(email, code, createdAt, expiresAt) 
										VALUES(?, ?, ?, ?)", connection);
				var createdAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
				var expiresAt = createdAt + 300000;
				insertCommand.Parameters.AddWithValue("?", email);
				insertCommand.Parameters.AddWithValue("?", verificationCode.ToString());
				insertCommand.Parameters.AddWithValue("?", createdAt);
				insertCommand.Parameters.AddWithValue("?", expiresAt);

				await insertCommand.ExecuteNonQueryAsync();

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
			catch (Exception exception)
			{
				return ExceptionHandler.HandleException(exception);
			}
		}

		[HttpDelete("deleteexpiresverification")]
		public async Task<IActionResult> DeleteExpiresVerification()
		{
			try
			{
				await connection.OpenAsync();

				using var checkCommand = new MySqlCommand("DELETE FROM Verification WHERE expiresAt <= @expiresAt", connection);
				checkCommand.Parameters.AddWithValue("@expiresAt", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());

				// 插入時改使用ExecuteNonQueryAsync
				await checkCommand.ExecuteNonQueryAsync();

				return Ok(new APIResponse(true, "過期驗證碼已刪除"));
			}
			catch (Exception exception)
			{
				return ExceptionHandler.HandleException(exception);
			}
		}

		[HttpPost("sendforgotpasswordcode")]
		public async Task<IActionResult> SendForgotPasswordcode()
		{
			try
			{
				using var reader = new StreamReader(Request.Body);
				var requestBody = await reader.ReadToEndAsync();
				var json = JObject.Parse(requestBody);
				var email = (string)json["email"];

				await connection.OpenAsync();
				using (var selectCommand = new MySqlCommand("SELECT email FROM User WHERE email = @email", connection))
				{
					selectCommand.Parameters.AddWithValue("@email", email);
					// 插入時改使用ExecuteNonQueryAsync
					using var dataReader = await selectCommand.ExecuteReaderAsync();
					if (!await dataReader.ReadAsync())
					{
						return Ok(new APIResponse(false, "該信箱從未被註冊過"));
					}
				}

				var random = new Random();
				int verificationCode = random.Next(100000, 999999);
				var createdAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
				var expiresAt = createdAt + 300000;
				using (var insertCommand = new MySqlCommand("INSERT INTO Verification (email, code, createdAt, expiresAt) VALUES(@email, @code, @createdAt, @expiresAt)", connection))
				{
					insertCommand.Parameters.AddWithValue("@email", email);
					insertCommand.Parameters.AddWithValue("@code", verificationCode.ToString());
					insertCommand.Parameters.AddWithValue("@createdAt", createdAt);
					insertCommand.Parameters.AddWithValue("@expiresAt", expiresAt);

					await insertCommand.ExecuteNonQueryAsync();
				}

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
			catch (Exception exception)
			{
				return ExceptionHandler.HandleException(exception);
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