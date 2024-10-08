using CSharpAPI.Services.Implementations;
using Microsoft.Extensions.FileProviders;
using MySqlConnector;
using System.Security.Authentication;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// 添加 Controller 支持
builder.Services.AddControllers();
builder.Services.AddScoped<ItemService>();
builder.Services.AddScoped<HttpClient>();
// 暫時不要使用 
// Access to XMLHttpRequest at 'https://api.louise.tw/api/getallitem' from origin 'https://www.louise.tw' has been blocked by CORS policy: No 'Access-Control-Allow-Origin' header is present on the requested resource.
// builder.Services.AddScoped<IItemService, ItemService>();

// 使用 Scoped 生命週期而不是 Transient 為了在同一個method 執行兩次MySqlCommand
// 雖然說不加還是可以 但是可能有 生命週期管理 測試困難 一致性 性能問題
// 資料庫連線資料在appsettings.json設定好了
//將Mysql注入DIContainer
builder.Services.AddScoped(sp =>
    new MySqlConnection(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

if (builder.Environment.IsProduction())
{
    // HTTPS 配置 在localhost 使用http 和 www.louise.tw都使用https
    builder.WebHost.ConfigureKestrel(serverOptions =>
    {
        // 取得appsettings.json
        var config = builder.Configuration;
        // 取得appsettings.json Kestrel的部分
        serverOptions.Configure(config.GetSection("Kestrel"));
        serverOptions.ConfigureHttpsDefaults(listenOptions =>
        {
            listenOptions.SslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13;
        });
    });
}


var app = builder.Build();

//使用Cors跨域設定 必須在其他 middleware 之前
app.UseCors("CorsPolicy");

// appsettings.json 最好只包含共用的數值 不共用 或是 根據Environment不同需要另外放置別的appsettings.環境名稱.json
if (app.Environment.IsProduction())
{
    // 會強制在http 加上 s
    app.UseHttpsRedirection();
}

app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();

string filePath = app.Environment.IsDevelopment()
? "/Users/wangshihchieh/Desktop/SourceTree/CSharpAPI"
// ? "/TestImg"
: "/var/www/html/img";

// 設定圖片相對位置 註解掉會讓前端無法透過/img/...取得圖片 
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(filePath), //GCE的時候放在這邊
    RequestPath = "/img"
});

// Port 根據 appsettings.json 的環境不同
app.Run();