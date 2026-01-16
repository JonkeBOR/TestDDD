# KYC Aggregation Service - Reflection Document

## Project Overview

This is a .NET 10 backend service that implements a KYC (Know Your Customer) Aggregation API. The service integrates with an external Customer Data API to fetch and aggregate customer information, providing a single endpoint for retrieving comprehensive KYC data.

## Architecture & Design Decisions

### 1. **Dual-Layer Caching Strategy**

**Decision:** Implemented both in-memory caching and persistent database caching.

**Reasoning:**
- **In-Memory Cache**: Fast access for recently accessed data with configurable expiration (1 hour)
- **Persistent Database Cache**: Survives application restarts, ensuring data is available even after shutdown
- **Layered Approach**: On startup, warm the in-memory cache from the database; on cache miss, check database before calling external API

**Benefits:**
- Reduces external API calls significantly
- Provides resilience if external API becomes unavailable
- Balances performance with memory efficiency
- Allows customers to be served data even if external service temporarily fails

### 2. **Entity Framework Core with SQLite**

**Decision:** Used EF Core with SQLite for persistent storage.

**Reasoning:**
- SQLite is lightweight and requires no external infrastructure
- EF Core provides type-safe queries and automatic migrations
- Easy to demonstrate persistence without complex setup
- Can be swapped for SQL Server, PostgreSQL, etc., in production without code changes

**Trade-offs:**
- SQLite has limitations with concurrent writes (acceptable for this use case)
- Could use Redis for higher performance if needed
- File-based database is simpler than client-server databases for this demo

### 3. **External API Client Abstraction**

**Decision:** Created `ICustomerDataApiClient` interface with dedicated implementation.

**Reasoning:**
- Enables dependency injection and testability
- Encapsulates HTTP communication concerns
- Centralized error handling and logging
- Easy to mock in unit tests
- Handles HttpClient best practices (reuse via DI)

### 4. **Aggregation Service Pattern**

**Decision:** Created a separate `IKycAggregationService` that orchestrates data fetching and caching.

**Reasoning:**
- Separates business logic from HTTP concerns
- Single Responsibility: service handles aggregation, caching decisions, and data transformation
- Controller remains thin and focused on HTTP concerns
- Easy to test in isolation
- Business logic can be reused if needed elsewhere (CLI, batch jobs, etc.)

## Error Handling Strategy

### User-Facing Errors
- **404 Not Found**: When customer data cannot be retrieved from the external API
- **400 Bad Request**: When invalid input is provided
- **500 Internal Server Error**: For unexpected system errors

All errors return consistent `ErrorResponse` DTOs to the client.

### System-Level Logging
- **Serilog Integration**: Centralized logging to console and rolling file logs
- **Log Levels**: 
  - INFO: Request tracking, cache hits/misses
  - WARNING: Expected failures (404s from external API)
  - ERROR: Unexpected exceptions with full stack traces
- **Structured Logging**: Uses named placeholders for queryable logs

### Benefits
- Clear separation of user-facing messages from internal diagnostics
- Logs provide audit trail of operations
- Can be easily exported to centralized logging service (ELK, Application Insights, etc.)

## Code Quality Measures

### Reusability
1. **Generic Exception Handling**: Centralized in controller with proper status code mapping
2. **Data Transformation Logic**: Extracted into private helper methods
3. **Caching Logic**: Encapsulated in service, can be reused across multiple endpoints
4. **API Client**: Reusable for multiple endpoints if needed

### Maintainability
1. **Clear Separation of Concerns**: Controllers → Services → Data Access → External APIs
2. **Dependency Injection**: Loose coupling enables easy testing and component swapping
3. **DTOs vs Domain Models**: Separate DTOs for external API responses to shield domain logic
4. **Nullable Reference Types**: Enabled for compile-time null safety

### Testability
- All dependencies injected via constructor
- Interfaces for all public services
- Mocking-friendly design

## Unit Testing

### Test Coverage Strategy

Focused tests on **KycAggregationService** which is the critical business logic:

1. **Happy Path Test** (`GetAggregatedKycDataAsync_WithValidSsn_ReturnsAggregatedData`)
   - Verifies successful aggregation of data from all sources
   - Ensures proper data transformation and mapping

2. **Caching Test** (`GetAggregatedKycDataAsync_ReturnsCachedDataFromMemory_OnSecondCall`)
   - Verifies in-memory cache is used on subsequent calls
   - Ensures external API is not called twice
   - Demonstrates cache effectiveness

3. **Error Handling Tests**
   - `ThrowsException_WhenApiReturnsNull`: Validates error when data is incomplete
   - `HandlesNullContactDetails`: Tests resilience to partial failures

### Why Focus on KycAggregationService?
- **Core Business Logic**: Contains the aggregation algorithm and caching strategy
- **Most Complex**: Has the highest cyclomatic complexity and decision points
- **Integration Point**: Bridges external API, database, and caching layers
- **Risk Assessment**: Failure here directly impacts the entire API
- **Reusability**: Tests serve as documentation for the service contract

### Test Approach
- **Moq Framework**: For mocking external dependencies
- **Arrange-Act-Assert Pattern**: Clear test structure
- **Focused Tests**: Each test verifies one specific behavior
- **Mock Verification**: Ensures dependencies are called correctly

## Implementation Highlights

### 1. Robust API Integration
```csharp
// Handles different HTTP status codes appropriately
if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
{
    return null;  // Propagate as expected failure
}
response.EnsureSuccessStatusCode();  // Throw for unexpected errors
```

### 2. Intelligent Data Aggregation
- Extracts preferred contact information from arrays
- Formats addresses combining multiple fields
- Handles case-insensitive KYC form keys (due to API inconsistency)
- Safely parses numeric fields with null coalescing

### 3. Database Persistence
- Unique index on SSN prevents duplicates
- Updates existing records instead of creating duplicates
- Timestamps track when data was cached

## Future Improvements

### Short-term
1. **Cache Invalidation Strategy**: Implement TTL or manual invalidation for updated customer data
2. **Batch Operations**: Support querying multiple customers in one request
3. **Metrics & Monitoring**: Add Application Insights integration for performance tracking
4. **Rate Limiting**: Protect external API from overuse

### Medium-term
1. **API Versioning**: Prepare for multiple API versions
2. **Background Jobs**: Use Hangfire or similar for periodic cache refresh
3. **Circuit Breaker Pattern**: Handle external API timeouts gracefully with Polly
4. **Database Abstraction**: Move data access to repository pattern
5. **Encryption**: Add encryption for sensitive cached data at rest

### Long-term
1. **Distributed Caching**: Replace SQLite with distributed cache (Redis) for multi-instance deployments
2. **Event Sourcing**: Track all data changes for audit compliance
3. **GraphQL**: Additional query interface for flexible data retrieval
4. **Machine Learning**: Anomaly detection for suspicious KYC patterns

## Testing the Service

### Test Data Available
```
19800115-1234
19900220-5678
19751230-9101
19850505-4321
19951212-3456
```

### Example Request
```
GET /kyc-data/19800115-1234
```

### Expected Response (200 OK)
```json
{
  "ssn": "19800115-1234",
  "first_name": "Lars",
  "last_name": "Larsson",
  "address": "Smågatan 1, 123 22 Malmö",
  "phone_number": "+46 70 123 45 67",
  "email": "lars.larsson@example.com",
  "tax_country": "SE",
  "income": 550000
}
```

## Running the Application

```bash
# Restore packages
dotnet restore

# Build the solution
dotnet build

# Run tests
dotnet test

# Run the application
dotnet run --project TestDDD/TestDDD.csproj
```

The API will be available at `https://localhost:5001` (HTTPS) or `http://localhost:5000` (HTTP).

## Conclusion

This implementation demonstrates a production-ready approach to building reliable backend services with proper error handling, caching strategies, and testability. The architecture is extensible and can be adapted to changing requirements while maintaining code quality and maintainability.
