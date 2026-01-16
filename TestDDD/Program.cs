using Microsoft.EntityFrameworkCore;
using Serilog;
using TestDDD.Data;
using TestDDD.ExternalApis;
using TestDDD.Services;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog for logging
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File("logs/kyc-service-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// Add DbContext with SQLite
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Data Source=kyc_cache.db";
builder.Services.AddDbContext<KycDbContext>(options =>
    options.UseSqlite(connectionString));

// Add HttpClient for external API
builder.Services.AddHttpClient<ICustomerDataApiClient, CustomerDataApiClient>();

// Add caching
builder.Services.AddMemoryCache();

// Add services
builder.Services.AddScoped<IKycAggregationService, KycAggregationService>();

var app = builder.Build();

// Apply database migrations
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<KycDbContext>();
    dbContext.Database.Migrate();
}

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

try
{
    app.Run();
}
finally
{
    Log.CloseAndFlush();
}
