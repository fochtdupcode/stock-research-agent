namespace Agent.Domain.Models;

public class Portfolio
{
    public double Cash { get; set; }
    public double Equity { get; set; }
    public double MaxPctPerTrade { get; set; } = 0.05;
}
