using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using System.Net.Mail;
using System.Net;
using CSharpAPI.Utilities;

namespace CSharpAPI.Controllers
{
   [ApiController]
   [Route("api")]
   public class QuestionController : ControllerBase
   {
      private readonly SmtpClient smtpClient;

      // 使用了builder.Services.AddControllers()
      // 並且在 app.MapControllers() 中啟用了控制器路由
      // 框架應該會自動註冊和調用 ItemController 所以不需要new
      public QuestionController()
      {
         smtpClient = new SmtpClient("smtp.gmail.com")
         {
            Port = 587,
            Credentials = new NetworkCredential("louise87276@gmail.com", "hssp gwtv aftv otkb"),
            EnableSsl = true,
         };
      }

      [HttpPost("sendquestion")]
      public async Task<IActionResult> SendQuestion()
      {
         try
         {
            using var reader = new StreamReader(Request.Body);
            var requestBody = await reader.ReadToEndAsync();
            var json = JObject.Parse(requestBody);
            var name = (string)json["name"];
            var email = (string)json["email"];
            var question = (string)json["question"];

            var mailMessage = new MailMessage
            {
               From = new MailAddress(email),
               Subject = $"{name} 問題",
               Body = $"我是{name}: 問題: {question}",
               IsBodyHtml = false
            };
            mailMessage.To.Add("louise87276@gmail.com");

            await smtpClient.SendMailAsync(mailMessage);

            return Ok(new { success = true, message = "訊息已發送" });
         }
         catch (Exception exception)
         {
            return ExceptionHandler.HandleException(exception);
         }
      }
   }
}