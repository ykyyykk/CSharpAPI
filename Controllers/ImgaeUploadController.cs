using Microsoft.AspNetCore.Mvc;
using CSharpAPI.Utilities;

namespace CSharpAPI.Controllers
{
   [ApiController]
   [Route("api")]
   public class ImageUploadController : ControllerBase
   {
      private readonly string _imageFolderPath = "/var/www/html/img"; // 圖片存放路徑 GCE的時候放在這邊

      // 圖片存放路徑 在家 localhost的時候放這邊
      // private readonly string _imageFolderPath = @"D:\Desktop\img";
      // 圖片存放路徑 學校 localhost的時候放這邊
      // private readonly string _imageFolderPath = @"C:\Desktop\img";

      [HttpPost("uploadimage")]
      public async Task<IActionResult> UploadImage(List<IFormFile> images)
      {
         try
         {
            if (images == null || images.Count == 0)
            {
               return BadRequest(new { success = false, message = "No images provided" });
            }

            var uploadedFiles = new List<dynamic>();

            foreach (var image in images)
            {
               // 生成文件名，使用时间戳加上原始文件名
               var fileName = $"{DateTime.Now.Ticks}-{image.FileName}";
               var filePath = Path.Combine(_imageFolderPath, fileName);
               Console.WriteLine($"filePath: {filePath}");

               if (!Directory.Exists(_imageFolderPath))
               {
                  Console.WriteLine($"路徑不存在: {_imageFolderPath}");
                  Directory.CreateDirectory(_imageFolderPath);
               }

               // 将图片保存到目标文件夹
               using (var stream = new FileStream(filePath, FileMode.Create))
               {
                  await image.CopyToAsync(stream);
               }

               //這裡不要動它 為了跟JS一樣
               uploadedFiles.Add(new { filename = fileName });
            }

            // 返回上传成功的文件信息
            return Ok(new { success = true, message = "圖片上傳成功", files = uploadedFiles });
         }
         catch (Exception exception)
         {
            return ExceptionHandler.HandleException(exception);
         }
      }
   }
}