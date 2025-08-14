namespace Agent.Domain.Models;

public class Recommendation
{
    public string Id { get; set; } = string.Empty;
    public string Ticker { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public double Confidence { get; set; }
    public DateTime CreatedAt { get; set; }
}