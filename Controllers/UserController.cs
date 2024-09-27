using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using CSharpAPI.Utilities;
using Newtonsoft.Json.Linq;

namespace CSharpAPI.Controllers
{
   [ApiController]
   [Route("api")]
   public class UserController : ControllerBase
   {
      private readonly MySqlConnection connection;

      public UserController(MySqlConnection connection)
      {
         this.connection = connection;
      }

      [HttpGet("getalluser")]
      public async Task<IActionResult> GetAllUser()
      {
         try
         {
            await connection.OpenAsync();
            using var command = new MySqlCommand("select * from User", connection);
            using var dataReader = await command.ExecuteReaderAsync();
            List<dynamic> users = new List<dynamic>();

            while (await dataReader.ReadAsync())
            {
               var user = new
               {
                  id = dataReader["id"],
                  name = dataReader["name"],
                  phoneNumber = dataReader["phoneNumber"],
                  email = dataReader["email"],
                  password = dataReader["password"],
                  totalPurchaseAmount = dataReader["totalPurchaseAmount"],
                  totalPurchasePrice = dataReader["totalPurchasePrice"],

               };
               users.Add(user);
            }
            return Ok(new { success = true, users = users });
         }
         catch (Exception exception)
         {
            return ExceptionHandler.HandleException(exception);
         }
      }

      [HttpDelete("deleteuser/{itemID}/{userID}")]
      public async Task<IActionResult> DeleteUser(string itemID, string userID)
      {
         try
         {
            if (userID != "6")
            {
               return Ok(new APIResponse(false, "不是管理者不能刪除"));
            }

            await connection.OpenAsync();

            using var command =
            new MySqlCommand("DELETE FROM User WHERE id = @id", connection);

            command.Parameters.AddWithValue("@id", itemID);

            int rowEffect = await command.ExecuteNonQueryAsync();

            if (rowEffect <= 0)
            {
               return Ok(new APIResponse(false, "找不到會員"));
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

      [HttpPost("updateuserpriceamount")]
      public async Task<IActionResult> UpdateUserPriceAmount()
      {
         try
         {
            using var reader = new StreamReader(Request.Body);
            var requestBody = await reader.ReadToEndAsync();
            var json = JObject.Parse(requestBody);
            string userID = (string)json["userID"];
            string amount = (string)json["amount"];
            string price = (string)json["price"];

            await connection.OpenAsync();

            using var command = new MySqlCommand(@"
            UPDATE User
            SET totalPurchaseAmount = ?,
            totalPurchasePrice = ?
            WHERE id = ?",
            connection);

            // 注意順序
            command.Parameters.AddWithValue("?", amount);
            command.Parameters.AddWithValue("?", price);
            command.Parameters.AddWithValue("?", userID);

            int rowEffect = await command.ExecuteNonQueryAsync();

            if (rowEffect <= 0)
            {
               return Ok(new APIResponse(false, "找不到會員"));
            }
            return Ok(new APIResponse(true, "成功修改會員購買歷史紀錄"));
         }
         catch (Exception exception)
         {
            return ExceptionHandler.HandleException(exception);
         }
      }
   }
}