using Google.Apis.Auth;
using Google.Apis.PeopleService.v1;
using Google.Apis.Services;
using Microsoft.AspNetCore.Mvc;
using CSharpAPI.Utilities;
using Google.Apis.Auth.OAuth2;
using Newtonsoft.Json.Linq;

namespace CSharpAPI.Controllers
{
   [ApiController]
   [Route("api")]
   public class GoogleAuthController : ControllerBase
   {
      private readonly string _googleClientId = "80477468988-2fldqciks5d038o7i2qrbvssoa7entnt.apps.googleusercontent.com";

      [HttpPost("googlesignin")]
      public async Task<IActionResult> GoogleSignIn()
      {
         Console.WriteLine($"accessToken: GoogleSignIn");
         try
         {
            using var reader = new StreamReader(Request.Body);
            var requestBody = await reader.ReadToEndAsync();
            var json = JObject.Parse(requestBody);
            string token = (string)json["token"];

            // 使用 access_token 調用 Google tokeninfo API 以獲取 id_token
            using var httpClient = new HttpClient();
            var response = await httpClient.GetAsync($"https://oauth2.googleapis.com/tokeninfo?access_token={token}");
            if (!response.IsSuccessStatusCode)
            {
               return Unauthorized("Invalid access token");
            }

            // Console.WriteLine($"response: {response}");
            var content = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"content: {content}");

            var contentjson = JObject.Parse(content);
            Console.WriteLine((string)contentjson["email"]);
            var user = new
            {
               email = (string)contentjson["email"],
               sub = (string)contentjson["sub"]
            };
            return Ok(new { success = true, user }); // 驗證成功後返回 payload
         }
         catch (Exception exception)
         {
            return ExceptionHandler.HandleException(exception);
         }
      }

      [HttpPost("googleregister")]
      public async Task<IActionResult> GoogleRegister([FromBody] GoogleSignInRequest request)
      {
         try
         {
            var validPayload = await ValidateGoogleToken(request.Token);
            if (validPayload == null)
            {
               return Unauthorized(new { error = "Invalid token" });
            }

            // Repeat People API call for additional registration data if needed
            var peopleService = new PeopleServiceService(new BaseClientService.Initializer
            {
               HttpClientInitializer = GoogleCredential.FromAccessToken(request.Token)
            });

            var peopleInfo = await peopleService.People.Get("people/me")
                .ExecuteAsync();

            var userInfo = new
            {
               Email = validPayload.Email,
               Name = validPayload.Name,
               PhoneNumbers = peopleInfo.PhoneNumbers
            };

            return Ok(new { message = "Google register success", user = userInfo });
         }
         catch (Exception exception)
         {
            return ExceptionHandler.HandleException(exception);
         }
      }

      private async Task<GoogleJsonWebSignature.Payload> ValidateGoogleToken(string token)
      {
         Console.WriteLine($"token: {token}");
         try
         {
            var validPayload = await GoogleJsonWebSignature.ValidateAsync(token, new GoogleJsonWebSignature.ValidationSettings()
            {
               Audience = new[] { _googleClientId }
            });
            Console.WriteLine($"validPayload: ${validPayload}");
            return validPayload;
         }
         catch (Exception ex)
         {
            Console.WriteLine($"Token validation failed: {ex.Message}");
            return null;
         }
      }
   }

   public class GoogleSignInRequest
   {
      public string Token { get; set; }
   }
}