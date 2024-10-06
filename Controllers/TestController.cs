using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using CSharpAPI.Utilities;

namespace CSharpAPI.Controllers
{
   [ApiController]
   [Route("api")]
   public class TestController : ControllerBase
   {
      private readonly MySqlConnection connection;

      public TestController(MySqlConnection connection)
      {
         this.connection = connection;
      }

      [HttpPost("test")]
      public async Task<IActionResult> Test()
      {
         Console.WriteLine("/api/test");
         try
         {
            await connection.OpenAsync();

            using var command = new MySqlCommand("SELECT * FROM User WHERE email = @email AND password = @password", connection);
            command.Parameters.AddWithValue("@email", "e");
            command.Parameters.AddWithValue("@password", "p");
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

               return Ok(new
               {
                  success = true,
                  message = $"connection.ConnectionString: {connection.ConnectionString}",
                  user
               });
            }

            return Ok(new
            {
               success = false,
               message = $"connection.ConnectionString: {connection.ConnectionString}",
            });
         }
         catch (Exception exception)
         {
            return ExceptionHandler.HandleException(exception);
         }
      }
   }
}