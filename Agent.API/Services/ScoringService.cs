using Agent.Domain.Models;

namespace Agent.API.Services;

public class ScoringService
{
    public DecisionCard Decide(Recommendation rec, Snapshot snap, double srcReliability, Portfolio? p)
    {
        // Calculate trend score from 20 vs 50 SMA
        var trendScore = CalculateTrendScore(snap);

        // Calculate RSI score with preference for 30-70 band
        var rsiScore = CalculateRsiScore(snap);

        // Calculate base score: 0.5*srcReliability + 0.3*trend + 0.2*rsiScore
        var baseScore = 0.5 * srcReliability + 0.3 * trendScore + 0.2 * rsiScore;

        // Map to actions based on score
        var action = MapScoreToAction(baseScore, p);

        // Create decision metrics
        var metrics = new DecisionMetrics
        {
            SrcReliability = srcReliability,
            Rsi14 = GetRsiFromSnapshot(snap),
            Trend20v50 = GetTrendFromSnapshot(snap),
            PeVsSectorPctile = 0 // Not specified in requirements, setting to 0
        };

        return new DecisionCard
        {
            RecId = rec.Id,
            Action = action,
            Confidence = Math.Min(1.0, Math.Max(0.0, baseScore)), // Clamp between 0 and 1
            Metrics = new List<DecisionMetrics> { metrics }
        };
    }

    private double CalculateTrendScore(Snapshot snap)
    {
        var sma20 = GetSmaFromSnapshot(snap, "SMA20");
        var sma50 = GetSmaFromSnapshot(snap, "SMA50");

        if (sma20 == null || sma50 == null || sma20 == 0 || sma50 == 0)
            return 0.5; // Neutral score if data unavailable

        // Positive trend if 20 SMA > 50 SMA, negative if opposite
        var ratio = sma20.Value / sma50.Value;

        if (ratio > 1.02) // 20 SMA significantly above 50 SMA (strong uptrend)
            return 1.0;
        else if (ratio > 1.0) // 20 SMA above 50 SMA (mild uptrend)
            return 0.75;
        else if (ratio > 0.98) // Close to neutral
            return 0.5;
        else if (ratio > 0.95) // 20 SMA below 50 SMA (mild downtrend)
            return 0.25;
        else // 20 SMA significantly below 50 SMA (strong downtrend)
            return 0.0;
    }

    private double CalculateRsiScore(Snapshot snap)
    {
        var rsi = GetRsiFromSnapshot(snap);

        if (rsi == 0)
            return 0.5; // Neutral score if RSI unavailable

        // Prefer RSI in 30-70 range
        if (rsi >= 30 && rsi <= 70)
            return 1.0; // Optimal range
        else if (rsi >= 25 && rsi < 30)
            return 0.8; // Near oversold but acceptable
        else if (rsi > 70 && rsi <= 75)
            return 0.8; // Near overbought but acceptable
        else if (rsi < 25)
            return 0.3; // Oversold - risky
        else if (rsi > 75)
            return 0.3; // Overbought - risky
        else
            return 0.5; // Default
    }

    private string MapScoreToAction(double baseScore, Portfolio? portfolio)
    {
        // Consider portfolio constraints if available
        var hasCapital = portfolio?.Cash > 0;
        var portfolioFactor = hasCapital ? 1.0 : 0.5; // Reduce action intensity if no capital

        var adjustedScore = baseScore * portfolioFactor;

        return adjustedScore switch
        {
            >= 0.8 => "Scale",        // High confidence, scale position
            >= 0.65 => "LiveSmall",   // Good confidence, small live trade
            >= 0.5 => "PaperTrade",   // Moderate confidence, paper trade
            >= 0.3 => "Watch",        // Low confidence, just watch
            _ => "Ignore"             // Very low confidence, ignore
        };
    }

    private double? GetSmaFromSnapshot(Snapshot snap, string key)
    {
        if (snap.MarketData.TryGetValue(key, out var value))
        {
            return value switch
            {
                double d => d,
                string s when double.TryParse(s, out var parsed) => parsed,
                _ => null
            };
        }
        return null;
    }

    private double GetRsiFromSnapshot(Snapshot snap)
    {
        var rsi = GetSmaFromSnapshot(snap, "RSI14") ??
                  GetSmaFromSnapshot(snap, "RSI") ??
                  0.0;
        return rsi;
    }

    private string GetTrendFromSnapshot(Snapshot snap)
    {
        var sma20 = GetSmaFromSnapshot(snap, "SMA20");
        var sma50 = GetSmaFromSnapshot(snap, "SMA50");

        if (sma20 == null || sma50 == null)
            return "Unknown";

        if (sma20 > sma50)
            return "Bullish";
        else if (sma20 < sma50)
            return "Bearish";
        else
            return "Neutral";
    }
}
