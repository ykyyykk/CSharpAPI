using MySqlConnector;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", policy =>
    {
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
    });
});
// builder.Services.AddMySqlDataSource在appsettings.json設定好了

// 使用 Scoped 生命週期而不是 Transient 為了在同一個method 執行兩次MySqlCommand
// 雖然說不加還是可以 但是可能會 生命週期管理 測試困難 一致性 性能問題
//將Mysql注入DIContainer
builder.Services.AddScoped(sp =>
    new MySqlConnection(builder.Configuration.GetConnectionString("DefaultConnection")));
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

app.Run("http://*:5000");