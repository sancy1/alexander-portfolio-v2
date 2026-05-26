// File: AuthService.Application/Common/DatabaseSettings.cs
// Purpose: Database configuration settings for multi-cloud connection management
// Layer: Application

namespace AuthService.Application.Common;

public class DatabaseSettings
{
    public const string SectionName = "Database";
    
    public string ConnectionString { get; set; } = string.Empty;
    public int DefaultPoolSize { get; set; } = 20;
    public int MaxOverflow { get; set; } = 40;
    public int OverrideMaxRetries { get; set; } = 0;
}

public enum DatabaseProfile
{
    LocalDocker,
    ServerlessPooler,
    EnterpriseInstance,
    CloudProxy
}