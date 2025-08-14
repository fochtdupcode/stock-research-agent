namespace Agent.Domain.Models;

public class DecisionCard
{
    public string RecId { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty; // Ignore|Watch|PaperTrade|LiveSmall|Scale
    public double Confidence { get; set; }
    public List<DecisionMetrics> Metrics { get; set; } = new List<DecisionMetrics>();
}