using System.Text;
using System.Threading.RateLimiting;
using Kalkimo.Api.Infrastructure;
using Kalkimo.Api.Services;
using Kalkimo.Domain.Calculators;
using Kalkimo.Domain.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// ===========================================
// Services konfigurieren
// ===========================================

// Controller
builder.Services.AddControllers();

// Rate Limiting for auth endpoints (brute force protection)
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // Global limiter - prevents DoS
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 10
            }));

    // Strict limiter for auth endpoints
    options.AddPolicy("auth", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 2
            }));
});

// Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Kalkimo Planner API",
        Version = "v1",
        Description = "API für den Kalkimo Immobilien-Investitionsrechner"
    });

    // JWT Bearer Auth in Swagger UI
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "JWT Authorization header. Beispiel: 'Bearer {token}'"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
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

// JWT Authentication
var jwtKey = builder.Configuration["Jwt:Key"];
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "Kalkimo";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "KalkimoApp";

// Validate JWT key is configured and has minimum length
if (string.IsNullOrEmpty(jwtKey))
{
    throw new InvalidOperationException(
        "JWT signing key not configured. Set 'Jwt:Key' in appsettings.json or environment variables.");
}
if (jwtKey.Length < 32)
{
    throw new InvalidOperationException(
        "JWT signing key must be at least 32 characters long for security.");
}

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.FromMinutes(5)
        };
    });

builder.Services.AddAuthorization();

// CORS (für Frontend-Entwicklung)
builder.Services.AddCors(options =>
{
    options.AddPolicy("Development", policy =>
    {
        policy.WithOrigins(
                  "http://localhost:8100",  // Ionic serve
                  "http://localhost:3000",  // Alternative dev server
                  "http://localhost:5173",  // Vite default
                  "http://localhost:5174",  // Vite alternative
                  "http://localhost:5175")  // Vite alternative
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Infrastructure Services
var dataRoot = builder.Configuration["DataRoot"] ?? Path.Combine(Directory.GetCurrentDirectory(), "data");
Directory.CreateDirectory(dataRoot);

// SECURITY: LocalDevEncryptionService is only for development
// In production, use a proper encryption service with persistent keys
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddSingleton<IEncryptionService, LocalDevEncryptionService>();
}
else
{
    // Production requires a proper encryption service
    // TODO: Implement production encryption service with Azure Key Vault, AWS KMS, or similar
    throw new InvalidOperationException(
        "Production encryption service not configured. " +
        "LocalDevEncryptionService is not suitable for production use. " +
        "Implement IEncryptionService with proper key management.");
}

builder.Services.AddSingleton<IProjectStore>(sp =>
    new FlatfileProjectStore(dataRoot, sp.GetRequiredService<IEncryptionService>()));

builder.Services.AddSingleton<IAuthStore>(sp =>
    new FlatfileAuthStore(dataRoot, sp.GetRequiredService<IEncryptionService>()));

// Domain Services
builder.Services.AddSingleton<IDateProvider, SystemDateProvider>();
builder.Services.AddTransient<TaxCalculator>();
builder.Services.AddTransient<CalculationOrchestrator>();

// API Services
builder.Services.AddSingleton<JsonPatchApplier>();

// ===========================================
// App konfigurieren
// ===========================================

var app = builder.Build();

// Swagger (nur Development)
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Kalkimo API v1");
        options.RoutePrefix = "swagger";
    });
}

// CORS
if (app.Environment.IsDevelopment())
{
    app.UseCors("Development");
}

// HSTS for production (tells browsers to always use HTTPS)
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseHttpsRedirection();

// Rate Limiting (must be before auth to protect login endpoints)
app.UseRateLimiter();

// Auth Middleware
app.UseAuthentication();
app.UseAuthorization();

// Controller Routing
app.MapControllers();

// Health Check Endpoint
app.MapGet("/health", () => Results.Ok(new
{
    status = "healthy",
    timestamp = DateTimeOffset.UtcNow,
    version = "1.0.0"
})).AllowAnonymous();

// API Info Endpoint
app.MapGet("/", () => Results.Ok(new
{
    name = "Kalkimo Planner API",
    version = "1.0.0",
    documentation = "/swagger"
})).AllowAnonymous();

app.Run();

// Partial class für WebApplicationFactory in Tests
public partial class Program { }
