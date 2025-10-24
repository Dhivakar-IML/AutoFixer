using AutoFixer.Models;
using AutoFixer.Services.Interfaces;
using AutoFixer.Data.Repositories;
using AutoFixer.Configuration;
using Microsoft.Extensions.Options;
using System.Text.RegularExpressions;

namespace AutoFixer.Services;

/// <summary>
/// AI-powered root cause analysis engine that analyzes error patterns and generates hypotheses
/// </summary>
public class RootCauseAnalysisEngine : IRootCauseAnalysisEngine
{
    private readonly IRootCauseAnalysisRepository _analysisRepository;
    private readonly IErrorEntryRepository _errorEntryRepository;
    private readonly IPatternResolutionRepository _resolutionRepository;
    private readonly MLSettings _mlSettings;
    private readonly ILogger<RootCauseAnalysisEngine> _logger;

    // Common error patterns and their likely root causes
    private readonly Dictionary<string, List<string>> _commonErrorPatterns = new()
    {
        { "NullReferenceException", new() { "Uninitialized object", "Missing null check", "Async timing issue" } },
        { "TimeoutException", new() { "Database connectivity", "Network latency", "Resource contention", "Deadlock" } },
        { "OutOfMemoryException", new() { "Memory leak", "Large dataset processing", "Inefficient algorithms" } },
        { "SqlException", new() { "Database connection", "Query performance", "Missing indexes", "Deadlock" } },
        { "UnauthorizedAccessException", new() { "Authentication failure", "Permission issues", "Token expiration" } },
        { "ArgumentException", new() { "Input validation", "Data type mismatch", "Parameter validation" } },
        { "InvalidOperationException", new() { "State management", "Concurrency issues", "Resource disposal" } },
        { "HttpRequestException", new() { "API connectivity", "Service unavailability", "Network issues" } }
    };

    public RootCauseAnalysisEngine(
        IRootCauseAnalysisRepository analysisRepository,
        IErrorEntryRepository errorEntryRepository,
        IPatternResolutionRepository resolutionRepository,
        IOptions<MLSettings> mlSettings,
        ILogger<RootCauseAnalysisEngine> logger)
    {
        _analysisRepository = analysisRepository;
        _errorEntryRepository = errorEntryRepository;
        _resolutionRepository = resolutionRepository;
        _mlSettings = mlSettings.Value;
        _logger = logger;
    }

    public async Task<RootCauseAnalysis> AnalyzePatternAsync(ErrorPattern pattern, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting root cause analysis for pattern {PatternId}", pattern.Id);

        try
        {
            // Check if analysis already exists
            var existingAnalysis = await _analysisRepository.GetByPatternIdAsync(pattern.Id!, cancellationToken);
            if (existingAnalysis != null && existingAnalysis.UpdatedAt > DateTime.UtcNow.AddHours(-24))
            {
                _logger.LogDebug("Using existing analysis for pattern {PatternId}", pattern.Id);
                return existingAnalysis;
            }

            // Get error entries for this pattern
            var errors = new List<ErrorEntry>();
            foreach (var clusterId in pattern.ClusterIds)
            {
                var clusterErrors = await _errorEntryRepository.GetByClusterIdAsync(clusterId, cancellationToken);
                errors.AddRange(clusterErrors);
            }

            // Generate hypotheses
            var hypotheses = await GenerateHypothesesAsync(pattern, errors, cancellationToken);

            // Create or update analysis
            var analysis = existingAnalysis ?? new RootCauseAnalysis
            {
                Id = Guid.NewGuid().ToString(),
                PatternId = pattern.Id!,
                CreatedAt = DateTime.UtcNow
            };

            analysis.Hypotheses = hypotheses;
            analysis.AffectedComponents = ExtractAffectedComponents(errors);
            analysis.AffectedDependencies = ExtractAffectedDependencies(errors);
            analysis.ConfidenceScores = hypotheses.ToDictionary(h => h.Category, h => h.Confidence);
            analysis.RelatedCodeLocations = ExtractCodeLocations(errors);
            analysis.UpdatedAt = DateTime.UtcNow;

            // Save analysis
            if (existingAnalysis != null)
            {
                await _analysisRepository.UpdateAnalysisAsync(pattern.Id!, analysis, cancellationToken);
            }
            else
            {
                await _analysisRepository.CreateAsync(analysis, cancellationToken);
            }

            _logger.LogInformation("Completed root cause analysis for pattern {PatternId} with {HypothesesCount} hypotheses", 
                pattern.Id, hypotheses.Count);

            return analysis;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing pattern {PatternId}", pattern.Id);
            throw;
        }
    }

    public async Task<List<RootCauseHypothesis>> GenerateHypothesesAsync(ErrorPattern pattern, List<ErrorEntry> errors, CancellationToken cancellationToken = default)
    {
        var hypotheses = new List<RootCauseHypothesis>();

        try
        {
            // Analyze exception types
            var exceptionHypotheses = await AnalyzeExceptionTypesAsync(errors, cancellationToken);
            hypotheses.AddRange(exceptionHypotheses);

            // Analyze temporal patterns
            var temporalHypotheses = await AnalyzeTemporalPatternsAsync(errors, cancellationToken);
            hypotheses.AddRange(temporalHypotheses);

            // Analyze stack traces
            var stackTraceHypotheses = await AnalyzeStackTracesAsync(errors, cancellationToken);
            hypotheses.AddRange(stackTraceHypotheses);

            // Analyze user impact patterns
            var userImpactHypotheses = await AnalyzeUserImpactPatternsAsync(errors, cancellationToken);
            hypotheses.AddRange(userImpactHypotheses);

            // Learn from historical resolutions
            var historicalHypotheses = await AnalyzeHistoricalResolutionsAsync(pattern, cancellationToken);
            hypotheses.AddRange(historicalHypotheses);

            // Rank hypotheses by confidence
            hypotheses = hypotheses
                .OrderByDescending(h => h.Confidence)
                .Take(10) // Limit to top 10 hypotheses
                .ToList();

            _logger.LogDebug("Generated {Count} hypotheses for pattern analysis", hypotheses.Count);
            return hypotheses;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating hypotheses");
            return new List<RootCauseHypothesis>();
        }
    }

    public async Task<List<SolutionSuggestion>> GenerateSolutionsAsync(RootCauseAnalysis analysis, CancellationToken cancellationToken = default)
    {
        var solutions = new List<SolutionSuggestion>();

        try
        {
            foreach (var hypothesis in analysis.Hypotheses.Take(5)) // Top 5 hypotheses
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                var hypothesisSolutions = await GenerateSolutionsForHypothesisAsync(hypothesis, cancellationToken);
                solutions.AddRange(hypothesisSolutions);
            }

            // Remove duplicates and rank by success rate
            solutions = solutions
                .GroupBy(s => s.Description)
                .Select(g => g.OrderByDescending(s => s.TimesSuccessful).First())
                .OrderByDescending(s => s.TimesSuccessful)
                .ThenBy(s => s.RiskLevel)
                .Take(15) // Limit to top 15 solutions
                .ToList();

            _logger.LogDebug("Generated {Count} solutions for analysis {AnalysisId}", solutions.Count, analysis.Id);
            return solutions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating solutions for analysis {AnalysisId}", analysis.Id);
            return new List<SolutionSuggestion>();
        }
    }

    public async Task<bool> UpdateConfidenceScoresAsync(string analysisId, List<RootCauseHypothesis> hypotheses, CancellationToken cancellationToken = default)
    {
        try
        {
            var analysis = await _analysisRepository.GetByIdAsync(analysisId, cancellationToken);
            if (analysis == null)
                return false;

            analysis.Hypotheses = hypotheses;
            analysis.ConfidenceScores = hypotheses.ToDictionary(h => h.Category, h => h.Confidence);
            analysis.UpdatedAt = DateTime.UtcNow;

            return await _analysisRepository.UpdateAnalysisAsync(analysis.PatternId, analysis, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating confidence scores for analysis {AnalysisId}", analysisId);
            return false;
        }
    }

    public async Task<bool> LearnFromResolutionAsync(string patternId, PatternResolution resolution, CancellationToken cancellationToken = default)
    {
        try
        {
            var analysis = await _analysisRepository.GetByPatternIdAsync(patternId, cancellationToken);
            if (analysis == null)
                return false;

            // Update success counts for matching solutions
            foreach (var hypothesis in analysis.Hypotheses)
            {
                foreach (var suggestion in hypothesis.Suggestions)
                {
                    if (IsResolutionMatch(suggestion, resolution))
                    {
                        suggestion.TimesSuccessful++;
                        _logger.LogDebug("Incremented success count for solution: {Solution}", suggestion.Description);
                    }
                }
            }

            analysis.UpdatedAt = DateTime.UtcNow;
            return await _analysisRepository.UpdateAnalysisAsync(patternId, analysis, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error learning from resolution for pattern {PatternId}", patternId);
            return false;
        }
    }

    private async Task<List<RootCauseHypothesis>> AnalyzeExceptionTypesAsync(List<ErrorEntry> errors, CancellationToken cancellationToken)
    {
        var hypotheses = new List<RootCauseHypothesis>();
        await Task.CompletedTask; // Make method async

        var exceptionCounts = errors
            .Where(e => !string.IsNullOrEmpty(e.ExceptionType))
            .GroupBy(e => e.ExceptionType)
            .ToDictionary(g => g.Key!, g => g.Count());

        foreach (var (exceptionType, count) in exceptionCounts)
        {
            if (_commonErrorPatterns.TryGetValue(exceptionType, out var commonCauses))
            {
                var confidence = Math.Min(0.9, count / (double)errors.Count);
                
                foreach (var cause in commonCauses)
                {
                    hypotheses.Add(new RootCauseHypothesis
                    {
                        Description = $"{exceptionType} likely caused by: {cause}",
                        Confidence = confidence,
                        Category = "Exception Analysis",
                        SupportingEvidence = new List<string> { $"{count} occurrences of {exceptionType}" },
                        Severity = confidence > 0.7 ? HypothesisSeverity.High : HypothesisSeverity.Medium,
                        Suggestions = GenerateStandardSolutions(exceptionType, cause)
                    });
                }
            }
        }

        return hypotheses;
    }

    private async Task<List<RootCauseHypothesis>> AnalyzeTemporalPatternsAsync(List<ErrorEntry> errors, CancellationToken cancellationToken)
    {
        var hypotheses = new List<RootCauseHypothesis>();
        await Task.CompletedTask; // Make method async

        // Check for time-based patterns
        var hourlyDistribution = errors
            .GroupBy(e => e.Timestamp.Hour)
            .ToDictionary(g => g.Key, g => g.Count());

        var maxHourlyCount = hourlyDistribution.Values.DefaultIfEmpty(0).Max();
        var avgHourlyCount = hourlyDistribution.Values.DefaultIfEmpty(0).Average();

        if (maxHourlyCount > avgHourlyCount * 3) // Spike detected
        {
            var peakHour = hourlyDistribution.First(kvp => kvp.Value == maxHourlyCount).Key;
            
            hypotheses.Add(new RootCauseHypothesis
            {
                Description = $"Error spike detected during hour {peakHour}:00. Likely related to scheduled processes or high traffic periods.",
                Confidence = 0.8,
                Category = "Temporal Analysis",
                SupportingEvidence = new List<string> { $"Peak at hour {peakHour} with {maxHourlyCount} errors vs average of {avgHourlyCount:F1}" },
                Severity = HypothesisSeverity.Medium,
                Suggestions = new List<SolutionSuggestion>
                {
                    new() { 
                        Description = "Investigate scheduled jobs or processes running during peak hours",
                        Category = SolutionCategory.ProcessImprovement,
                        EstimatedEffort = "Medium",
                        RiskLevel = "Low"
                    }
                }
            });
        }

        return hypotheses;
    }

    private async Task<List<RootCauseHypothesis>> AnalyzeStackTracesAsync(List<ErrorEntry> errors, CancellationToken cancellationToken)
    {
        var hypotheses = new List<RootCauseHypothesis>();
        await Task.CompletedTask; // Make method async

        var stackTraces = errors
            .Where(e => !string.IsNullOrEmpty(e.StackTrace))
            .Select(e => e.StackTrace!)
            .ToList();

        if (!stackTraces.Any())
            return hypotheses;

        // Analyze common stack trace patterns
        var commonMethods = ExtractCommonMethods(stackTraces);
        var commonNamespaces = ExtractCommonNamespaces(stackTraces);

        if (commonMethods.Any())
        {
            var topMethod = commonMethods.First();
            hypotheses.Add(new RootCauseHypothesis
            {
                Description = $"Errors frequently originate from method: {topMethod.Key}",
                Confidence = Math.Min(0.9, topMethod.Value / (double)stackTraces.Count),
                Category = "Code Analysis",
                SupportingEvidence = new List<string> { $"Method {topMethod.Key} appears in {topMethod.Value} stack traces" },
                Severity = HypothesisSeverity.High,
                Suggestions = new List<SolutionSuggestion>
                {
                    new() {
                        Description = $"Review and add error handling to method {topMethod.Key}",
                        Category = SolutionCategory.CodeFix,
                        EstimatedEffort = "Medium",
                        RiskLevel = "Low"
                    }
                }
            });
        }

        return hypotheses;
    }

    private async Task<List<RootCauseHypothesis>> AnalyzeUserImpactPatternsAsync(List<ErrorEntry> errors, CancellationToken cancellationToken)
    {
        var hypotheses = new List<RootCauseHypothesis>();
        await Task.CompletedTask; // Make method async

        var userErrors = errors
            .Where(e => !string.IsNullOrEmpty(e.UserId))
            .GroupBy(e => e.UserId)
            .ToDictionary(g => g.Key!, g => g.Count());

        if (userErrors.Any())
        {
            var affectedUserCount = userErrors.Count;
            var totalErrors = errors.Count;
            var avgErrorsPerUser = userErrors.Values.Average();

            if (affectedUserCount < totalErrors * 0.1) // Less than 10% of errors have user IDs
            {
                hypotheses.Add(new RootCauseHypothesis
                {
                    Description = "Errors may be occurring in background processes or system operations rather than user-initiated actions",
                    Confidence = 0.7,
                    Category = "User Impact Analysis",
                    SupportingEvidence = new List<string> { $"Only {affectedUserCount} users affected out of {totalErrors} total errors" },
                    Severity = HypothesisSeverity.Medium,
                    Suggestions = new List<SolutionSuggestion>
                    {
                        new() {
                            Description = "Investigate background services and scheduled tasks",
                            Category = SolutionCategory.Monitoring,
                            EstimatedEffort = "Medium",
                            RiskLevel = "Low"
                        }
                    }
                });
            }
        }

        return hypotheses;
    }

    private async Task<List<RootCauseHypothesis>> AnalyzeHistoricalResolutionsAsync(ErrorPattern pattern, CancellationToken cancellationToken)
    {
        var hypotheses = new List<RootCauseHypothesis>();

        try
        {
            // Find similar resolved patterns
            // This is a simplified approach - in a real implementation, you'd use more sophisticated similarity matching
            var allResolutions = await _resolutionRepository.GetAllAsync(cancellationToken);
            var similarResolutions = allResolutions
                .Where(r => r.Effectiveness > 0.8) // Only successful resolutions
                .Take(5) // Limit for performance
                .ToList();

            foreach (var resolution in similarResolutions)
            {
                hypotheses.Add(new RootCauseHypothesis
                {
                    Description = $"Based on historical resolution: {resolution.SolutionApplied}",
                    Confidence = 0.6 * resolution.Effectiveness, // Moderate confidence based on historical success
                    Category = "Historical Analysis",
                    SupportingEvidence = new List<string> { $"Similar pattern resolved with {resolution.Effectiveness:P1} effectiveness" },
                    Severity = HypothesisSeverity.Medium,
                    Suggestions = new List<SolutionSuggestion>
                    {
                        new() {
                            Description = resolution.SolutionApplied,
                            Category = SolutionCategory.CodeFix,
                            TimesSuccessful = 1,
                            EstimatedEffort = "Medium",
                            RiskLevel = "Low"
                        }
                    }
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing historical resolutions");
        }

        return hypotheses;
    }

    private List<string> ExtractAffectedComponents(List<ErrorEntry> errors)
    {
        var components = new HashSet<string>();

        foreach (var error in errors)
        {
            if (!string.IsNullOrEmpty(error.Source))
                components.Add(error.Source);

            if (!string.IsNullOrEmpty(error.Endpoint))
            {
                // Extract component from endpoint (e.g., "/api/users" -> "users")
                var segments = error.Endpoint.Split('/', StringSplitOptions.RemoveEmptyEntries);
                if (segments.Length > 1)
                    components.Add(segments[1]);
            }
        }

        return components.ToList();
    }

    private List<string> ExtractAffectedDependencies(List<ErrorEntry> errors)
    {
        var dependencies = new HashSet<string>();

        foreach (var error in errors)
        {
            if (!string.IsNullOrEmpty(error.StackTrace))
            {
                // Extract external dependencies from stack traces
                var matches = Regex.Matches(error.StackTrace, @"at ([a-zA-Z0-9_.]+\.[a-zA-Z0-9_]+)\.");
                foreach (Match match in matches)
                {
                    var @namespace = match.Groups[1].Value;
                    if (!@namespace.StartsWith("System.") && !@namespace.StartsWith("Microsoft."))
                    {
                        dependencies.Add(@namespace);
                    }
                }
            }
        }

        return dependencies.ToList();
    }

    private List<string> ExtractCodeLocations(List<ErrorEntry> errors)
    {
        var locations = new HashSet<string>();

        foreach (var error in errors)
        {
            if (!string.IsNullOrEmpty(error.StackTrace))
            {
                // Extract file and line information
                var matches = Regex.Matches(error.StackTrace, @"in (.+\.cs):line (\d+)");
                foreach (Match match in matches)
                {
                    locations.Add($"{match.Groups[1].Value}:{match.Groups[2].Value}");
                }
            }
        }

        return locations.Take(10).ToList(); // Limit to avoid too much data
    }

    private Dictionary<string, int> ExtractCommonMethods(List<string> stackTraces)
    {
        var methods = new Dictionary<string, int>();

        foreach (var stackTrace in stackTraces)
        {
            var matches = Regex.Matches(stackTrace, @"at ([a-zA-Z0-9_.]+\.[a-zA-Z0-9_]+)\(");
            foreach (Match match in matches.Take(3)) // Only top 3 methods in each stack trace
            {
                var method = match.Groups[1].Value;
                methods[method] = methods.GetValueOrDefault(method, 0) + 1;
            }
        }

        return methods
            .OrderByDescending(kvp => kvp.Value)
            .Take(5)
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }

    private Dictionary<string, int> ExtractCommonNamespaces(List<string> stackTraces)
    {
        var namespaces = new Dictionary<string, int>();

        foreach (var stackTrace in stackTraces)
        {
            var matches = Regex.Matches(stackTrace, @"at ([a-zA-Z0-9_]+\.[a-zA-Z0-9_]+)\.");
            foreach (Match match in matches.Take(5))
            {
                var @namespace = match.Groups[1].Value;
                namespaces[@namespace] = namespaces.GetValueOrDefault(@namespace, 0) + 1;
            }
        }

        return namespaces
            .OrderByDescending(kvp => kvp.Value)
            .Take(5)
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }

    private List<SolutionSuggestion> GenerateStandardSolutions(string exceptionType, string cause)
    {
        var solutions = new List<SolutionSuggestion>();

        // Generate standard solutions based on exception type and cause
        switch (exceptionType)
        {
            case "NullReferenceException":
                solutions.Add(new SolutionSuggestion
                {
                    Description = "Add null checks before object access",
                    CodeExample = "if (obj != null) { obj.Method(); }",
                    Category = SolutionCategory.CodeFix,
                    EstimatedEffort = "Low",
                    RiskLevel = "Low"
                });
                break;
            case "TimeoutException":
                solutions.Add(new SolutionSuggestion
                {
                    Description = "Increase timeout values or optimize query performance",
                    Category = SolutionCategory.Configuration,
                    EstimatedEffort = "Medium",
                    RiskLevel = "Medium"
                });
                break;
            // Add more cases as needed
        }

        return solutions;
    }

    private async Task<List<SolutionSuggestion>> GenerateSolutionsForHypothesisAsync(RootCauseHypothesis hypothesis, CancellationToken cancellationToken)
    {
        await Task.CompletedTask; // Make method async

        // Return existing suggestions from hypothesis, or generate new ones based on category
        if (hypothesis.Suggestions.Any())
            return hypothesis.Suggestions;

        var solutions = new List<SolutionSuggestion>();

        switch (hypothesis.Category)
        {
            case "Exception Analysis":
                solutions.Add(new SolutionSuggestion
                {
                    Description = "Add comprehensive error handling and logging",
                    Category = SolutionCategory.CodeFix,
                    EstimatedEffort = "Medium",
                    RiskLevel = "Low"
                });
                break;
            case "Temporal Analysis":
                solutions.Add(new SolutionSuggestion
                {
                    Description = "Implement rate limiting or load balancing",
                    Category = SolutionCategory.Infrastructure,
                    EstimatedEffort = "High",
                    RiskLevel = "Medium"
                });
                break;
            case "Code Analysis":
                solutions.Add(new SolutionSuggestion
                {
                    Description = "Refactor problematic code section",
                    Category = SolutionCategory.CodeFix,
                    EstimatedEffort = "High",
                    RiskLevel = "Medium"
                });
                break;
        }

        return solutions;
    }

    private bool IsResolutionMatch(SolutionSuggestion suggestion, PatternResolution resolution)
    {
        // Simple matching logic - in a real implementation, you'd use more sophisticated comparison
        return suggestion.Description.Contains(resolution.SolutionApplied, StringComparison.OrdinalIgnoreCase) ||
               resolution.SolutionApplied.Contains(suggestion.Description, StringComparison.OrdinalIgnoreCase);
    }
}