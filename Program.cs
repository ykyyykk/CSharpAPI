using Microsoft.Extensions.FileProviders;
using MySqlConnector;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", policy =>
    {
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
    });
});

// 使用 Scoped 生命週期而不是 Transient 為了在同一個method 執行兩次MySqlCommand
// 雖然說不加還是可以 但是可能會 生命週期管理 測試困難 一致性 性能問題
// 添加 Controller 支持
builder.Services.AddControllers();

// 資料庫連線在appsettings.json設定好了
//將Mysql注入DIContainer
builder.Services.AddScoped(sp =>
    new MySqlConnection(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

//使用Cors跨域設定
app.UseCors("CorsPolicy");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// 會強制在http 加上 s
app.UseHttpsRedirection();
app.MapControllers();

// 启用静态文件访问 為了將商品圖片直接上傳
// app.UseStaticFiles(new StaticFileOptions
// {
//     FileProvider = new PhysicalFileProvider(@"D:\Desktop\img"), //localhost的時候放在這邊
//     // FileProvider = new PhysicalFileProvider("/var/www/html/img"), //GEC的時候放在這邊
//     RequestPath = "/img"
// });

app.MapPost("/api/test", async (HttpContext context, MySqlConnection connection) =>
{
    Console.WriteLine("Test1111");
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
            var connString = builder.Configuration.GetConnectionString("DefaultConnection");

            // await context.Response.WriteAsJsonAsync(new { message = $"connString: {connString}" });
            await context.Response.WriteAsJsonAsync(new { message = $"Test successful: {user}" });
            return;
        }
        await context.Response.WriteAsJsonAsync(new APIResponse(false, $"找不到任何資料"));
    }
    catch (Exception exception)
    {
        Console.WriteLine("Exception");
        await context.Response.WriteAsJsonAsync(new APIResponse(false, $"錯誤{exception.Message}"));
    }
});

// app.Run("http://*:5000"); // 一直被佔用改用3500
app.Run("https://*:3500");