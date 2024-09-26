// 這是目前GoogleSignIn的狀態 
// GoogleJsonWebSignature.ValidateAsync 如果取消會有什麼後果 
// 因為我只是需要他的 content.sub 和 email 回傳就好

// public async Task<IActionResult> GoogleSignIn()
// {
//    Console.WriteLine($"accessToken: GoogleSignIn");
//    try
//    {
//       using var reader = new StreamReader(Request.Body);
//       var requestBody = await reader.ReadToEndAsync();
//       var json = JObject.Parse(requestBody);
//       string token = (string)json["token"];

//       // 使用 access_token 調用 Google tokeninfo API 以獲取 id_token
//       using var httpClient = new HttpClient();
//       var response = await httpClient.GetAsync($"https://oauth2.googleapis.com/tokeninfo?access_token={token}");
//       if (!response.IsSuccessStatusCode)
//       {
//          return Unauthorized("Invalid access token");
//       }

//       // Console.WriteLine($"response: {response}");
//       var content = await response.Content.ReadAsStringAsync();
//       Console.WriteLine($"content: {content}");
//       // content.sub
//       // content.email
//       var tokenInfo = JsonConvert.DeserializeObject<GoogleTokenInfo>(content); // Deserialize to your token info model
//       var idToken = tokenInfo.id_token;
//       Console.WriteLine($"tokenInfo: {tokenInfo.UserId}");
//       Console.WriteLine($"idToken: {tokenInfo.id_token}");

//       // 對 id_token 進行驗證
//       var payload = await GoogleJsonWebSignature.ValidateAsync(idToken, new GoogleJsonWebSignature.ValidationSettings()
//       {
//          Audience = new[] { _googleClientId }
//       });
//       Console.WriteLine($"payload: {payload}");

//       return Ok(payload); // 驗證成功後返回 payload
//    }
//    catch (Exception exception)
//    {
//       return ExceptionHandler.HandleException(exception);
//    }
// }

// content:
// {
//   "azp": "80477468988-2fldqciks5d038o7i2qrbvssoa7entnt.apps.googleusercontent.com",
//   "aud": "80477468988-2fldqciks5d038o7i2qrbvssoa7entnt.apps.googleusercontent.com",
//   "sub": "109348961960736109184",
//   "scope": "https://www.googleapis.com/auth/userinfo.email https://www.googleapis.com/auth/userinfo.profile openid",
//   "exp": "1727332678",
//   "expires_in": "3598",
//   "email": "louise87276@gmail.com",
//   "email_verified": "true",
//   "access_type": "online"
// }