using AutoFixer.Configuration;
using AutoFixer.Data;
using AutoFixer.Data.Repositories;
using AutoFixer.Services;
using AutoFixer.Services.Interfaces;

namespace AutoFixer.Extensions;

/// <summary>
/// Extension methods for registering services with dependency injection
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers all AutoFixer services with the dependency injection container
    /// </summary>
    public static IServiceCollection AddAutoFixerServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Add configuration
        services.Configure<MongoDbSettings>(configuration.GetSection(MongoDbSettings.SectionName));
        services.Configure<SeqSettings>(configuration.GetSection(SeqSettings.SectionName));
        services.Configure<NewRelicSettings>(configuration.GetSection(NewRelicSettings.SectionName));
        services.Configure<SlackSettings>(configuration.GetSection(SlackSettings.SectionName));
        services.Configure<TeamsSettings>(configuration.GetSection(TeamsSettings.SectionName));
        services.Configure<MLSettings>(configuration.GetSection(MLSettings.SectionName));

        // Add database context
        services.AddSingleton<IAutoFixerDbContext, AutoFixerDbContext>();

        // Add repositories
        services.AddScoped<IErrorEntryRepository>(provider =>
        {
            var dbContext = provider.GetRequiredService<IAutoFixerDbContext>();
            var settings = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<MongoDbSettings>>();
            return new ErrorEntryRepository(dbContext.Database, settings.Value.ErrorEntriesCollection);
        });

        services.AddScoped<IErrorClusterRepository>(provider =>
        {
            var dbContext = provider.GetRequiredService<IAutoFixerDbContext>();
            var settings = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<MongoDbSettings>>();
            return new ErrorClusterRepository(dbContext.Database, settings.Value.ErrorClustersCollection);
        });

        services.AddScoped<IErrorPatternRepository>(provider =>
        {
            var dbContext = provider.GetRequiredService<IAutoFixerDbContext>();
            var settings = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<MongoDbSettings>>();
            return new ErrorPatternRepository(dbContext.Database, settings.Value.ErrorPatternsCollection);
        });

        services.AddScoped<IRootCauseAnalysisRepository>(provider =>
        {
            var dbContext = provider.GetRequiredService<IAutoFixerDbContext>();
            var settings = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<MongoDbSettings>>();
            return new RootCauseAnalysisRepository(dbContext.Database, settings.Value.RootCauseAnalysesCollection);
        });

        services.AddScoped<IPatternResolutionRepository>(provider =>
        {
            var dbContext = provider.GetRequiredService<IAutoFixerDbContext>();
            var settings = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<MongoDbSettings>>();
            return new PatternResolutionRepository(dbContext.Database, settings.Value.PatternResolutionsCollection);
        });

        services.AddScoped<IPatternAlertRepository>(provider =>
        {
            var dbContext = provider.GetRequiredService<IAutoFixerDbContext>();
            var settings = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<MongoDbSettings>>();
            return new PatternAlertRepository(dbContext.Database, settings.Value.PatternAlertsCollection);
        });

        // Add HTTP client for external API calls
        services.AddHttpClient<ISeqLogIngestionService, SeqLogIngestionService>();
        services.AddHttpClient<INewRelicLogIngestionService, NewRelicLogIngestionService>();

        // Add ingestion services
        services.AddScoped<ILogIngestionService, LogIngestionService>();
        services.AddScoped<IMongoAuditLogIngestionService, MongoAuditLogIngestionService>();

        // Add ML services
        services.AddScoped<IErrorNormalizationService, ErrorNormalizationService>();
        services.AddScoped<IErrorClusteringEngine, ML.ErrorClusteringEngine>();

        // Add pattern detection services
        services.AddScoped<ITrendAnalyzer, TrendAnalyzer>();
        services.AddScoped<IFrequencyAnalyzer, FrequencyAnalyzer>();
        services.AddScoped<IPatternDetectionService, PatternDetectionService>();

        // Add root cause analysis services
        services.AddScoped<IRootCauseAnalysisEngine, RootCauseAnalysisEngine>();

        return services;
    }
}