namespace Agent.Domain.Models;

public class MarketDataSettings
{
    public string Provider { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string FinnhubBaseUrl { get; set; } = "https://finnhub.io/api/v1";
    public string TwelveDataBaseUrl { get; set; } = "https://api.twelvedata.com";
}