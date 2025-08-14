using System.Text.Json;
using System.Text.Json.Serialization;

namespace Agent.API.Providers;

public class FinnhubOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://finnhub.io/api/v1";
}

public class FinnhubProvider : IMarketDataProvider
{
    private readonly HttpClient _httpClient;
    private readonly FinnhubOptions _options;
    private readonly JsonSerializerOptions _jsonOptions;

    public FinnhubProvider(HttpClient httpClient, FinnhubOptions options)
    {
        _httpClient = httpClient;
        _options = options;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            NumberHandling = JsonNumberHandling.AllowReadingFromString
        };
    }

    public async Task<Quote?> GetQuoteAsync(string ticker)
    {
        try
        {
            var url = $"{_options.BaseUrl}/quote?symbol={ticker}&token={_options.ApiKey}";
            var response = await _httpClient.GetAsync(url);
            
            if (!response.IsSuccessStatusCode)
                return null;

            var content = await response.Content.ReadAsStringAsync();
            var jsonDocument = JsonDocument.Parse(content);
            
            if (!jsonDocument.RootElement.TryGetProperty("c", out var priceElement))
                return null;

            double price;
            if (priceElement.ValueKind == JsonValueKind.Number)
            {
                price = priceElement.GetDouble();
            }
            else if (priceElement.ValueKind == JsonValueKind.String && 
                     double.TryParse(priceElement.GetString(), out var parsedPrice))
            {
                price = parsedPrice;
            }
            else
            {
                return null;
            }

            return new Quote(ticker, price);
        }
        catch
        {
            return null;
        }
    }

    public async Task<IEnumerable<EarningsEvent>> GetEarningsEventsAsync(string? ticker = null, DateTime? fromDate = null, DateTime? toDate = null)
    {
        if (string.IsNullOrEmpty(ticker))
            return Enumerable.Empty<EarningsEvent>();

        try
        {
            var from = fromDate?.ToString("yyyy-MM-dd") ?? DateTime.Today.ToString("yyyy-MM-dd");
            var to = toDate?.ToString("yyyy-MM-dd") ?? DateTime.Today.AddYears(1).ToString("yyyy-MM-dd");
            
            var url = $"{_options.BaseUrl}/calendar/earnings?symbol={ticker}&from={from}&to={to}&token={_options.ApiKey}";
            var response = await _httpClient.GetAsync(url);
            
            if (!response.IsSuccessStatusCode)
                return Enumerable.Empty<EarningsEvent>();

            var content = await response.Content.ReadAsStringAsync();
            var jsonDocument = JsonDocument.Parse(content);
            
            if (!jsonDocument.RootElement.TryGetProperty("earningsCalendar", out var earningsElement) || 
                earningsElement.ValueKind != JsonValueKind.Array)
                return Enumerable.Empty<EarningsEvent>();

            var events = new List<EarningsEvent>();
            
            foreach (var item in earningsElement.EnumerateArray())
            {
                if (item.TryGetProperty("date", out var dateElement) &&
                    DateTime.TryParse(dateElement.GetString(), out var date))
                {
                    var time = item.TryGetProperty("hour", out var timeElement) ? timeElement.GetString() : null;
                    
                    if (!fromDate.HasValue || date >= fromDate.Value)
                    {
                        if (!toDate.HasValue || date <= toDate.Value)
                        {
                            events.Add(new EarningsEvent(date, ticker, time));
                        }
                    }
                }
            }

            return events.OrderBy(e => e.Date);
        }
        catch
        {
            return Enumerable.Empty<EarningsEvent>();
        }
    }

    public async Task<double?> GetSMAAsync(string ticker, int period)
    {
        var candles = await GetCandleDataAsync(ticker, period + 10); // Get extra data to ensure we have enough
        if (candles == null || candles.Count < period)
            return null;

        // Calculate SMA from the last 'period' closes
        var recentCloses = candles.TakeLast(period).ToArray();
        return recentCloses.Average();
    }

    public async Task<double?> GetRSIAsync(string ticker, int period)
    {
        var candles = await GetCandleDataAsync(ticker, period * 3); // Get more data for RSI calculation
        if (candles == null || candles.Count < period + 1)
            return null;

        return CalculateWildersRSI(candles.ToArray(), period);
    }

    public async Task<DateTime?> GetNextEarningsAsync(string ticker)
    {
        try
        {
            var from = DateTime.Today.ToString("yyyy-MM-dd");
            var to = DateTime.Today.AddYears(1).ToString("yyyy-MM-dd");
            
            var url = $"{_options.BaseUrl}/calendar/earnings?symbol={ticker}&from={from}&to={to}&token={_options.ApiKey}";
            var response = await _httpClient.GetAsync(url);
            
            if (!response.IsSuccessStatusCode)
                return null;

            var content = await response.Content.ReadAsStringAsync();
            var jsonDocument = JsonDocument.Parse(content);
            
            if (!jsonDocument.RootElement.TryGetProperty("earningsCalendar", out var earningsElement) || 
                earningsElement.ValueKind != JsonValueKind.Array)
                return null;

            var today = DateTime.Today;
            DateTime? earliestFutureDate = null;

            foreach (var item in earningsElement.EnumerateArray())
            {
                if (item.TryGetProperty("date", out var dateElement) &&
                    DateTime.TryParse(dateElement.GetString(), out var date))
                {
                    if (date >= today)
                    {
                        if (!earliestFutureDate.HasValue || date < earliestFutureDate.Value)
                        {
                            earliestFutureDate = date;
                        }
                    }
                }
            }

            return earliestFutureDate;
        }
        catch
        {
            return null;
        }
    }

    private async Task<List<double>?> GetCandleDataAsync(string ticker, int daysBack)
    {
        try
        {
            var to = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var from = DateTimeOffset.UtcNow.AddDays(-daysBack).ToUnixTimeSeconds();
            
            var url = $"{_options.BaseUrl}/stock/candle?symbol={ticker}&resolution=D&from={from}&to={to}&token={_options.ApiKey}";
            var response = await _httpClient.GetAsync(url);
            
            if (!response.IsSuccessStatusCode)
                return null;

            var content = await response.Content.ReadAsStringAsync();
            var jsonDocument = JsonDocument.Parse(content);
            
            if (!jsonDocument.RootElement.TryGetProperty("c", out var closesElement) || 
                closesElement.ValueKind != JsonValueKind.Array)
                return null;

            var closes = new List<double>();
            foreach (var closeElement in closesElement.EnumerateArray())
            {
                double close;
                if (closeElement.ValueKind == JsonValueKind.Number)
                {
                    close = closeElement.GetDouble();
                    closes.Add(close);
                }
                else if (closeElement.ValueKind == JsonValueKind.String && 
                         double.TryParse(closeElement.GetString(), out var parsedClose))
                {
                    close = parsedClose;
                    closes.Add(close);
                }
            }

            return closes.Count > 0 ? closes : null;
        }
        catch
        {
            return null;
        }
    }

    private static double? CalculateWildersRSI(double[] closes, int period)
    {
        if (closes.Length < period + 1)
            return null;

        var gains = new List<double>();
        var losses = new List<double>();

        // Calculate price changes
        for (int i = 1; i < closes.Length; i++)
        {
            var change = closes[i] - closes[i - 1];
            gains.Add(change > 0 ? change : 0);
            losses.Add(change < 0 ? Math.Abs(change) : 0);
        }

        if (gains.Count < period)
            return null;

        // Calculate initial average gain and loss
        var avgGain = gains.Take(period).Average();
        var avgLoss = losses.Take(period).Average();

        // Apply Wilder's smoothing for the rest
        for (int i = period; i < gains.Count; i++)
        {
            avgGain = (avgGain * (period - 1) + gains[i]) / period;
            avgLoss = (avgLoss * (period - 1) + losses[i]) / period;
        }

        if (avgLoss == 0)
            return 100;

        var rs = avgGain / avgLoss;
        var rsi = 100 - (100 / (1 + rs));

        return rsi;
    }
}