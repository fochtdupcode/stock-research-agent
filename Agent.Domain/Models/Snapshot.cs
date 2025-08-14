namespace Agent.Domain.Models;

public class Snapshot
{
    public string Id { get; set; } = string.Empty;
    public string Ticker { get; set; } = string.Empty;
    public double Price { get; set; }
    public double Volume { get; set; }
    public DateTime Timestamp { get; set; }
    public Dictionary<string, object> MarketData { get; set; } = new();
}
