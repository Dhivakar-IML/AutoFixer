// MongoDB initialization script for AutoFixer
print('AutoFixer: Creating database and collections...');

// Switch to AutoFixer database
db = db.getSiblingDB('AutoFixer');

// Create collections with proper indexes
print('AutoFixer: Creating PatternAlerts collection...');
db.createCollection('PatternAlerts');
db.PatternAlerts.createIndex({ "PatternId": 1 });
db.PatternAlerts.createIndex({ "Severity": 1 });
db.PatternAlerts.createIndex({ "Status": 1 });
db.PatternAlerts.createIndex({ "CreatedAt": -1 });
db.PatternAlerts.createIndex({ "ResolvedAt": 1 });

print('AutoFixer: Creating AlertSuppressionRules collection...');
db.createCollection('AlertSuppressionRules');
db.AlertSuppressionRules.createIndex({ "PatternId": 1 });
db.AlertSuppressionRules.createIndex({ "IsActive": 1 });
db.AlertSuppressionRules.createIndex({ "ExpiresAt": 1 });

print('AutoFixer: Creating ErrorEntries collection...');
db.createCollection('ErrorEntries');
db.ErrorEntries.createIndex({ "Timestamp": -1 });
db.ErrorEntries.createIndex({ "Application": 1 });
db.ErrorEntries.createIndex({ "Severity": 1 });
db.ErrorEntries.createIndex({ "Message": "text" });

print('AutoFixer: Creating ErrorClusters collection...');
db.createCollection('ErrorClusters');
db.ErrorClusters.createIndex({ "CreatedAt": -1 });
db.ErrorClusters.createIndex({ "PatternId": 1 });
db.ErrorClusters.createIndex({ "IsActive": 1 });

print('AutoFixer: Creating ErrorPatterns collection...');
db.createCollection('ErrorPatterns');
db.ErrorPatterns.createIndex({ "Id": 1 }, { unique: true });
db.ErrorPatterns.createIndex({ "Category": 1 });
db.ErrorPatterns.createIndex({ "Severity": 1 });
db.ErrorPatterns.createIndex({ "IsEnabled": 1 });

print('AutoFixer: Creating RootCauseAnalyses collection...');
db.createCollection('RootCauseAnalyses');
db.RootCauseAnalyses.createIndex({ "PatternId": 1 });
db.RootCauseAnalyses.createIndex({ "CreatedAt": -1 });
db.RootCauseAnalyses.createIndex({ "Confidence": -1 });

print('AutoFixer: Creating PatternResolutions collection...');
db.createCollection('PatternResolutions');
db.PatternResolutions.createIndex({ "PatternId": 1 });
db.PatternResolutions.createIndex({ "CreatedAt": -1 });
db.PatternResolutions.createIndex({ "IsVerified": 1 });

// Insert some sample error patterns
print('AutoFixer: Inserting sample error patterns...');
db.ErrorPatterns.insertMany([
  {
    Id: "MEMORY_LEAK",
    Name: "Memory Leak Pattern",
    Pattern: "OutOfMemoryException|memory leak|heap overflow",
    Severity: "High",
    Category: "Performance",
    IsEnabled: true,
    Description: "Detects potential memory leak patterns in application logs",
    CreatedAt: new Date(),
    UpdatedAt: new Date()
  },
  {
    Id: "SQL_INJECTION",
    Name: "SQL Injection Pattern",
    Pattern: "SQL injection|SqlException.*'|union select|drop table",
    Severity: "Critical",
    Category: "Security",
    IsEnabled: true,
    Description: "Detects potential SQL injection attempts",
    CreatedAt: new Date(),
    UpdatedAt: new Date()
  },
  {
    Id: "AUTHENTICATION_FAILURE",
    Name: "Authentication Failure",
    Pattern: "authentication failed|invalid credentials|unauthorized access",
    Severity: "Medium",
    Category: "Security",
    IsEnabled: true,
    Description: "Detects authentication and authorization failures",
    CreatedAt: new Date(),
    UpdatedAt: new Date()
  },
  {
    Id: "NULL_REFERENCE",
    Name: "Null Reference Exception",
    Pattern: "NullReferenceException|Object reference not set",
    Severity: "Medium",
    Category: "Code Quality",
    IsEnabled: true,
    Description: "Detects null reference exceptions in the application",
    CreatedAt: new Date(),
    UpdatedAt: new Date()
  },
  {
    Id: "TIMEOUT_ERROR",
    Name: "Timeout Error",
    Pattern: "timeout|request timed out|operation timeout",
    Severity: "Medium",
    Category: "Performance",
    IsEnabled: true,
    Description: "Detects timeout-related errors",
    CreatedAt: new Date(),
    UpdatedAt: new Date()
  }
]);

print('AutoFixer: Database initialization complete!');