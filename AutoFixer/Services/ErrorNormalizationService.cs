using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using AutoFixer.Models;
using AutoFixer.Services.Interfaces;

namespace AutoFixer.Services;

/// <summary>
/// Service for normalizing error messages and extracting features for ML processing
/// </summary>
public class ErrorNormalizationService : IErrorNormalizationService
{
    private readonly ILogger<ErrorNormalizationService> _logger;

    // Regex patterns for normalization
    private static readonly Regex GuidPattern = new(@"\b[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex IdPattern = new(@"\b[0-9a-f]{8,}\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex DatePattern = new(@"\b\d{4}-\d{2}-\d{2}(T\d{2}:\d{2}:\d{2}(\.\d{3})?Z?)?\b", RegexOptions.Compiled);
    private static readonly Regex NumberPattern = new(@"\b\d+\b", RegexOptions.Compiled);
    private static readonly Regex IpPattern = new(@"\b\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}\b", RegexOptions.Compiled);
    private static readonly Regex UrlPattern = new(@"https?://[^\s]+", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex FilePathPattern = new(@"[A-Za-z]:\\[^\\/:*?""<>|\r\n]+", RegexOptions.Compiled);
    private static readonly Regex JsonPattern = new(@"\{[^}]*\}", RegexOptions.Compiled);

    public ErrorNormalizationService(ILogger<ErrorNormalizationService> logger)
    {
        _logger = logger;
    }

    public string NormalizeError(string message, string? stackTrace)
    {
        if (string.IsNullOrEmpty(message))
            return string.Empty;

        var normalized = message;

        try
        {
            // Remove dynamic values that vary between occurrences but don't affect the error pattern
            normalized = GuidPattern.Replace(normalized, "[GUID]");
            normalized = IdPattern.Replace(normalized, "[ID]");
            normalized = DatePattern.Replace(normalized, "[DATE]");
            normalized = NumberPattern.Replace(normalized, "[NUM]");
            normalized = IpPattern.Replace(normalized, "[IP]");
            normalized = UrlPattern.Replace(normalized, "[URL]");
            normalized = FilePathPattern.Replace(normalized, "[PATH]");
            normalized = JsonPattern.Replace(normalized, "[JSON]");

            // Normalize whitespace
            normalized = Regex.Replace(normalized, @"\s+", " ");
            normalized = normalized.Trim();

            // Extract and append key stack trace frames
            var keyFrames = ExtractKeyStackFrames(stackTrace);
            if (keyFrames.Any())
            {
                normalized += " " + string.Join(" ", keyFrames);
            }

            // Convert to lowercase for consistency
            normalized = normalized.ToLowerInvariant();

            _logger.LogDebug("Normalized error: {Original} -> {Normalized}", message, normalized);
            return normalized;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error normalizing message: {Message}", message);
            return message.ToLowerInvariant();
        }
    }

    public List<string> ExtractKeyStackFrames(string? stackTrace)
    {
        if (string.IsNullOrEmpty(stackTrace))
            return new List<string>();

        try
        {
            var frames = stackTrace.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Select(line => line.Trim())
                .Where(line => !string.IsNullOrEmpty(line))
                .ToList();

            var keyFrames = new List<string>();

            foreach (var frame in frames.Take(10)) // Only look at top 10 frames
            {
                // Look for application-specific frames (exclude framework code)
                if (IsApplicationFrame(frame))
                {
                    var normalizedFrame = NormalizeStackFrame(frame);
                    if (!string.IsNullOrEmpty(normalizedFrame))
                    {
                        keyFrames.Add(normalizedFrame);
                    }
                }

                // Stop after finding 5 key frames
                if (keyFrames.Count >= 5)
                    break;
            }

            return keyFrames;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting stack frames from: {StackTrace}", stackTrace);
            return new List<string>();
        }
    }

    public string GeneratePatternSignature(string normalizedError)
    {
        if (string.IsNullOrEmpty(normalizedError))
            return string.Empty;

        try
        {
            using var sha256 = SHA256.Create();
            var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(normalizedError));
            return Convert.ToBase64String(hashBytes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating pattern signature for: {NormalizedError}", normalizedError);
            return normalizedError.GetHashCode().ToString();
        }
    }

    public ErrorFeatures ExtractFeatures(ErrorEntry error)
    {
        try
        {
            var normalizedMessage = NormalizeError(error.Message, error.StackTrace);
            
            return new ErrorFeatures
            {
                ErrorText = normalizedMessage,
                ExceptionType = error.ExceptionType ?? "Unknown",
                Source = error.Source,
                Endpoint = error.Endpoint,
                StatusCode = error.StatusCode ?? 0,
                Timestamp = error.Timestamp
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting features from error: {ErrorId}", error.Id);
            return new ErrorFeatures
            {
                ErrorText = error.Message,
                ExceptionType = "Unknown",
                Source = error.Source,
                Timestamp = error.Timestamp
            };
        }
    }

    private bool IsApplicationFrame(string frame)
    {
        // Identify application-specific stack frames vs framework code
        var applicationPatterns = new[]
        {
            "Registration.",
            "AutoFixer.",
            "YourApp.", // Replace with actual application namespaces
            "MyCompany."
        };

        var frameworkPatterns = new[]
        {
            "System.",
            "Microsoft.",
            "Newtonsoft.",
            "MongoDB.",
            "at lambda_method",
            "at System.Runtime.",
            "--- End of stack trace"
        };

        // Prioritize application frames
        if (applicationPatterns.Any(pattern => frame.Contains(pattern, StringComparison.OrdinalIgnoreCase)))
            return true;

        // Exclude known framework frames
        if (frameworkPatterns.Any(pattern => frame.Contains(pattern, StringComparison.OrdinalIgnoreCase)))
            return false;

        // Include other frames that might be relevant
        return true;
    }

    private string NormalizeStackFrame(string frame)
    {
        try
        {
            // Extract method signature without line numbers and file paths
            var normalized = frame;

            // Remove "at " prefix
            if (normalized.StartsWith("at "))
                normalized = normalized.Substring(3);

            // Remove line numbers and file info (everything after " in ")
            var inIndex = normalized.IndexOf(" in ");
            if (inIndex > 0)
                normalized = normalized.Substring(0, inIndex);

            // Remove parameter details (keep method signature but remove parameter types)
            normalized = Regex.Replace(normalized, @"\([^)]*\)", "()");

            // Remove generic type parameters
            normalized = Regex.Replace(normalized, @"`\d+", "");

            return normalized.Trim();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error normalizing stack frame: {Frame}", frame);
            return frame;
        }
    }
}