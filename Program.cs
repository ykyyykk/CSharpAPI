using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", policy =>
    {
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
    });
});

//將Mysql注入DIContainer
// builder.Services.AddMySqlDataSource("Server=192.168.38.128;User ID=aaa;Password=aaa;Database=ShoppingWebsite");
builder.Services.AddMySqlDataSource("Server=34.82.250.51;User ID=aaa;Password=louise87276;Database=ShoppingWebsite");
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
// 添加 Controller 支持
builder.Services.AddControllers();

var app = builder.Build();

//使用Cors跨域設定
app.UseCors("CorsPolicy");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapControllers();

app.MapPost("/api/test", async (HttpContext context, MySqlConnection connection) =>
{
    // dotnet run 時會錯誤
    // logger.LogInformation("Test endpoint called at {time}", DateTime.UtcNow);
    Console.WriteLine("Test");
    Console.WriteLine("sss");
    Console.WriteLine("bbb");
    Console.WriteLine("ccc");
    await context.Response.WriteAsJsonAsync(new { message = "Test successful", time = DateTime.UtcNow });
});

app.MapPost("/api/checkverification", async (HttpContext context, MySqlConnection connection) =>
{
    try
    {
        using var reader = new StreamReader(context.Request.Body);
        var requestBody = await reader.ReadToEndAsync();
        // 將Json反序列化成VerificationRequest
        var request = JsonSerializer.Deserialize<VerificationRequest>(requestBody);

        await connection.OpenAsync();

        using var command = new MySqlCommand("SELECT * FROM Verification WHERE email = @email AND code = @code", connection);
        command.Parameters.AddWithValue("@email", request.Email);
        command.Parameters.AddWithValue("@code", request.VerificationCode);

        using var dataReader = await command.ExecuteReaderAsync();

        if (await dataReader.ReadAsync())
        {
            var row = new
            {
                id = dataReader.GetString("id"),
                email = dataReader.GetString("email"),
                code = dataReader.GetString("code")
            };
            await context.Response.WriteAsJsonAsync(new { success = true, row = row });
        }
        else
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            await context.Response.WriteAsJsonAsync(new { success = false, message = "驗證碼錯誤" });
        }
    }
    catch (JsonException)
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        await context.Response.WriteAsJsonAsync(new { success = false, message = "Invalid JSON format" });
    }
    catch (Exception exception)
    {
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await context.Response.WriteAsJsonAsync(new { success = false, message = $"錯誤: {exception.Message}", error = exception.Message });
    }
});

app.Run("http://*:5000");