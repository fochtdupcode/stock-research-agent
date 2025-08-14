namespace Agent.API.Providers;

public record Quote(string Ticker, double Price);

public record EarningsEvent(DateTime Date, string? Ticker, string? Time);

public interface IMarketDataProvider
{
    Task<Quote?> GetQuoteAsync(string ticker);
    Task<IEnumerable<EarningsEvent>> GetEarningsEventsAsync(string? ticker = null, DateTime? fromDate = null, DateTime? toDate = null);
    Task<double?> GetSMAAsync(string ticker, int period);
    Task<double?> GetRSIAsync(string ticker, int period);
    Task<DateTime?> GetNextEarningsAsync(string ticker);
}
