using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using CSharpAPI.Utilities;
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

		[HttpDelete("deletefromcart/{itemID}/{userID}")]
		public async Task<IActionResult> DeleteFromCart(string itemID, string userID)
		{
			try
			{
				await connection.OpenAsync();

				using var command = new MySqlCommand("DELETE FROM Cart WHERE itemID = @itemID AND userID = @userID", connection);

				command.Parameters.AddWithValue("@itemID", itemID);
				command.Parameters.AddWithValue("@userID", userID);

				int rowEffect = await command.ExecuteNonQueryAsync();

				// if (rowEffect <= 0)
				// {
				// 	return NotFound(new APIResponse(false, "找不到物品"));

				// }
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
				// if (rows.Count <= 0)
				// {
				// 	return NotFound(new APIResponse(true, "找不到購物車物品"));
				// }
				//不要在這邊加NotFound 會讓前端Alert
				return Ok(new { success = true, items = rows });
			}
			catch (Exception exception)
			{
				return ExceptionHandler.HandleException(exception);
			}
		}

		[HttpPost("addtocart")]
		public async Task<IActionResult> AddToCart()
		{
			try
			{
				using var reader = new StreamReader(Request.Body);
				var requestBody = await reader.ReadToEndAsync();
				var json = JObject.Parse(requestBody);
				string itemID = (string)json["itemID"];
				string userID = (string)json["userID"];
				string amount = (string)json["amount"];

				await connection.OpenAsync();

				using var command = new MySqlCommand("INSERT INTO Cart (itemID, userID, buyAmount) VALUES(@itemID, @userID, @buyAmount)", connection);

				command.Parameters.AddWithValue("@itemID", itemID);
				command.Parameters.AddWithValue("@userID", userID);
				command.Parameters.AddWithValue("@buyAmount", amount);

				await command.ExecuteNonQueryAsync();

				return Ok(new APIResponse(true, "成功加入購物車"));
			}
			catch (Exception exception)
			{
				return ExceptionHandler.HandleException(exception);
			}
		}

		[HttpPost("changecartamount")]
		public async Task<IActionResult> ChangeCartAmount()
		{
			try
			{
				using var reader = new StreamReader(Request.Body);
				var requestBody = await reader.ReadToEndAsync();
				var json = JObject.Parse(requestBody);
				string itemID = (string)json["itemID"];
				string userID = (string)json["userID"];
				string amount = (string)json["amount"];

				await connection.OpenAsync();

				using var command = new MySqlCommand("UPDATE Cart SET buyAmount = @buyAmount WHERE itemID = @itemID AND userID = @userID", connection);

				command.Parameters.AddWithValue("@buyAmount", amount);
				command.Parameters.AddWithValue("@itemID", itemID);
				command.Parameters.AddWithValue("@userID", userID);

				await command.ExecuteNonQueryAsync();

				return Ok(new APIResponse(true, "成功修改購物車物品數量"));
			}
			catch (Exception exception)
			{
				return ExceptionHandler.HandleException(exception);
			}
		}
	}
}