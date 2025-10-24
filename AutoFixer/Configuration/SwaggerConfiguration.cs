using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using Microsoft.OpenApi.Any;
using System.Reflection;

namespace AutoFixer.Configuration;

/// <summary>
/// Configuration for Swagger/OpenAPI documentation
/// </summary>
public static class SwaggerConfiguration
{
    /// <summary>
    /// Configures Swagger services
    /// </summary>
    public static IServiceCollection AddSwaggerConfiguration(this IServiceCollection services)
    {
        services.AddSwaggerGen(options =>
        {
            // API Information
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "AutoFixer API",
                Version = "v1.0",
                Description = "Intelligent Error Pattern Detection and Alerting System",
                Contact = new OpenApiContact
                {
                    Name = "AutoFixer Team",
                    Email = "support@autofixer.com",
                    Url = new Uri("https://autofixer.com")
                },
                License = new OpenApiLicense
                {
                    Name = "MIT License",
                    Url = new Uri("https://opensource.org/licenses/MIT")
                }
            });

            // Include XML comments for better documentation
            var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFilename);
            if (File.Exists(xmlPath))
            {
                options.IncludeXmlComments(xmlPath);
            }

            // Configure security scheme for API keys (if needed in future)
            options.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
            {
                Description = "API Key needed to access the endpoints. API Key should be provided in the X-API-Key header",
                In = ParameterLocation.Header,
                Name = "X-API-Key",
                Type = SecuritySchemeType.ApiKey,
                Scheme = "ApiKeyScheme"
            });

            // Configure bearer token authentication (if needed in future)
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.ApiKey,
                Scheme = "Bearer"
            });

            // Group endpoints by controller
            options.TagActionsBy(api => new[] { api.GroupName ?? api.ActionDescriptor.RouteValues["controller"] });
            options.DocInclusionPredicate((name, api) => true);

            // Custom operation filters
            options.OperationFilter<SwaggerResponseExamplesFilter>();
            options.OperationFilter<SwaggerParameterFilter>();

            // Schema filters for better model documentation
            options.SchemaFilter<SwaggerSchemaFilter>();

            // Custom document filter
            options.DocumentFilter<SwaggerDocumentFilter>();
        });

        return services;
    }

    /// <summary>
    /// Configures Swagger UI
    /// </summary>
    public static IApplicationBuilder UseSwaggerConfiguration(this IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment() || env.IsStaging())
        {
            app.UseSwagger(options =>
            {
                options.RouteTemplate = "api-docs/{documentName}/swagger.json";
            });

            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/api-docs/v1/swagger.json", "AutoFixer API v1");
                options.RoutePrefix = "api-docs";
                options.DocumentTitle = "AutoFixer API Documentation";
                
                // UI Customization
                options.DefaultModelsExpandDepth(-1); // Hide models section by default
                options.DefaultModelExpandDepth(1);
                options.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.List);
                options.EnableFilter();
                options.EnableTryItOutByDefault();
                options.DisplayRequestDuration();
                
                // Custom CSS for better appearance
                options.InjectStylesheet("/swagger-ui/custom.css");
                
                // Enable deep linking
                options.EnableDeepLinking();
                
                // Show only the API documentation
                options.ConfigObject.AdditionalItems.Add("persistAuthorization", "true");
            });
        }

        return app;
    }
}

/// <summary>
/// Operation filter to add response examples
/// </summary>
public class SwaggerResponseExamplesFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        // Add common response examples
        if (operation.Responses.ContainsKey("400"))
        {
            operation.Responses["400"].Content.Add("application/json", new OpenApiMediaType
            {
                Example = new OpenApiString("""
                {
                  "title": "Bad Request",
                  "status": 400,
                  "detail": "The request parameters are invalid",
                  "traceId": "00-1234567890abcdef-1234567890abcdef-01"
                }
                """)
            });
        }

        if (operation.Responses.ContainsKey("500"))
        {
            operation.Responses["500"].Content.Add("application/json", new OpenApiMediaType
            {
                Example = new OpenApiString("""
                {
                  "title": "Internal Server Error",
                  "status": 500,
                  "detail": "An unexpected error occurred while processing the request",
                  "traceId": "00-1234567890abcdef-1234567890abcdef-01"
                }
                """)
            });
        }
    }
}

/// <summary>
/// Parameter filter for better parameter documentation
/// </summary>
public class SwaggerParameterFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        // Add descriptions for common parameters
        foreach (var parameter in operation.Parameters)
        {
            switch (parameter.Name.ToLowerInvariant())
            {
                case "timeframe":
                    parameter.Description ??= "Timeframe for data analysis in hours (1-720)";
                    parameter.Example = new OpenApiString("24");
                    break;
                case "count":
                    parameter.Description ??= "Number of items to return (1-100)";
                    parameter.Example = new OpenApiString("10");
                    break;
                case "severity":
                    parameter.Description ??= "Alert severity level (Info, Warning, Critical, Emergency)";
                    break;
                case "type":
                    parameter.Description ??= "Pattern type (Transient, Persistent, Trending)";
                    break;
                case "priority":
                    parameter.Description ??= "Pattern priority (Low, Medium, High, Critical)";
                    break;
            }
        }
    }
}

/// <summary>
/// Schema filter for better model documentation
/// </summary>
public class SwaggerSchemaFilter : ISchemaFilter
{
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        // Add examples for common models
        if (context.Type.Name == "PatternDetectionRequest")
        {
            schema.Example = new OpenApiString("""
            {
              "timeframeHours": 24,
              "minConfidence": 0.8,
              "forceReanalysis": false
            }
            """);
        }
        else if (context.Type.Name == "AlertAcknowledgmentRequest")
        {
            schema.Example = new OpenApiString("""
            {
              "acknowledgedBy": "john.doe@company.com",
              "notes": "Investigating the issue"
            }
            """);
        }
    }
}

/// <summary>
/// Document filter for API-wide documentation enhancements
/// </summary>
public class SwaggerDocumentFilter : IDocumentFilter
{
    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        // Add global tags for better organization
        swaggerDoc.Tags = new List<OpenApiTag>
        {
            new OpenApiTag
            {
                Name = "Patterns",
                Description = "Error pattern detection and management operations"
            },
            new OpenApiTag
            {
                Name = "Alerts",
                Description = "Alert management and notification operations"
            },
            new OpenApiTag
            {
                Name = "Dashboard",
                Description = "Monitoring, analytics, and health check operations"
            }
        };

        // Add servers information
        swaggerDoc.Servers = new List<OpenApiServer>
        {
            new OpenApiServer
            {
                Url = "https://api.autofixer.com",
                Description = "Production server"
            },
            new OpenApiServer
            {
                Url = "https://staging-api.autofixer.com",
                Description = "Staging server"
            },
            new OpenApiServer
            {
                Url = "http://localhost:5000",
                Description = "Development server"
            }
        };
    }
}