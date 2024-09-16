using MySqlConnector;

var builder = WebApplication.CreateBuilder(args);

// 确保添加此行来加载 appsettings.json 文件
// builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", policy =>
    {
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
    });
});

// 資料庫連線在appsettings.json設定好了
// builder.Services.AddMySqlDataSource
// "comment": {
//     "DefaultConnection": "Server=34.82.250.51;Database=ShoppingWebsite;Uid=aaa;Pwd=louise87276;",
//     "DefaultConnection": "Server=192.168.38.128;Database=ShoppingWebsite;Uid=aaa;Pwd=aaa;"
//   },

// 使用 Scoped 生命週期而不是 Transient 為了在同一個method 執行兩次MySqlCommand
// 雖然說不加還是可以 但是可能會 生命週期管理 測試困難 一致性 性能問題
// 添加 Controller 支持
builder.Services.AddControllers();
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

app.UseHttpsRedirection();
app.MapControllers();

app.MapPost("/api/test", async (HttpContext context, MySqlConnection connection) =>
{
    // dotnet run 時會錯誤
    Console.WriteLine("Test");
    connection = new MySqlConnection("Server=34.82.250.51;Database=ShoppingWebsite;Uid=aaa;Pwd=louise87276;");
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

            await context.Response.WriteAsJsonAsync(new { message = $"connString: {connString}" });
            // await context.Response.WriteAsJsonAsync(new { message = $"Test successful: {user}" });
        }
        await context.Response.WriteAsJsonAsync(new APIResponse(false, $"找不到任何資料"));
    }
    catch (Exception exception)
    {
        Console.WriteLine("Exception");
        await context.Response.WriteAsJsonAsync(new APIResponse(false, $"錯誤{exception.Message}"));
    }
});

app.Run("http://*:5000");