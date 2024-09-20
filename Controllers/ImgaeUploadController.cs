using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.Threading.Tasks;

namespace CSharpAPI.Controllers
{
   [ApiController]
   [Route("api")]
   public class ImageUploadController : ControllerBase
   {
      // private readonly string _imageFolderPath = "/var/www/html/img"; // 图片存放路径
      private readonly string _imageFolderPath = "/tmp"; // 图片存放路径

      [HttpPost("uploadimage")]
      public async Task<IActionResult> UploadImage(List<IFormFile> images)
      {
         Console.WriteLine("UploadImage");
         Console.WriteLine($"images.Count: {images.Count}");
         try
         {
            if (images == null || images.Count == 0)
            {
               return BadRequest(new { success = false, message = "No images provided" });
            }

            Console.WriteLine(0);
            var uploadedFiles = new List<string>();

            foreach (var image in images)
            {
               Console.WriteLine("for");
               // 生成文件名，使用时间戳加上原始文件名
               var fileName = $"{DateTime.Now.Ticks}-{image.FileName}";
               var filePath = Path.Combine(_imageFolderPath, fileName);
               Console.WriteLine($"filePath: {filePath}");

               if (!Directory.Exists(_imageFolderPath))
               {
                  Console.WriteLine($"路徑不存在: {_imageFolderPath}");
                  // Directory.CreateDirectory(_imageFolderPath);
               }

               Console.WriteLine(0.5f);
               // 将图片保存到目标文件夹
               using (var stream = new FileStream(filePath, FileMode.Create))
               {
                  Console.WriteLine("1");
                  await image.CopyToAsync(stream);
               }

               uploadedFiles.Add(fileName);
            }

            Console.WriteLine("2");
            // 返回上传成功的文件信息
            return Ok(new { success = true, message = "圖片上傳成功", files = uploadedFiles });
         }
         catch (UnauthorizedAccessException ex)
         {
            Console.WriteLine($"無權限寫入文件: {ex.Message}");
            return StatusCode(500, new APIResponse(false, $"無權限寫入文件: {ex.Message}"));
         }
         catch (IOException ex)
         {
            Console.WriteLine($"文件IO錯誤: {ex.Message}");
            return StatusCode(500, new APIResponse(false, $"文件IO錯誤: {ex.Message}"));
         }
         catch (Exception ex)
         {
            Console.WriteLine($"圖片上傳失敗: {ex.Message}");
            return StatusCode(500, new APIResponse(false, $"圖片上傳失敗: {ex.Message}"));
         }
      }
   }
}
