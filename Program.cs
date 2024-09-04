using MySqlConnector;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", policy =>
    {
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
    });
});

//將Mysql注入DIContainer
builder.Services.AddMySqlDataSource("Server=192.168.38.128;User ID=aaa;Password=aaa;Database=shop");


// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

//使用Cors跨域設定
app.UseCors("CorsPolicy");

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/api/v1/item", (MySqlConnection connection) =>
{
    //連接資料庫
    connection.Open();

    //準備sql
    using var command = new MySqlCommand("SELECT * FROM Item;", connection);
    //執行sql
    using var reader = command.ExecuteReader();

    List<dynamic> values = new List<dynamic>();
    //讀出每一筆資料
    while (reader.Read())
    {
        var value0 = reader.GetValue(0);
        var value1 = reader.GetValue(1);
        var value2 = reader.GetValue(2);
        var value3 = reader.GetValue(3);
        var value4 = reader.GetValue(4);

        values.Add(new
        {
            id = value0,
            name = value1,
            detail = value2,
            price = value3,
            stock = value4,
        });
    }

    Console.WriteLine($"values.Count: {values.Count}");
    foreach (dynamic value in values)
    {
        Console.WriteLine(value);
    }

    return new
    {
        Code = 200,
        Message = "success",
        Data = values
    };
});

app.MapGet("/weatherforecast", () =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast")
.WithOpenApi();

app.Run("http://*:5000");

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
