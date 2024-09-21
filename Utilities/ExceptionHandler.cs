using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace CSharpAPI.Utilities
{
   public static class ExceptionHandler
   {
      public static IActionResult HandleException(Exception exception)
      {
         if (exception is UnauthorizedAccessException)
         {
            return new ObjectResult(new { success = false, message = $"無權限寫入文件: {exception.Message}" })
            {
               StatusCode = StatusCodes.Status500InternalServerError
            };
         }
         if (exception is IOException)
         {
            return new ObjectResult(new { success = false, message = $"文件IO錯誤: {exception.Message}" })
            {
               StatusCode = StatusCodes.Status500InternalServerError
            };
         }
         if (exception is JsonException)
         {
            return new BadRequestObjectResult(new { success = false, message = "Invalid JSON format" });
         }
         // 可以根據需要添加更多的異常類型處理
         return new ObjectResult(new { success = false, message = $"錯誤: {exception.Message}" })
         {
            StatusCode = StatusCodes.Status500InternalServerError
         };
      }
   }
}