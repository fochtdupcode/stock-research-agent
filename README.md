# Stock Research Agent

A .NET 8 API for stock analysis and trading recommendations using market data and technical indicators.

## Quick Start

### 1. Start MongoDB with Docker

```bash
docker compose -f infra/docker-compose.yml up -d
```

This will start a MongoDB instance on port 27017 with persistent data storage.

### 2. Set MarketData API Key

Update the `ApiKey` in `Agent.API/appsettings.json`:

```json
{
  "MarketData": {
    "Provider": "Finnhub",
    "ApiKey": "YOUR_API_KEY_HERE",
    "FinnhubBaseUrl": "https://finnhub.io/api/v1",
    "TwelveDataBaseUrl": "https://api.twelvedata.com"
  }
}
```

**Get a free API key:**
- **Finnhub**: https://finnhub.io/register
- **TwelveData**: https://twelvedata.com/pricing (alternative provider)

### 3. Build and Run the Application

```bash
# Restore dependencies
dotnet restore

# Build the project
dotnet build

# Run the API
dotnet run --project Agent.API
```

The API will be available at `https://localhost:5001` (or check console output for the exact URL).

### 4. Test the API

Visit `https://localhost:5001/swagger` to explore the API documentation.

## API Endpoints

### POST /api/score-and-suggest

Analyzes stocks and provides trading recommendations based on technical indicators and market data.

**Request Body Examples:**

#### Single Stock Analysis
```json
{
  "ticker": "AAPL",
  "portfolio": {
    "cash": 10000.0,
    "equity": 5000.0,
    "maxPctPerTrade": 0.05
  },
  "sourceReliability": 0.85
}
```

#### Multiple Stocks Analysis
```json
{
  "tickers": ["AAPL", "MSFT", "GOOGL"],
  "portfolio": {
    "cash": 25000.0,
    "equity": 15000.0,
    "maxPctPerTrade": 0.03
  },
  "sourceReliability": 0.9
}
```

#### Recommendation with Source Information
```json
{
  "recommendation": {
    "ticker": "TSLA",
    "action": "Buy",
    "confidence": 0.8
  },
  "portfolio": {
    "cash": 5000.0,
    "equity": 0.0,
    "maxPctPerTrade": 0.1
  },
  "source": {
    "name": "Goldman Sachs",
    "type": "Investment Bank",
    "reliability": 0.92
  }
}
```

**Response Format:**
```json
{
  "recId": "recommendation-id",
  "action": "LiveSmall",
  "confidence": 0.78,
  "metrics": [
    {
      "srcReliability": 0.85,
      "rsi14": 65.4,
      "trend20v50": "Bullish",
      "peVsSectorPctile": 0.0
    }
  ]
}
```

**Action Types:**
- `Scale` - High confidence, scale position (score ≥ 0.8)
- `LiveSmall` - Good confidence, small live trade (score ≥ 0.65)
- `PaperTrade` - Moderate confidence, paper trade (score ≥ 0.5)
- `Watch` - Low confidence, just watch (score ≥ 0.3)
- `Ignore` - Very low confidence, ignore (score < 0.3)

## Project Structure

- `Agent.API/` - Web API project with endpoints and services
- `Agent.Domain/` - Domain models and data contracts
- `infra/` - Infrastructure configuration (Docker Compose)

## Features

- **Market Data Integration**: Supports Finnhub and TwelveData providers
- **Technical Analysis**: RSI, SMA (20/50), trend analysis
- **Portfolio Management**: Cash and equity tracking with risk limits
- **MongoDB Storage**: Persistent storage for recommendations, outcomes, and snapshots
- **Scoring Algorithm**: Multi-factor scoring based on source reliability, technical indicators

## Configuration

Key configuration sections in `appsettings.json`:

- `Mongo`: MongoDB connection settings
- `MarketData`: Market data provider configuration
- `Logging`: Application logging levels

## Development

### Prerequisites
- .NET 8 SDK
- Docker & Docker Compose
- MongoDB (via Docker or local installation)
- Market data API key (Finnhub or TwelveData)

### Running Tests
```bash
dotnet test
```

### Database Collections
- `recommendations` - Trading recommendations
- `sources` - Data source information and reliability scores  
- `outcomes` - Trade outcomes and performance tracking
- `snapshots` - Market data snapshots with technical indicators