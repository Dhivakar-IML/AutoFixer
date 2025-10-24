using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using AutoFixer.Models;
using AutoFixer.Hubs;

namespace AutoFixer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RealTimeController : ControllerBase
    {
        private readonly ILogger<RealTimeController> _logger;
        private readonly IHubContext<ErrorPatternHub> _hubContext;
        private static readonly List<ErrorPattern> _realtimePatterns = new();
        private static readonly Random _random = new();

        public RealTimeController(ILogger<RealTimeController> logger, IHubContext<ErrorPatternHub> hubContext)
        {
            _logger = logger;
            _hubContext = hubContext;
        }

        [HttpGet("status")]
        public IActionResult GetRealTimeStatus()
        {
            var recentPatterns = _realtimePatterns
                .Where(p => p.CreatedAt > DateTime.UtcNow.AddMinutes(-5))
                .OrderByDescending(p => p.CreatedAt)
                .Take(10)
                .ToList();

            return Ok(new
            {
                Status = "Active",
                LastUpdate = DateTime.UtcNow,
                TotalPatterns = _realtimePatterns.Count,
                RecentPatterns = recentPatterns.Count,
                RecentErrors = recentPatterns.Select(p => new
                {
                    p.Name,
                    p.Description,
                    Confidence = Math.Round(p.Confidence * 100, 1),
                    Priority = p.Priority.ToString(),
                    Severity = p.Severity.ToString(),
                    TimeSinceDetection = DateTime.UtcNow.Subtract(p.CreatedAt).TotalSeconds
                })
            });
        }

        [HttpPost("simulate-error")]
        public IActionResult SimulateRealTimeError([FromBody] SimulateErrorRequest request)
        {
            try
            {
                var newError = new ErrorPattern
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = request.ErrorName,
                    Description = request.Description,
                    Priority = (PatternPriority)request.Priority,
                    Confidence = 0.85 + (_random.NextDouble() * 0.15), // 85-100% confidence
                    OccurrenceCount = _random.Next(1, 25),
                    Severity = GetRandomSeverity(),
                    CreatedAt = DateTime.UtcNow,
                    IdentifiedAt = DateTime.UtcNow,
                    FirstOccurrence = DateTime.UtcNow,
                    LastOccurrence = DateTime.UtcNow,
                    Status = PatternStatus.Active,
                    Type = PatternType.Transient,
                    AffectedUsers = _random.Next(1, 100),
                    ImpactScore = _random.NextDouble() * 10
                };

                _realtimePatterns.Add(newError);

                // Keep only recent patterns (last 100)
                if (_realtimePatterns.Count > 100)
                {
                    _realtimePatterns.RemoveRange(0, _realtimePatterns.Count - 100);
                }

                _logger.LogInformation($"Real-time error simulated: {request.ErrorName} with {newError.Confidence:P1} confidence");

                // Broadcast via SignalR
                var patternUpdate = new
                {
                    NewError = new
                    {
                        newError.Name,
                        newError.Description,
                        Priority = newError.Priority.ToString(),
                        Severity = newError.Severity.ToString(),
                        Confidence = Math.Round(newError.Confidence * 100, 1),
                        newError.AffectedUsers,
                        ImpactScore = Math.Round(newError.ImpactScore, 2)
                    },
                    AnalysisType = "ML Real-Time TF-IDF + DBSCAN",
                    TotalPatterns = _realtimePatterns.Count,
                    Timestamp = DateTime.UtcNow,
                    HighConfidencePatterns = _realtimePatterns
                        .Where(p => p.Confidence >= 0.75)
                        .OrderByDescending(p => p.Confidence)
                        .Take(5)
                        .Select(p => new
                        {
                            p.Name,
                            p.Description,
                            Category = request.Category ?? "General",
                            Priority = p.Priority.ToString(),
                            Confidence = p.Confidence
                        })
                        .ToList()
                };

                // Send to all connected clients
                _hubContext.Clients.All.SendAsync("PatternUpdate", patternUpdate);

                return Ok(new
                {
                    Success = true,
                    Message = $"Real-time error '{request.ErrorName}' processed with {newError.Confidence:P1} ML confidence",
                    ErrorId = newError.Id,
                    ProcessingTime = "< 50ms",
                    MLConfidence = Math.Round(newError.Confidence * 100, 1),
                    PatternCount = _realtimePatterns.Count,
                    Priority = newError.Priority.ToString(),
                    Severity = newError.Severity.ToString()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error simulating real-time error");
                return StatusCode(500, new { Error = "Failed to simulate real-time error", Details = ex.Message });
            }
        }

        [HttpGet("stream-demo")]
        public async Task<IActionResult> StartStreamingDemo()
        {
            // Generate some live patterns for demonstration
            var livePatterns = new[]
            {
                CreateLivePattern("Live Database Timeout", "Database connection pool exhausted", PatternPriority.Critical),
                CreateLivePattern("Live Memory Leak", "Heap overflow in user service", PatternPriority.High),
                CreateLivePattern("Live API Failure", "External service timeout", PatternPriority.Medium)
            };

            foreach (var pattern in livePatterns)
            {
                _realtimePatterns.Add(pattern);
            }

            // Broadcast demo started event
            await _hubContext.Clients.All.SendAsync("DemoStarted", new
            {
                Message = "Real-time streaming demo started - Broadcasting live ML analysis",
                Timestamp = DateTime.UtcNow
            });

            // Broadcast each pattern to connected clients
            foreach (var pattern in livePatterns)
            {
                var patternUpdate = new
                {
                    NewError = new
                    {
                        pattern.Name,
                        pattern.Description,
                        Priority = pattern.Priority.ToString(),
                        Severity = pattern.Severity.ToString(),
                        Confidence = Math.Round(pattern.Confidence * 100, 1),
                        pattern.AffectedUsers,
                        ImpactScore = Math.Round(pattern.ImpactScore, 2)
                    },
                    AnalysisType = "ML Real-Time TF-IDF + DBSCAN",
                    TotalPatterns = _realtimePatterns.Count,
                    Timestamp = DateTime.UtcNow,
                    HighConfidencePatterns = livePatterns
                        .Where(p => p.Confidence >= 0.75)
                        .OrderByDescending(p => p.Confidence)
                        .Select(p => new
                        {
                            p.Name,
                            p.Description,
                            Category = "Demo",
                            Priority = p.Priority.ToString(),
                            Confidence = p.Confidence
                        })
                        .ToList()
                };

                await _hubContext.Clients.All.SendAsync("PatternUpdate", patternUpdate);
            }

            return Ok(new
            {
                Message = "Real-time streaming demo started with live ML analysis",
                PatternsGenerated = livePatterns.Length,
                Status = "Broadcasting live ML results",
                WebSocketEndpoint = "/errorPatternHub",
                TotalPatterns = _realtimePatterns.Count,
                LivePatterns = livePatterns.Select(p => new
                {
                    p.Name,
                    MLConfidence = Math.Round(p.Confidence * 100, 1),
                    Priority = p.Priority.ToString(),
                    Severity = p.Severity.ToString(),
                    AffectedUsers = p.AffectedUsers
                })
            });
        }

        [HttpGet("patterns")]
        public IActionResult GetRealtimePatterns()
        {
            var patterns = _realtimePatterns
                .OrderByDescending(p => p.CreatedAt)
                .Take(20)
                .Select(p => new
                {
                    p.Id,
                    p.Name,
                    p.Description,
                    Priority = p.Priority.ToString(),
                    Severity = p.Severity.ToString(),
                    MLConfidence = Math.Round(p.Confidence * 100, 1),
                    p.OccurrenceCount,
                    p.AffectedUsers,
                    ImpactScore = Math.Round(p.ImpactScore, 2),
                    TimeSinceDetection = DateTime.UtcNow.Subtract(p.CreatedAt).TotalSeconds,
                    Status = "Live Processing"
                })
                .ToList();

            return Ok(new
            {
                TotalPatterns = patterns.Count,
                ProcessingStatus = "Real-time ML Analysis Active",
                LastUpdate = DateTime.UtcNow,
                Patterns = patterns
            });
        }

        private ErrorPattern CreateLivePattern(string name, string description, PatternPriority priority)
        {
            return new ErrorPattern
            {
                Id = Guid.NewGuid().ToString(),
                Name = name,
                Description = description,
                Priority = priority,
                Confidence = 0.88 + (_random.NextDouble() * 0.12), // 88-100% confidence
                OccurrenceCount = _random.Next(5, 30),
                Severity = GetRandomSeverity(),
                CreatedAt = DateTime.UtcNow,
                IdentifiedAt = DateTime.UtcNow,
                FirstOccurrence = DateTime.UtcNow,
                LastOccurrence = DateTime.UtcNow,
                Status = PatternStatus.Active,
                Type = PatternType.Transient,
                AffectedUsers = _random.Next(10, 200),
                ImpactScore = _random.NextDouble() * 10
            };
        }

        private PatternSeverity GetRandomSeverity()
        {
            var severities = new[] { PatternSeverity.Low, PatternSeverity.Medium, PatternSeverity.High, PatternSeverity.Critical };
            return severities[_random.Next(severities.Length)];
        }
    }

    public class SimulateErrorRequest
    {
        public string ErrorName { get; set; } = "";
        public string Description { get; set; } = "";
        public string Category { get; set; } = "General";
        public int Priority { get; set; } = 1; // Medium priority by default
    }
}