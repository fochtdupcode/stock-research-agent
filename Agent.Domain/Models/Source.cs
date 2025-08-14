namespace Agent.Domain.Models;

public class Source
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public double Reliability { get; set; }
    public DateTime LastUpdated { get; set; }
}
