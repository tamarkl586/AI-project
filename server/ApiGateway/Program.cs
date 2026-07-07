using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// ==========================================
// Serilog (early init — captures startup errors)
// ==========================================
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .CreateLogger();

builder.Host.UseSerilog();

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
	?? new[]
	{
		"http://localhost:4200",
		"https://localhost:4200",
		"http://127.0.0.1:4200",
		"https://127.0.0.1:4200",
		"http://localhost:8080",
		"https://localhost:8080"
	};

builder.Services.AddCors(options =>
{
	options.AddPolicy("GatewayCors", policy =>
		policy.WithOrigins(allowedOrigins)
			.AllowAnyHeader()
			.AllowAnyMethod());
});

builder.Services.AddReverseProxy().LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

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
	});

builder.Services.AddAuthorization(options =>
{
	options.AddPolicy("RequireAuth", policy =>
		policy.RequireAuthenticatedUser());
});

var app = builder.Build();

app.UseCors("GatewayCors");

// ==========================================
// Correlation ID: generate or propagate X-Correlation-ID for every proxied request.
// This runs before auth so the ID appears in auth-failure logs.
// YARP forwards all request headers to upstream services by default,
// so the ID naturally reaches every downstream microservice.
// ==========================================
app.Use(async (context, next) =>
{
    const string header = "X-Correlation-ID";
    var correlationId = context.Request.Headers[header].FirstOrDefault()
        ?? Guid.NewGuid().ToString();

    context.Items[header] = correlationId;
    context.Response.Headers[header] = correlationId;
    context.Request.Headers[header] = correlationId; // ensure YARP forwards it even on first hop

    using (Serilog.Context.LogContext.PushProperty("CorrelationId", correlationId))
    {
        await next(context);
    }
});

app.UseAuthentication();
app.UseAuthorization();

app.Use(async (context, next) =>
{
	context.Request.Headers.Remove("X-User-Id");
	context.Request.Headers.Remove("X-User-Role");

	if (context.User.Identity?.IsAuthenticated == true)
	{
		var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
		var userRole = context.User.FindFirstValue(ClaimTypes.Role);

		if (!string.IsNullOrWhiteSpace(userId))
		{
			context.Request.Headers["X-User-Id"] = userId;
		}

		if (!string.IsNullOrWhiteSpace(userRole))
		{
			context.Request.Headers["X-User-Role"] = userRole;
		}
	}

	await next();
});

app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "ApiGateway" }))
	.AllowAnonymous();

app.MapReverseProxy();

app.Run();
