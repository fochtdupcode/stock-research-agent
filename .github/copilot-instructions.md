# Stock Research Agent - GitHub Copilot Instructions

Stock Research Agent is a .NET 8 Web API for stock analysis and trading recommendations using market data and technical indicators with MongoDB for persistent storage.

**ALWAYS reference these instructions first and fallback to search or bash commands only when you encounter unexpected information that does not match the info here.**

## Working Effectively

### Prerequisites & Setup
- Install .NET 8 SDK (version 8.0.118 or later)
- Install Docker & Docker Compose
- Clone this repository

### Bootstrap, Build, and Test the Repository
Execute these commands in sequence:

```bash
# Navigate to repository root
cd /path/to/stock-research-agent

# Start MongoDB database (takes ~0.3 seconds after first run)
docker compose -f infra/docker-compose.yml up -d

# Restore .NET dependencies (takes ~1.4 seconds when up-to-date)
dotnet restore

# Build the solution (takes ~1.8 seconds)
dotnet build

# Run tests (completes in ~1.5 seconds - currently no test projects exist)
dotnet test
```

**TIMING NOTE**: All build commands complete very quickly (under 2 seconds). Use standard timeouts.

### Run the Application

**ALWAYS run the bootstrap steps first before starting the API.**

```bash
# Start MongoDB (if not already running)
docker compose -f infra/docker-compose.yml up -d

# Configure API key (see Configuration section below)
# Edit Agent.API/appsettings.json and replace "REPLACE_ME" with your API key

# Run the Web API
dotnet run --project Agent.API
```

The API will be available at:
- HTTP: `http://localhost:5007`
- HTTPS: `https://localhost:7296`
- Swagger UI: `http://localhost:5007/swagger` or `https://localhost:7296/swagger`

## Validation

### Manual Validation Requirements
ALWAYS perform these validation steps after making changes:

1. **Start MongoDB and verify it's running**:
   ```bash
   docker compose -f infra/docker-compose.yml up -d
   docker ps | grep mongo
   ```

2. **Build and run the API**:
   ```bash
   dotnet build
   dotnet run --project Agent.API
   ```

3. **Test API endpoints**:
   ```bash
   # Test the weatherforecast endpoint (currently the only working endpoint)
   curl http://localhost:5007/weatherforecast
   
   # Access Swagger UI in browser
   open http://localhost:5007/swagger
   ```

4. **Code formatting validation**:
   ```bash
   # Check for formatting issues (MUST pass before committing, takes ~8 seconds)
   dotnet format --verify-no-changes
   
   # Fix formatting issues if found (takes ~8 seconds)
   dotnet format
   ```

**CRITICAL**: Always run `dotnet format` before committing. The project has `TreatWarningsAsErrors` enabled and strict formatting requirements.

### Complete End-to-End Scenario
Run through this scenario after making changes:
1. Start MongoDB container
2. Build the solution 
3. Start the API server
4. Access Swagger UI at http://localhost:5007/swagger
5. Test the /weatherforecast endpoint via Swagger or curl
6. Verify API returns JSON response with weather data
7. Run formatting checks and fix any issues

## Configuration

### Required Configuration Changes
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

### MongoDB Configuration
MongoDB runs via Docker on port 27017 with persistent data storage:
- Connection String: `mongodb://localhost:27017`
- Database: `stockrec`
- Collections: `recommendations`, `sources`, `outcomes`, `snapshots`

## Project Structure

```
├── Agent.API/              # Web API project with endpoints and services
│   ├── Agent.API.csproj    # Main API project file
│   ├── Program.cs          # Application entry point and service configuration
│   ├── appsettings.json    # Configuration (API keys, MongoDB, logging)
│   ├── Providers/          # Market data providers (Finnhub, TwelveData)
│   └── Services/           # Business logic and scoring services
├── Agent.Domain/           # Domain models and data contracts
│   ├── Agent.Domain.csproj # Domain project file
│   └── Models/             # Data models and configuration classes
├── infra/                  # Infrastructure configuration
│   └── docker-compose.yml  # MongoDB container setup
├── StockRecAgent.sln       # Visual Studio solution file
├── README.md               # Project documentation
└── .editorconfig           # Code formatting rules
```

## Development Guidelines

### Code Quality Requirements
- **TreatWarningsAsErrors**: Enabled in both projects - all warnings must be fixed
- **Formatting**: Follow .editorconfig rules strictly
- **Null Safety**: Nullable reference types enabled
- **ImplicitUsings**: Enabled for cleaner code

### Before Committing Changes
ALWAYS run these commands:
```bash
# Build and check for errors/warnings
dotnet build

# Fix code formatting
dotnet format

# Verify formatting is correct
dotnet format --verify-no-changes
```

### Common Development Tasks

#### Adding New API Endpoints
1. Add endpoint mapping in `Agent.API/Program.cs` after line 85 (before `app.Run()`)
2. Follow the existing `/weatherforecast` pattern
3. Add request/response models as records at the bottom of the file
4. Test via Swagger UI

#### Working with MongoDB
- MongoDB collections are registered as singletons in DI container
- Access via `IMongoCollection<T>` dependencies
- Models are in `Agent.Domain/Models/`

#### Market Data Integration
- Providers: `FinnhubProvider` and `TwelveDataProvider`
- Interface: `IMarketDataProvider`
- Configuration determines which provider is used

### Important Notes
- **Missing Functionality**: The `/api/score-and-suggest` endpoint described in README.md does not exist in the current codebase
- **No Tests**: Currently no test projects exist - `dotnet test` runs but finds no tests
- **Early Development**: The application appears to be in early development stages

## Common File Locations

### Frequently Modified Files
- `Agent.API/Program.cs` - API endpoints and service configuration
- `Agent.API/appsettings.json` - Configuration including API keys
- `Agent.Domain/Models/` - Data models and DTOs
- `README.md` - Project documentation

### Configuration Files  
- `.editorconfig` - Code formatting rules (177 lines of C# style rules)
- `Agent.API/appsettings.json` - Application configuration
- `infra/docker-compose.yml` - MongoDB container setup

### Build Files
- `StockRecAgent.sln` - Solution file (2 projects)
- `Agent.API/Agent.API.csproj` - API project dependencies
- `Agent.Domain/Agent.Domain.csproj` - Domain project dependencies
