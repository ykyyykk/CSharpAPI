using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using System.Text.Json;
using Newtonsoft.Json.Linq;
using System.Data;

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

		[HttpDelete("deletefromcart")]
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

				int rowEffect = await command.ExecuteNonQueryAsync();

				if (rowEffect <= 0)
				{
					return NotFound(new APIResponse(false, "找不到物品"));

				}
				return Ok(new APIResponse(true, "成功刪除"));
			}
			catch (JsonException)
			{
				Console.WriteLine("JsonException");
				return BadRequest(new APIResponse(false, "Invalid JSON format"));
			}
			catch (Exception exception)
			{
				Console.WriteLine("Exception");
				return StatusCode(500, new APIResponse(false, $"錯誤: {exception.Message}"));
			}
		}

		[HttpGet("getcartitems")]
		public async Task<IActionResult> GetCartItems()
		{
			try
			{
				//不懂為什麼用在client端用params包起來就要用Query 雖然看起來比較好用沒錯
				var userID = (string)Request.Query["userID"];

				await connection.OpenAsync();

				using var command = new MySqlCommand("SELECT Item.*, Cart.buyAmount FROM Cart JOIN Item ON Cart.itemID = Item.id WHERE Cart.userID = @userID", connection);

				command.Parameters.AddWithValue("@userID", userID);

				using var dataReader = await command.ExecuteReaderAsync();

				var rows = new List<dynamic>();
				while (await dataReader.ReadAsync())
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
						buyAmount = dataReader["buyAmount"],
					};
					rows.Add(row);
				}
				//不要在這邊加NotFound 會讓前端Alert
				return Ok(new { success = true, items = rows });
			}
			catch (JsonException)
			{
				Console.WriteLine("JsonException");
				return BadRequest(new APIResponse(false, "Invalid JSON format"));
			}
			catch (Exception exception)
			{
				Console.WriteLine("Exception");
				return StatusCode(500, new APIResponse(false, $"錯誤: {exception.Message}"));
			}
		}
	}
}