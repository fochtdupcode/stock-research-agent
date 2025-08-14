namespace Agent.Domain.Models;

public class Outcome
{
    public string Id { get; set; } = string.Empty;
    public string RecommendationId { get; set; } = string.Empty;
    public string Ticker { get; set; } = string.Empty;
    public double ActualReturn { get; set; }
    public double ExpectedReturn { get; set; }
    public DateTime OutcomeDate { get; set; }
}
