namespace Agent.Domain.Models;

public class DecisionMetrics
{
    public double SrcReliability { get; set; }
    public double Rsi14 { get; set; }
    public string Trend20v50 { get; set; } = string.Empty;
    public double PeVsSectorPctile { get; set; }
}