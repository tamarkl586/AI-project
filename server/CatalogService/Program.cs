using CatalogService.BLL.Implementations;
using CatalogService.BLL.Interfaces;
using CatalogService.DAL;
using CatalogService.DAL.Implementations;
using CatalogService.DAL.Interfaces;
using CatalogService.Mapping;
using CatalogService.Middleware;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Unicode;

var builder = WebApplication.CreateBuilder(args);

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? new[]
    {
        "http://localhost:4200",
        "https://localhost:4200",
        "http://127.0.0.1:4200",
        "https://127.0.0.1:4200"
    };

// ==========================================
// 1. Serilog (early init so all startup errors are captured)
// ==========================================
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

builder.Host.UseSerilog();

// ==========================================
// 2. CORS
// ==========================================
builder.Services.AddCors(options =>
{
    options.AddPolicy("DevelopmentCors", policy =>
        policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod());
});

// ==========================================
// 3. Controllers
// ==========================================
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Encoder = JavaScriptEncoder.Create(UnicodeRanges.All);
    });

builder.Services.AddEndpointsApiExplorer();

// ==========================================
// 4. Swagger
// ==========================================
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "Catalog Service API", Version = "v1" });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT bearer token"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

// ==========================================
// 5. MongoDB (Singleton — MongoClient is thread-safe)
// ==========================================
builder.Services.AddSingleton<CatalogDbContext>();
builder.Services.AddSingleton<SequenceService>();

// ==========================================
// 6. DAL Registration
// ==========================================
builder.Services.AddScoped<IGiftDAL, GiftDAL>();
builder.Services.AddScoped<IDonorDAL, DonorDAL>();
builder.Services.AddScoped<ICategoryDAL, CategoryDAL>();

// ==========================================
// 7. BLL Registration
// ==========================================
builder.Services.AddScoped<IGiftService, GiftService>();
builder.Services.AddScoped<IDonorService, DonorService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();

// ==========================================
// 8. AutoMapper
// ==========================================
builder.Services.AddAutoMapper(cfg => cfg.AddProfile<CatalogMappingProfile>());

// ==========================================
// 9. JWT Authentication (validate only — tokens are issued by the Identity Service)
// ==========================================
var jwt = builder.Configuration.GetSection("Jwt");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt["Issuer"],
            ValidAudience = jwt["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Key"]!))
        };

        options.Events = new JwtBearerEvents
        {
            OnChallenge = context =>
            {
                context.HandleResponse();
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.ContentType = "application/json";
                return context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(new
                {
                    message = "אין הרשאה. יש להתחבר מחדש עם JWT תקין"
                }));
            }
        };
    });

builder.Services.AddAuthorization();

// ==========================================
// 10. Redis (IDistributedCache)
// ==========================================
var redisConnectionString = builder.Configuration["Redis_ConnectionString"]
    ?? builder.Configuration.GetConnectionString("Redis");

if (!string.IsNullOrWhiteSpace(redisConnectionString))
{
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = redisConnectionString;
        options.InstanceName = "catalog:";
    });
}
else
{
    Log.Warning("Redis connection string not configured. Falling back to in-memory distributed cache.");
    builder.Services.AddDistributedMemoryCache();
}

// ==========================================
// 11. Health Checks — deep MongoDB + Redis
// ==========================================
var mongoConnStr = builder.Configuration["MongoDB_ConnectionString"] ?? "mongodb://localhost:27017";
var redisHcStr   = redisConnectionString ?? "localhost:6379";

builder.Services.AddHealthChecks()
    .AddMongoDb(
        mongodbConnectionString: mongoConnStr,
        name: "mongodb",
        tags: new[] { "db", "ready" })
    .AddRedis(
        redisConnectionString: redisHcStr,
        name: "redis",
        tags: new[] { "cache", "ready" });

// ==========================================
// 11. Middleware pipeline
// ==========================================
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.Use(async (context, next) =>
{
    var containerId = Environment.GetEnvironmentVariable("HOSTNAME") ?? Environment.MachineName;
    context.Response.OnStarting(() =>
    {
        context.Response.Headers["X-Container-ID"] = containerId;
        return Task.CompletedTask;
    });

    await next();
});

app.UseCors("DevelopmentCors");

// Correlation ID enriches Serilog context before auth/auth middleware
app.UseMiddleware<CorrelationIdMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

// Deep health check endpoint
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
}).AllowAnonymous();

app.MapControllers();

app.Run();
