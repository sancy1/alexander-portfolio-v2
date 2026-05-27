// File: AuthService.API/Program.cs
// Purpose: Application entry point with database resilience and Swagger
// Layer: API

// New chnages

using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using System.Reflection;
using AuthService.Application.Common;
using AuthService.Infrastructure.Persistence;
using DotNetEnv;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using AuthService.Application.Features.Admin.Commands;
using AuthService.Application.Validators;
using AuthService.Application.Interfaces.Persistence;
using AuthService.Infrastructure.Persistence.Repositories;
using AuthService.Infrastructure.Security;
using AuthService.Domain.Interfaces;
using AuthService.Application.Interfaces.Security;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using AuthService.Application.Interfaces.Services;

using AuthService.Infrastructure.Messaging.RabbitMQ;
using AuthService.Infrastructure.Messaging.Kafka;
using AuthService.Application.Interfaces.Messaging;

using AuthService.Infrastructure.Caching;
using AuthService.API.Middleware;
using AuthService.Infrastructure.Services;



// Load .env file from current directory
Env.Load();

// After Env.Load();
var testDbUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
Console.WriteLine($"DEBUG: DATABASE_URL = {testDbUrl}");

var builder = WebApplication.CreateBuilder(args);

// Add configuration from environment variables
builder.Configuration.AddEnvironmentVariables();

// Add services to container
builder.Services.AddControllers();

builder.Services.AddScoped<ChangePasswordValidator>();
builder.Services.AddScoped<ResetPasswordValidator>();

// Register Outbox Repository and Processor
builder.Services.AddScoped<IOutboxRepository, OutboxRepository>();
builder.Services.AddScoped<IOutboxProcessorService, OutboxProcessorService>();

// Register Outbox Repository and Processor
builder.Services.AddScoped<AuthService.Application.Interfaces.Services.IOutboxProcessorService, AuthService.Infrastructure.Services.OutboxProcessorService>();

// ============================================================================
// JWT AUTHENTICATION
// ============================================================================
var jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET");
if (string.IsNullOrEmpty(jwtSecret))
{
    throw new Exception("JWT_SECRET not found in .env file");
}

var key = Encoding.ASCII.GetBytes(jwtSecret);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = true,
        ValidIssuer = Environment.GetEnvironmentVariable("JWT_ISSUER") ?? "auth-service",
        ValidateAudience = true,
        ValidAudience = Environment.GetEnvironmentVariable("JWT_AUDIENCE") ?? "portfolio-api",
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

// ============================================================================
// LOAD JWT SETTINGS FROM .ENV (ONCE - NO DUPLICATE jwtSecret HERE)
// ============================================================================
var jwtSettings = new JwtSettings
{
    Secret = jwtSecret,  // Use the existing jwtSecret variable
    Issuer = Environment.GetEnvironmentVariable("JWT_ISSUER") ?? "auth-service",
    Audience = Environment.GetEnvironmentVariable("JWT_AUDIENCE") ?? "portfolio-api",
    ExpiryMinutes = int.Parse(Environment.GetEnvironmentVariable("JWT_EXPIRY_MINUTES") ?? "60")
};
builder.Services.Configure<JwtSettings>(options =>
{
    options.Secret = jwtSettings.Secret;
    options.Issuer = jwtSettings.Issuer;
    options.Audience = jwtSettings.Audience;
    options.ExpiryMinutes = jwtSettings.ExpiryMinutes;
});
Console.WriteLine($"JWT Settings loaded - Issuer: {jwtSettings.Issuer}, Expiry: {jwtSettings.ExpiryMinutes} mins");

// ============================================================================
// LOAD ADMIN KEY SETTINGS FROM .ENV
// ============================================================================
var adminKeySettings = new AdminKeySettings
{
    MasterKey = Environment.GetEnvironmentVariable("ADMIN_MASTER_KEY") ?? "SUPER_SECRET_ADMIN_KEY_2024",
    RequireForRegistration = bool.Parse(Environment.GetEnvironmentVariable("ADMIN_KEY_REQUIRE_FOR_REGISTRATION") ?? "true"),
    AllowedKeys = (Environment.GetEnvironmentVariable("ADMIN_ALLOWED_KEYS") ?? "SUPER_SECRET_ADMIN_KEY_2024")
        .Split(',', StringSplitOptions.RemoveEmptyEntries)
        .ToList()
};
builder.Services.Configure<AdminKeySettings>(options =>
{
    options.MasterKey = adminKeySettings.MasterKey;
    options.RequireForRegistration = adminKeySettings.RequireForRegistration;
    options.AllowedKeys = adminKeySettings.AllowedKeys;
});
Console.WriteLine($"Admin Key Settings loaded - MasterKey exists: {!string.IsNullOrEmpty(adminKeySettings.MasterKey)}");

// Add after other settings
var cloudinarySettings = new CloudinarySettings
{
    CloudName = Environment.GetEnvironmentVariable("CLOUDINARY_CLOUD_NAME") ?? "",
    ApiKey = Environment.GetEnvironmentVariable("CLOUDINARY_API_KEY") ?? "",
    ApiSecret = Environment.GetEnvironmentVariable("CLOUDINARY_API_SECRET") ?? ""
};
builder.Services.Configure<CloudinarySettings>(options =>
{
    options.CloudName = cloudinarySettings.CloudName;
    options.ApiKey = cloudinarySettings.ApiKey;
    options.ApiSecret = cloudinarySettings.ApiSecret;
});

builder.Services.AddScoped<AuthService.Application.Interfaces.Services.ICloudinaryService, AuthService.Infrastructure.Services.CloudinaryService>();

// Register validators
builder.Services.AddScoped<AdminRegisterValidator>();
builder.Services.AddScoped<AdminLoginValidator>();  

// Register repositories
builder.Services.AddScoped<IAdminRepository, AdminRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// Register security services
builder.Services.AddSingleton<IPasswordHasher, PasswordHasher>();
builder.Services.AddSingleton<AuthService.Application.Interfaces.Security.IAdminKeyValidator, AuthService.Infrastructure.Security.AdminKeyValidator>();
builder.Services.AddSingleton<IJwtGenerator, JwtGenerator>();

// Register Token Blacklist Service
builder.Services.AddSingleton<AuthService.Application.Interfaces.Security.ITokenBlacklistService, AuthService.Infrastructure.Caching.TokenBlacklistService>();

// Register MediatR
builder.Services.AddMediatR(cfg => {
    cfg.RegisterServicesFromAssembly(typeof(RegisterAdminHandler).Assembly);
});

builder.Services.AddEndpointsApiExplorer();

// Swagger Configuration with XML documentation
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "AuthService API",
        Version = "v1",
        Description = "Authentication and Authorization Microservice for Alexander Portfolio",
        Contact = new OpenApiContact
        {
            Name = "Alexander Portfolio Team",
            Email = "support@alexanderportfolio.com"
        }
    });
    
    // Include XML comments for better documentation
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }
    
    // Add JWT authentication to Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter 'Bearer' followed by your JWT token"
    });
    
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });


});

// Database Configuration - Read from .env
var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL")
    ?? builder.Configuration.GetValue<string>("Database:ConnectionString")
    ?? throw new Exception("DATABASE_URL not found in .env file or appsettings.json");

Console.WriteLine($"DATABASE_URL loaded (length: {databaseUrl.Length})");

builder.Services.Configure<DatabaseSettings>(options =>
{
    options.ConnectionString = databaseUrl;
    options.DefaultPoolSize = 5;
    options.MaxOverflow = 10;
    options.OverrideMaxRetries = 6;
});


// Add after loading other settings
var googleClientId = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_ID");
var googleClientSecret = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_SECRET");
var githubClientId = Environment.GetEnvironmentVariable("GITHUB_CLIENT_ID");
var githubClientSecret = Environment.GetEnvironmentVariable("GITHUB_CLIENT_SECRET");

if (string.IsNullOrEmpty(googleClientId) || string.IsNullOrEmpty(githubClientId))
{
    Console.WriteLine("Warning: OAuth credentials not configured. Social login will not work.");
}

// Add HTTP Client for OAuth calls
builder.Services.AddHttpClient();

// Register SocialUser repository
builder.Services.AddScoped<ISocialUserRepository, SocialUserRepository>();

// Register database services
builder.Services.AddSingleton<IDatabaseConnectionManager, DatabaseConnectionManager>();
builder.Services.AddTransient<IDatabaseStartupVerifier, DatabaseStartupVerifier>();

// Configure DbContext with dynamic connection string
builder.Services.AddDbContext<AppDbContext>((serviceProvider, options) =>
{
    var connectionManager = serviceProvider.GetRequiredService<IDatabaseConnectionManager>();
    var connectionString = connectionManager.BuildOptimizedConnectionString();
    
    Console.WriteLine($"Connecting to database with optimized connection string");
    
    options.UseNpgsql(connectionString, npgsqlOptions =>
    {
        npgsqlOptions.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(5),
            errorCodesToAdd: null);
    });
});


// Health Checks
builder.Services.AddHealthChecks()
    .AddNpgSql(databaseUrl, name: "database", tags: new[] { "ready", "live" })
    .AddDbContextCheck<AppDbContext>("dbcontext", tags: new[] { "ready" });

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});



// ============================================================================
// REDIS CACHE SETUP (For Token Blacklisting & Rate Limiting)
// ============================================================================

// ============================================================================
// REDIS CACHE SETUP
// ============================================================================
var redisConnection = Environment.GetEnvironmentVariable("REDIS_CONNECTION");
var redisHost = Environment.GetEnvironmentVariable("REDIS_HOST");
var redisPort = Environment.GetEnvironmentVariable("REDIS_PORT") ?? "23851";
var redisPassword = Environment.GetEnvironmentVariable("REDIS_PASSWORD");
var redisUser = Environment.GetEnvironmentVariable("REDIS_USER") ?? "default";

// Build StackExchange-compatible connection string from parts
// (StackExchangeRedisCache doesn't parse rediss:// URL format natively)
string redisConfigString;
if (!string.IsNullOrEmpty(redisHost) && !string.IsNullOrEmpty(redisPassword))
{
    redisConfigString = $"{redisHost}:{redisPort},password={redisPassword},ssl=True,abortConnect=False,connectTimeout=10000,syncTimeout=10000";
    Console.WriteLine($"Redis configured via host parts: {redisHost}:{redisPort}");
}
else if (!string.IsNullOrEmpty(redisConnection))
{
    // Fallback: try to parse rediss:// URL manually
    var uri = new Uri(redisConnection);
    var host = uri.Host;
    var port = uri.Port;
    var password = Uri.UnescapeDataString(uri.UserInfo.Split(':').Last());
    redisConfigString = $"{host}:{port},password={password},ssl=True,abortConnect=False,connectTimeout=10000,syncTimeout=10000";
    Console.WriteLine($"Redis configured via URL parsing: {host}:{port}");
}
else
{
    redisConfigString = "localhost:6379,abortConnect=False";
    Console.WriteLine("WARNING: Redis not configured, using localhost fallback");
}

builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = redisConfigString;
    options.InstanceName = "AuthService_";
});

// // Register Redis distributed cache
// builder.Services.AddStackExchangeRedisCache(options =>
// {
//     options.Configuration = Environment.GetEnvironmentVariable("REDIS_CONNECTION") ?? "localhost:6379";
//     options.InstanceName = "AuthService_";
// });


// ============================================================================
// MESSAGING SERVICES (RabbitMQ & Kafka)
// ============================================================================

// RabbitMQ Configuration
var rabbitMQSettings = new RabbitMQSettings
{
    HostName = Environment.GetEnvironmentVariable("RABBITMQ_HOST") ?? "localhost",
    Port = int.Parse(Environment.GetEnvironmentVariable("RABBITMQ_PORT") ?? "5672"),
    UserName = Environment.GetEnvironmentVariable("RABBITMQ_USER") ?? "guest",
    Password = Environment.GetEnvironmentVariable("RABBITMQ_PASSWORD") ?? "guest"
};
builder.Services.Configure<RabbitMQSettings>(options =>
{
    options.HostName = rabbitMQSettings.HostName;
    options.Port = rabbitMQSettings.Port;
    options.UserName = rabbitMQSettings.UserName;
    options.Password = rabbitMQSettings.Password;
});

// Kafka Configuration
var kafkaSettings = new KafkaSettings
{
    BootstrapServers = Environment.GetEnvironmentVariable("KAFKA_BOOTSTRAP_SERVERS") ?? "localhost:9092"
};
builder.Services.Configure<KafkaSettings>(options =>
{
    options.BootstrapServers = kafkaSettings.BootstrapServers;
});

// Register messaging services
builder.Services.AddSingleton<IMessagePublisher, RabbitMQPublisher>();
builder.Services.AddSingleton<IKafkaProducer, KafkaProducer>();
// builder.Services.AddHostedService<RabbitMQSubscriber>();
// builder.Services.AddHostedService<KafkaConsumer>();





var app = builder.Build();

// Database Startup Verification
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    
    try
    {
        var dbContext = services.GetRequiredService<AppDbContext>();
        var startupVerifier = services.GetRequiredService<IDatabaseStartupVerifier>();
        
        logger.LogInformation("Verifying Neon database connection...");
        var isDbReady = await startupVerifier.VerifyAndWakeDatabaseAsync(dbContext);
        
        if (!isDbReady)
        {
            logger.LogCritical("Neon database connection failed. Application shutting down.");
            Environment.Exit(1);
        }
        
        logger.LogInformation("Neon database connection verified.");
    }
    catch (Exception ex)
    {
        logger.LogCritical(ex, "Database initialization failed. Application shutting down.");
        Environment.Exit(1);
    }
}


// Configure pipeline
// Always enable Swagger (including production)
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Auth Service API v1");
    c.RoutePrefix = "swagger";
});

// Optional: Add security for Swagger in production
if (!app.Environment.IsDevelopment())
{
    // You can add basic auth or disable completely if concerned
    // For now, let's keep it accessible for testing
}

// app.UseSwagger();
// app.UseSwaggerUI(c =>
// {
//     c.SwaggerEndpoint("/swagger/v1/swagger.json", "AuthService API v1");
//     c.RoutePrefix = "swagger";
// });


app.UseHttpsRedirection();
app.UseCors("AllowAll");

// IMPORTANT: Authentication MUST come before Authorization
app.UseMiddleware<GatewaySecretMiddleware>(); 
app.UseAuthentication();
app.UseMiddleware<JwtBlacklistMiddleware>();
app.UseAuthorization();

app.MapControllers();

// Map health checks
app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var result = new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                description = e.Value.Description
            }),
            timestamp = DateTime.UtcNow
        };
        await context.Response.WriteAsJsonAsync(result);
    }
});

app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("live")
});

app.Run();