using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AutoFixer.Models;

/// <summary>
/// Represents an individual error entry ingested from various sources (Seq, New Relic, MongoDB audit logs)
/// </summary>
public class ErrorEntry
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    [BsonElement("timestamp")]
    public DateTime Timestamp { get; set; }

    [BsonElement("message")]
    public string Message { get; set; } = string.Empty;

    [BsonElement("stackTrace")]
    public string? StackTrace { get; set; }

    [BsonElement("exceptionType")]
    public string? ExceptionType { get; set; }

    [BsonElement("source")]
    public string Source { get; set; } = string.Empty; // Seq, NewRelic, MongoDB

    [BsonElement("context")]
    public Dictionary<string, object> Context { get; set; } = new();

    [BsonElement("userId")]
    public string? UserId { get; set; }

    [BsonElement("endpoint")]
    public string? Endpoint { get; set; }

    [BsonElement("statusCode")]
    public int? StatusCode { get; set; }

    [BsonElement("clusterId")]
    public string? ClusterId { get; set; }

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("normalizedMessage")]
    public string? NormalizedMessage { get; set; }

    [BsonElement("severity")]
    public ErrorSeverity Severity { get; set; } = ErrorSeverity.Info;
}

/// <summary>
/// Enumeration for error severity levels
/// </summary>
public enum ErrorSeverity
{
    Info = 0,
    Warning = 1,
    Critical = 2,
    Emergency = 3
}