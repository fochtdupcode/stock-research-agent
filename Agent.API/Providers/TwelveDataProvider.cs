using System.Text.Json;
using System.Text.Json.Serialization;

namespace Agent.API.Providers;

public class TwelveDataOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://api.twelvedata.com";
}

public class TwelveDataProvider : IMarketDataProvider
{
    private readonly HttpClient _httpClient;
    private readonly TwelveDataOptions _options;
    private readonly JsonSerializerOptions _jsonOptions;

    public TwelveDataProvider(HttpClient httpClient, TwelveDataOptions options)
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
            var url = $"{_options.BaseUrl}/quote?symbol={ticker}&apikey={_options.ApiKey}";
            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
                return null;

            var content = await response.Content.ReadAsStringAsync();
            var jsonDocument = JsonDocument.Parse(content);

            if (!jsonDocument.RootElement.TryGetProperty("price", out var priceElement))
                return null;

            if (!double.TryParse(priceElement.GetString(), out var price))
                return null;

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
            var url = $"{_options.BaseUrl}/earnings?symbol={ticker}&apikey={_options.ApiKey}";
            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
                return Enumerable.Empty<EarningsEvent>();

            var content = await response.Content.ReadAsStringAsync();
            var jsonDocument = JsonDocument.Parse(content);

            if (!jsonDocument.RootElement.TryGetProperty("earnings", out var earningsElement) ||
                earningsElement.ValueKind != JsonValueKind.Array)
                return Enumerable.Empty<EarningsEvent>();

            var events = new List<EarningsEvent>();

            foreach (var item in earningsElement.EnumerateArray())
            {
                if (item.TryGetProperty("date", out var dateElement) &&
                    DateTime.TryParse(dateElement.GetString(), out var date))
                {
                    var time = item.TryGetProperty("time", out var timeElement) ? timeElement.GetString() : null;

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
        try
        {
            var url = $"{_options.BaseUrl}/sma?symbol={ticker}&interval=1day&time_period={period}&series_type=close&apikey={_options.ApiKey}";
            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
                return null;

            var content = await response.Content.ReadAsStringAsync();
            var jsonDocument = JsonDocument.Parse(content);

            if (!jsonDocument.RootElement.TryGetProperty("values", out var valuesElement) ||
                valuesElement.ValueKind != JsonValueKind.Array)
                return null;

            var firstValue = valuesElement.EnumerateArray().FirstOrDefault();
            if (firstValue.ValueKind == JsonValueKind.Undefined)
                return null;

            if (!firstValue.TryGetProperty("sma", out var smaElement))
                return null;

            if (double.TryParse(smaElement.GetString(), out var sma))
                return sma;

            return null;
        }
        catch
        {
            return null;
        }
    }

    public async Task<double?> GetRSIAsync(string ticker, int period)
    {
        try
        {
            var url = $"{_options.BaseUrl}/rsi?symbol={ticker}&interval=1day&time_period={period}&series_type=close&apikey={_options.ApiKey}";
            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
                return null;

            var content = await response.Content.ReadAsStringAsync();
            var jsonDocument = JsonDocument.Parse(content);

            if (!jsonDocument.RootElement.TryGetProperty("values", out var valuesElement) ||
                valuesElement.ValueKind != JsonValueKind.Array)
                return null;

            var firstValue = valuesElement.EnumerateArray().FirstOrDefault();
            if (firstValue.ValueKind == JsonValueKind.Undefined)
                return null;

            if (!firstValue.TryGetProperty("rsi", out var rsiElement))
                return null;

            if (double.TryParse(rsiElement.GetString(), out var rsi))
                return rsi;

            return null;
        }
        catch
        {
            return null;
        }
    }

    public async Task<DateTime?> GetNextEarningsAsync(string ticker)
    {
        try
        {
            var url = $"{_options.BaseUrl}/earnings?symbol={ticker}&apikey={_options.ApiKey}";
            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
                return null;

            var content = await response.Content.ReadAsStringAsync();
            var jsonDocument = JsonDocument.Parse(content);

            if (!jsonDocument.RootElement.TryGetProperty("earnings", out var earningsElement) ||
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
}
