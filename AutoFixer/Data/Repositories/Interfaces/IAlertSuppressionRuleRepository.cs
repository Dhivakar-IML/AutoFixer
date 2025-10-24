using AutoFixer.Models;

namespace AutoFixer.Data.Repositories.Interfaces;

/// <summary>
/// Repository interface for managing alert suppression rules
/// </summary>
public interface IAlertSuppressionRuleRepository
{
    /// <summary>
    /// Creates a new suppression rule
    /// </summary>
    Task CreateAsync(AlertSuppressionRule rule, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets a suppression rule by ID
    /// </summary>
    Task<AlertSuppressionRule?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets all suppression rules with optional filtering
    /// </summary>
    Task<List<AlertSuppressionRule>> GetAllAsync(bool? isActive = null, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets only active (non-expired) suppression rules
    /// </summary>
    Task<List<AlertSuppressionRule>> GetActiveRulesAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Updates a suppression rule
    /// </summary>
    Task UpdateAsync(string id, Dictionary<string, object> updates, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Deletes a suppression rule
    /// </summary>
    Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Increments the usage count for a rule
    /// </summary>
    Task IncrementRuleUsageAsync(string id, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Deletes expired suppression rules
    /// </summary>
    Task<int> DeleteExpiredRulesAsync(CancellationToken cancellationToken = default);
}