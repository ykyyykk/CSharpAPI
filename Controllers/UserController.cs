using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using CSharpAPI.Utilities;

// TODO: 這邊都還沒試 在家試會比較方便
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
   }
}