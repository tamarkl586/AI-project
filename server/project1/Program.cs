using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using project1.BLL.Implementations;
using project1.BLL.Interfaces;
using project1.BLL.Services;
using project1.DAL;
using project1.DAL.Implementations;
using project1.DAL.Interfaces;
using project1.Mapping;
using Serilog;
using System.Text;
// ����� �-Namespace ����� ������
using System.Text.Encodings.Web;
using System.Text.Unicode;

var builder = WebApplication.CreateBuilder(args);

// ==========================================
// 1. Services Configuration (Dependency Injection)
// ==========================================

// ����� CORS - ����� ������ ������ ��-Frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularApp",
        policy =>
        {
            policy.AllowAnyOrigin()
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        });
});

// ����� ���: ����� ������ ���� �-JSON
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // ����� �-Encoder ����� ����� ����� ��� (���� ����� ������'��)
        options.JsonSerializerOptions.Encoder = JavaScriptEncoder.Create(UnicodeRanges.All);
    });

builder.Services.AddEndpointsApiExplorer();

// Swagger Configuration - ����� ����� �������
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "Chinese Auction API", Version = "v1" });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "�� ����� �� �� �����"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            new string[] {}
        }
    });
});

// Database
builder.Services.AddDbContext<ProjectContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// AutoMapper
builder.Services.AddAutoMapper(cfg => cfg.AddProfile<MappingProfile>());

// DAL Registration
builder.Services.AddScoped<IGiftDAL, GiftDAL>();
builder.Services.AddScoped<IDonorDAL, DonorDAL>();
builder.Services.AddScoped<ICartDAL, CartDAL>();
builder.Services.AddScoped<IUserDAL, UserDAL>();
builder.Services.AddScoped<ICategoryDAL, CategoryDAL>();

// BLL Registration
builder.Services.AddScoped<IGiftService, GiftService>();
builder.Services.AddScoped<IDonorService, DonorService>();
builder.Services.AddScoped<ICartService, CartService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IReportService, ReportService>();

// ==========================================
// 2. Authentication & Authorization
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
                    message = "���� ����� ������. ���� ���� ������� ��� ���� ������� ���"
                }));
            }
        };
    });

builder.Services.AddAuthorization();

//Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

builder.Host.UseSerilog();

var redisConnectionString = builder.Configuration["Redis_ConnectionString"]
                            ?? builder.Configuration.GetConnectionString("Redis")
                            ?? builder.Configuration["Redis:ConnectionString"];

if (string.IsNullOrWhiteSpace(redisConnectionString))
{
    Log.Warning("Redis connection string is not configured. Set Redis_ConnectionString or ConnectionStrings:Redis.");
}

builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = redisConnectionString;
    options.InstanceName = "project1:";
});

// ==========================================
// 3. Middleware Pipeline
// ==========================================

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

// Only redirect HTTPS outside of Docker/container environments
if (!app.Environment.IsEnvironment("Docker"))
{
    app.UseHttpsRedirection();
}

// Wait for SQL Server to be ready, then apply migrations
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ProjectContext>();
    var retries = 15;
    while (retries > 0)
    {
        try
        {
            db.Database.Migrate();
            Log.Information("Database migration applied successfully.");
            break;
        }
        catch (Exception ex)
        {
            retries--;
            Log.Warning("Database not ready, retrying in 5s... ({Retries} retries left). {Error}", retries, ex.Message);
            Thread.Sleep(5000);
        }
    }
}

// ����� �-CORS Middleware
app.UseCors("AllowAngularApp");

app.UseAuthentication();
app.UseAuthorization();

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapControllers();

app.MapFallbackToFile("index.html");

app.Run();









//using Microsoft.AspNetCore.Authentication.JwtBearer;
//using Microsoft.EntityFrameworkCore;
//using Microsoft.Extensions.DependencyInjection;
//using Microsoft.IdentityModel.Tokens;
//using Microsoft.OpenApi.Models;
//using project1.BLL.Implementations;
//using project1.BLL.Interfaces;
//using project1.BLL.Services;
//using project1.DAL;
//using project1.DAL.Implementations;
//using project1.DAL.Interfaces;
//using project1.Mapping;
//using Serilog;
//using System.Text;

//var builder = WebApplication.CreateBuilder(args);

//// ==========================================
//// 1. Services Configuration (Dependency Injection)
//// ==========================================

//builder.Services.AddControllers();
//builder.Services.AddEndpointsApiExplorer();

//// Swagger Configuration - ����� ����� �������
//builder.Services.AddSwaggerGen(options =>
//{
//    options.SwaggerDoc("v1", new OpenApiInfo { Title = "Chinese Auction API", Version = "v1" });

//    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
//    {
//        Name = "Authorization",
//        Type = SecuritySchemeType.Http,
//        Scheme = "Bearer",
//        BearerFormat = "JWT",
//        In = ParameterLocation.Header,
//        Description = "�� ����� �� �� �����"
//    });

//    options.AddSecurityRequirement(new OpenApiSecurityRequirement
//    {
//        {
//            new OpenApiSecurityScheme
//            {
//                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
//            },
//            new string[] {}
//        }
//    });
//});

//// Database
//builder.Services.AddDbContext<ProjectContext>(options =>
//    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

//// AutoMapper
//builder.Services.AddAutoMapper(cfg => cfg.AddProfile<MappingProfile>());

//// DAL Registration
//builder.Services.AddScoped<IGiftDAL, GiftDAL>();
//builder.Services.AddScoped<IDonorDAL, DonorDAL>();
//builder.Services.AddScoped<ICartDAL, CartDAL>();
//builder.Services.AddScoped<IUserDAL, UserDAL>();
//builder.Services.AddScoped<ICategoryDAL, CategoryDAL>();

//// BLL Registration
//builder.Services.AddScoped<IGiftService, GiftService>();
//builder.Services.AddScoped<IDonorService, DonorService>();
//builder.Services.AddScoped<ICartService, CartService>();
//builder.Services.AddScoped<IAuthService, AuthService>();
//builder.Services.AddScoped<ICategoryService, CategoryService>();
//builder.Services.AddScoped<IEmailService, EmailService>();
//builder.Services.AddScoped<IReportService, ReportService>();

//// ==========================================
//// 2. Authentication & Authorization
//// ==========================================

//var jwt = builder.Configuration.GetSection("Jwt");
//builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
//    .AddJwtBearer(options =>
//    {
//        options.TokenValidationParameters = new TokenValidationParameters
//        {
//            ValidateIssuer = true,
//            ValidateAudience = true,
//            ValidateLifetime = true,
//            ValidateIssuerSigningKey = true,
//            ValidIssuer = jwt["Issuer"],
//            ValidAudience = jwt["Audience"],
//            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Key"]!))
//        };

//        options.Events = new JwtBearerEvents
//        {
//            OnChallenge = context =>
//            {
//                context.HandleResponse();
//                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
//                context.Response.ContentType = "application/json";
//                return context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(new
//                {
//                    message = "���� ����� ������. ���� ���� ������� ��� ���� ������� ���"
//                }));
//            }
//        };
//    });

//builder.Services.AddAuthorization();

////Serilog
//Log.Logger = new LoggerConfiguration()
//    .ReadFrom.Configuration(builder.Configuration)
//    .CreateLogger();

//builder.Host.UseSerilog();

//// ==========================================
//// 3. Middleware Pipeline
//// ==========================================

//var app = builder.Build();

//if (app.Environment.IsDevelopment())
//{
//    app.UseSwagger();
//    app.UseSwaggerUI();
//}

//app.UseHttpsRedirection();

//app.UseAuthentication();
//app.UseAuthorization();

//app.MapControllers();

//app.Run();