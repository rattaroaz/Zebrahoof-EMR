# Zebrahoof EMR

A modern Electronic Medical Records (EMR) system built with ASP.NET Core and Blazor.

## Overview

Zebrahoof EMR is a web-based healthcare management application designed for veterinary practices. It provides comprehensive patient record management, appointment scheduling, and clinical workflow support.

## Technology Stack

- **Framework**: .NET 8.0
- **UI**: Blazor with MudBlazor components
- **Database**: PostgreSQL (production) / SQLite (development)
- **ORM**: Entity Framework Core 8.0
- **Authentication**: ASP.NET Core Identity with 2FA support
- **Logging**: Serilog with structured logging
- **Testing**: xUnit with Playwright for UI tests

## Getting Started

### Prerequisites

- .NET 8.0 SDK or later
- PostgreSQL 15+ (for production) or SQLite (for development)
- Node.js 20+ (for UI dependencies)

### Installation

1. Clone the repository:
```bash
git clone https://github.com/rattaroaz/Zebrahoof-EMR.git
cd Zebrahoof-EMR
```

2. Restore dependencies:
```bash
dotnet restore "Zebrahoof EMR.csproj"
npm install
```

3. Run the application:
```bash
dotnet run
```

The application will be available at `http://localhost:5000`.

### Configuration

Configuration settings are managed through `appsettings.json` and environment-specific files. For production and staging deployments, ensure proper connection strings and security settings are configured via environment variables or secure configuration providers.

## Testing

The project includes comprehensive test suites:

- **Unit Tests**: Core business logic validation
- **Integration Tests**: Database and API integration testing
- **UI Smoke Tests**: Automated browser testing with Playwright
- **API Tests**: RESTful endpoint testing
- **Performance Tests**: Load and stress testing with NBomber
- **Mutation Tests**: Code quality analysis with Stryker.NET

Run all tests:
```bash
dotnet test
```

## CI/CD

The project uses GitHub Actions for continuous integration and deployment. The pipeline runs on every push to `main` and `develop` branches, executing:

- Build validation
- Unit and integration tests
- Code coverage analysis (75% line coverage, 60% branch coverage required)
- Security scanning
- Automated deployment to staging (develop) and production (main)

## License

This project is provided as-is for educational and demonstration purposes.

## Contributing

Contributions are welcome. Please ensure all tests pass and maintain the required code coverage thresholds before submitting pull requests.
