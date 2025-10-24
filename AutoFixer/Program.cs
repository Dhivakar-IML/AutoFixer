using AutoFixer.Data;
using AutoFixer.Data.Repositories;
using AutoFixer.Data.Repositories.Interfaces;
using AutoFixer.ML;
using AutoFixer.Services;
using AutoFixer.Services.Interfaces;
using AutoFixer.Configuration;
using AutoFixer.Health;
using MongoDB.Driver;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Configure Swagger with custom configuration
builder.Services.AddSwaggerConfiguration();

// Add SignalR for real-time communication
builder.Services.AddSignalR();

// Add health checks
builder.Services.AddHealthChecks()
    .AddCheck<MongoDbHealthCheck>("mongodb")
    .AddCheck<PatternDetectionHealthCheck>("pattern-detection")
    .AddCheck<AlertServiceHealthCheck>("alert-service")
    .AddCheck<NotificationHealthCheck>("notifications");

// Configure settings
builder.Services.Configure<NotificationSettings>(
    builder.Configuration.GetSection("Notifications"));
builder.Services.Configure<PatternDetectionSettings>(
    builder.Configuration.GetSection("PatternDetection"));
builder.Services.Configure<AlertEscalationSettings>(
    builder.Configuration.GetSection("AlertEscalation"));
builder.Services.Configure<MongoDbSettings>(
    builder.Configuration.GetSection("MongoDB"));

// MongoDB Configuration
builder.Services.AddSingleton<IMongoClient>(sp =>
{
    var settings = builder.Configuration.GetSection("MongoDB").Get<MongoDbSettings>() ?? new MongoDbSettings();
    var clientSettings = MongoClientSettings.FromConnectionString(settings.ConnectionString);
    clientSettings.ConnectTimeout = TimeSpan.FromSeconds(settings.ConnectionTimeoutSeconds);
    clientSettings.SocketTimeout = TimeSpan.FromSeconds(settings.SocketTimeoutSeconds);
    clientSettings.MaxConnectionPoolSize = settings.MaxConnectionPoolSize;
    clientSettings.ServerSelectionTimeout = TimeSpan.FromSeconds(settings.ServerSelectionTimeoutSeconds);
    return new MongoClient(clientSettings);
});

builder.Services.AddSingleton<IMongoDatabase>(sp =>
{
    var client = sp.GetRequiredService<IMongoClient>();
    var settings = builder.Configuration.GetSection("MongoDB").Get<MongoDbSettings>() ?? new MongoDbSettings();
    return client.GetDatabase(settings.DatabaseName);
});

// Repository services
builder.Services.AddScoped<IPatternAlertRepository, PatternAlertRepository>();
builder.Services.AddScoped<IAlertSuppressionRuleRepository, AlertSuppressionRuleRepository>();
builder.Services.AddScoped<IErrorPatternRepository, ErrorPatternRepository>();
builder.Services.AddScoped<IErrorClusterRepository, ErrorClusterRepository>();
builder.Services.AddScoped<IErrorEntryRepository, ErrorEntryRepository>();
builder.Services.AddScoped<IRootCauseAnalysisRepository, RootCauseAnalysisRepository>();
builder.Services.AddScoped<IPatternResolutionRepository, PatternResolutionRepository>();

// Core services
builder.Services.AddHttpClient();
builder.Services.AddScoped<IPatternDetectionService, PatternDetectionService>();
builder.Services.AddScoped<IErrorNormalizationService, ErrorNormalizationService>();
builder.Services.AddScoped<IFrequencyAnalyzer, FrequencyAnalyzer>();
builder.Services.AddScoped<IRootCauseAnalysisEngine, RootCauseAnalysisEngine>();
builder.Services.AddScoped<ITrendAnalyzer, TrendAnalyzer>();

// Log ingestion services
builder.Services.AddScoped<ILogIngestionService, LogIngestionService>();
builder.Services.AddHttpClient<INewRelicLogIngestionService, NewRelicLogIngestionService>();
builder.Services.AddHttpClient<ISeqLogIngestionService, SeqLogIngestionService>();
builder.Services.AddScoped<IMongoAuditLogIngestionService, MongoAuditLogIngestionService>();
builder.Services.AddScoped<MongoLogReaderService>();

// ML services
builder.Services.AddScoped<ErrorClusteringEngine>();

// Alerting services
builder.Services.AddScoped<IAlertService, AlertService>();
builder.Services.AddScoped<ISlackNotificationService, SlackNotificationService>();
builder.Services.AddScoped<ITeamsNotificationService, TeamsNotificationService>();
builder.Services.AddScoped<IEmailNotificationService, EmailNotificationService>();
builder.Services.AddScoped<IAlertSuppressionService, AlertSuppressionService>();

// Real-time services  
// builder.Services.AddHostedService<RealTimeDataService>();

// Configure logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwaggerConfiguration(app.Environment);
}

// Enable CORS
app.UseCors("AllowAll");

// Add health checks endpoint
app.MapHealthChecks("/health");
app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});
app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = _ => false
});

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// Add SignalR hub  
// app.MapHub<ErrorPatternHub>("/errorPatternHub");

// Add static files for real-time demo
app.UseStaticFiles();

// Add root endpoint for API information
app.MapGet("/", () => new
{
    Name = "AutoFixer API",
    Version = "1.0.0",
    Description = "Intelligent Error Pattern Detection and Alerting System",
    Documentation = "/api-docs",
    Health = "/health",
    Endpoints = new
    {
        Patterns = "/api/patterns",
        Alerts = "/api/alerts",
        Dashboard = "/api/dashboard"
    }
});

app.Run();
