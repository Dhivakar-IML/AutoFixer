using AutoFixer.Models;
using AutoFixer.Services.Interfaces;
using AutoFixer.Data.Repositories.Interfaces;
using Microsoft.Extensions.Logging;

namespace AutoFixer.Services;

/// <summary>
/// Service for managing alert suppression rules and evaluating alerts against them
/// </summary>
public class AlertSuppressionService : IAlertSuppressionService
{
    private readonly IAlertSuppressionRuleRepository _suppressionRuleRepository;
    private readonly ILogger<AlertSuppressionService> _logger;

    public AlertSuppressionService(
        IAlertSuppressionRuleRepository suppressionRuleRepository,
        ILogger<AlertSuppressionService> logger)
    {
        _suppressionRuleRepository = suppressionRuleRepository;
        _logger = logger;
    }

    /// <summary>
    /// Evaluates whether an alert should be suppressed based on existing suppression rules
    /// </summary>
    public async Task<bool> ShouldSuppressAlertAsync(PatternAlert alert, CancellationToken cancellationToken = default)
    {
        try
        {
            var activeRules = await _suppressionRuleRepository.GetActiveRulesAsync(cancellationToken);
            
            foreach (var rule in activeRules)
            {
                if (await EvaluateRuleAsync(alert, rule, cancellationToken))
                {
                    _logger.LogInformation("Alert {AlertId} suppressed by rule {RuleId}: {RuleName}", 
                        alert.Id, rule.Id, rule.Name);
                    
                    // Update rule usage statistics
                    await _suppressionRuleRepository.IncrementRuleUsageAsync(rule.Id, cancellationToken);
                    
                    return true;
                }
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error evaluating alert suppression for alert {AlertId}", alert.Id);
            return false; // Don't suppress on errors to avoid missing critical alerts
        }
    }

    /// <summary>
    /// Creates a new alert suppression rule
    /// </summary>
    public async Task<AlertSuppressionRule> CreateSuppressionRuleAsync(
        string name,
        string description,
        List<SuppressionCondition> conditions,
        TimeSpan? duration = null,
        bool isActive = true,
        CancellationToken cancellationToken = default)
    {
        var rule = new AlertSuppressionRule
        {
            Id = Guid.NewGuid().ToString(),
            Name = name,
            Description = description,
            Conditions = conditions,
            IsActive = isActive,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = duration.HasValue ? DateTime.UtcNow.Add(duration.Value) : null,
            CreatedBy = "system", // TODO: Get from current user context
            TimesTriggered = 0
        };

        await _suppressionRuleRepository.CreateAsync(rule, cancellationToken);
        
        _logger.LogInformation("Created suppression rule {RuleId}: {RuleName}", rule.Id, rule.Name);
        
        return rule;
    }

    /// <summary>
    /// Updates an existing suppression rule
    /// </summary>
    public async Task<AlertSuppressionRule?> UpdateSuppressionRuleAsync(
        string ruleId,
        string? name = null,
        string? description = null,
        List<SuppressionCondition>? conditions = null,
        bool? isActive = null,
        TimeSpan? duration = null,
        CancellationToken cancellationToken = default)
    {
        var existingRule = await _suppressionRuleRepository.GetByIdAsync(ruleId, cancellationToken);
        if (existingRule == null)
        {
            _logger.LogWarning("Suppression rule {RuleId} not found for update", ruleId);
            return null;
        }

        var updates = new Dictionary<string, object>();
        
        if (name != null) updates["Name"] = name;
        if (description != null) updates["Description"] = description;
        if (conditions != null) updates["Conditions"] = conditions;
        if (isActive.HasValue) updates["IsActive"] = isActive.Value;
        if (duration.HasValue) updates["ExpiresAt"] = DateTime.UtcNow.Add(duration.Value);
        
        updates["UpdatedAt"] = DateTime.UtcNow;

        await _suppressionRuleRepository.UpdateAsync(ruleId, updates, cancellationToken);
        
        _logger.LogInformation("Updated suppression rule {RuleId}", ruleId);
        
        return await _suppressionRuleRepository.GetByIdAsync(ruleId, cancellationToken);
    }

    /// <summary>
    /// Deletes a suppression rule
    /// </summary>
    public async Task<bool> DeleteSuppressionRuleAsync(string ruleId, CancellationToken cancellationToken = default)
    {
        var result = await _suppressionRuleRepository.DeleteAsync(ruleId, cancellationToken);
        
        if (result)
        {
            _logger.LogInformation("Deleted suppression rule {RuleId}", ruleId);
        }
        else
        {
            _logger.LogWarning("Failed to delete suppression rule {RuleId} - not found", ruleId);
        }
        
        return result;
    }

    /// <summary>
    /// Gets all suppression rules with optional filtering
    /// </summary>
    public async Task<List<AlertSuppressionRule>> GetSuppressionRulesAsync(
        bool? isActive = null,
        CancellationToken cancellationToken = default)
    {
        return await _suppressionRuleRepository.GetAllAsync(isActive, cancellationToken);
    }

    /// <summary>
    /// Gets a specific suppression rule by ID
    /// </summary>
    public async Task<AlertSuppressionRule?> GetSuppressionRuleAsync(string ruleId, CancellationToken cancellationToken = default)
    {
        return await _suppressionRuleRepository.GetByIdAsync(ruleId, cancellationToken);
    }

    /// <summary>
    /// Cleans up expired suppression rules
    /// </summary>
    public async Task CleanupExpiredRulesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var expiredCount = await _suppressionRuleRepository.DeleteExpiredRulesAsync(cancellationToken);
            
            if (expiredCount > 0)
            {
                _logger.LogInformation("Cleaned up {Count} expired suppression rules", expiredCount);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up expired suppression rules");
        }
    }

    /// <summary>
    /// Evaluates a specific rule against an alert
    /// </summary>
    private async Task<bool> EvaluateRuleAsync(PatternAlert alert, AlertSuppressionRule rule, CancellationToken cancellationToken)
    {
        try
        {
            // Check if rule is expired
            if (rule.ExpiresAt.HasValue && rule.ExpiresAt.Value <= DateTime.UtcNow)
            {
                _logger.LogDebug("Suppression rule {RuleId} has expired", rule.Id);
                return false;
            }

            // All conditions must match for the rule to trigger
            foreach (var condition in rule.Conditions)
            {
                if (!await EvaluateConditionAsync(alert, condition, cancellationToken))
                {
                    return false;
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error evaluating suppression rule {RuleId}", rule.Id);
            return false;
        }
    }

    /// <summary>
    /// Evaluates a specific condition against an alert
    /// </summary>
    private async Task<bool> EvaluateConditionAsync(PatternAlert alert, SuppressionCondition condition, CancellationToken cancellationToken)
    {
        try
        {
            var alertValue = GetAlertFieldValue(alert, condition.Field);
            if (alertValue == null)
            {
                return false;
            }

            return condition.Operator switch
            {
                SuppressionOperator.Equals => string.Equals(alertValue, condition.Value, StringComparison.OrdinalIgnoreCase),
                SuppressionOperator.NotEquals => !string.Equals(alertValue, condition.Value, StringComparison.OrdinalIgnoreCase),
                SuppressionOperator.Contains => alertValue.Contains(condition.Value, StringComparison.OrdinalIgnoreCase),
                SuppressionOperator.NotContains => !alertValue.Contains(condition.Value, StringComparison.OrdinalIgnoreCase),
                SuppressionOperator.StartsWith => alertValue.StartsWith(condition.Value, StringComparison.OrdinalIgnoreCase),
                SuppressionOperator.EndsWith => alertValue.EndsWith(condition.Value, StringComparison.OrdinalIgnoreCase),
                SuppressionOperator.Regex => System.Text.RegularExpressions.Regex.IsMatch(alertValue, condition.Value, System.Text.RegularExpressions.RegexOptions.IgnoreCase),
                SuppressionOperator.GreaterThan => CompareNumericValues(alertValue, condition.Value) > 0,
                SuppressionOperator.LessThan => CompareNumericValues(alertValue, condition.Value) < 0,
                SuppressionOperator.GreaterThanOrEqual => CompareNumericValues(alertValue, condition.Value) >= 0,
                SuppressionOperator.LessThanOrEqual => CompareNumericValues(alertValue, condition.Value) <= 0,
                _ => false
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error evaluating condition for field {Field} with operator {Operator}", 
                condition.Field, condition.Operator);
            return false;
        }
    }

    /// <summary>
    /// Gets the value of a specific field from an alert
    /// </summary>
    private string? GetAlertFieldValue(PatternAlert alert, string field)
    {
        return field.ToLowerInvariant() switch
        {
            "patternname" => alert.PatternName,
            "severity" => alert.Severity.ToString(),
            "source" => alert.Source,
            "environment" => alert.Environment,
            "message" => alert.Message,
            "patternid" => alert.PatternId,
            "triggercount" => alert.TriggerCount.ToString(),
            "escalationlevel" => alert.EscalationLevel.ToString(),
            "status" => alert.Status.ToString(),
            _ => null
        };
    }

    /// <summary>
    /// Compares two values as numbers if possible
    /// </summary>
    private int CompareNumericValues(string alertValue, string conditionValue)
    {
        if (double.TryParse(alertValue, out var alertNum) && double.TryParse(conditionValue, out var conditionNum))
        {
            return alertNum.CompareTo(conditionNum);
        }
        
        // Fall back to string comparison if not numeric
        return string.Compare(alertValue, conditionValue, StringComparison.OrdinalIgnoreCase);
    }
}