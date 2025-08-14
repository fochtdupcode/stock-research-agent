using Agent.API.Providers;
using Agent.Domain.Models;
using MongoDB.Driver;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Bind configuration sections
var mongoSettings = builder.Configuration.GetSection("Mongo").Get<MongoSettings>()!;
var marketDataSettings = builder.Configuration.GetSection("MarketData").Get<MarketDataSettings>()!;

// Register MongoDB services
builder.Services.Configure<MongoSettings>(builder.Configuration.GetSection("Mongo"));
builder.Services.AddSingleton<IMongoClient>(sp => new MongoClient(mongoSettings.ConnectionString));
builder.Services.AddSingleton<IMongoDatabase>(sp => 
    sp.GetRequiredService<IMongoClient>().GetDatabase(mongoSettings.Database));

// Register MongoDB collections
builder.Services.AddSingleton<IMongoCollection<Recommendation>>(sp => 
    sp.GetRequiredService<IMongoDatabase>().GetCollection<Recommendation>("recommendations"));
builder.Services.AddSingleton<IMongoCollection<Source>>(sp => 
    sp.GetRequiredService<IMongoDatabase>().GetCollection<Source>("sources"));
builder.Services.AddSingleton<IMongoCollection<Outcome>>(sp => 
    sp.GetRequiredService<IMongoDatabase>().GetCollection<Outcome>("outcomes"));
builder.Services.AddSingleton<IMongoCollection<Snapshot>>(sp => 
    sp.GetRequiredService<IMongoDatabase>().GetCollection<Snapshot>("snapshots"));

// Add HttpClient factory
builder.Services.AddHttpClient();

// Register market data provider based on configuration
if (marketDataSettings.Provider.Equals("Finnhub", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddSingleton(new FinnhubOptions 
    { 
        ApiKey = marketDataSettings.ApiKey,
        BaseUrl = marketDataSettings.FinnhubBaseUrl
    });
    builder.Services.AddSingleton<IMarketDataProvider, FinnhubProvider>();
}
else
{
    builder.Services.AddSingleton(new TwelveDataOptions 
    { 
        ApiKey = marketDataSettings.ApiKey,
        BaseUrl = marketDataSettings.TwelveDataBaseUrl
    });
    builder.Services.AddSingleton<IMarketDataProvider, TwelveDataProvider>();
}

var app = builder.Build();

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

app.MapGet("/weatherforecast", () =>
{
    var forecast =  Enumerable.Range(1, 5).Select(index =>
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

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
