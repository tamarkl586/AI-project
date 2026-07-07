using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using WebBff.Infrastructure;
using WebBff.Middleware;

var builder = WebApplication.CreateBuilder(args);

// ==========================================
// Serilog (early init — captures startup errors)
// ==========================================
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .CreateLogger();

builder.Host.UseSerilog();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// CorrelationId propagation to outbound HTTP calls
builder.Services.AddHttpContextAccessor();
builder.Services.AddTransient<CorrelationIdDelegatingHandler>();

builder.Services.AddHttpClient("ticketing", client =>
{
	client.BaseAddress = new Uri(builder.Configuration["Services:TicketingBaseUrl"] ?? "http://ticketingservice:8080");
}).AddHttpMessageHandler<CorrelationIdDelegatingHandler>();

builder.Services.AddHttpClient("catalog", client =>
{
	client.BaseAddress = new Uri(builder.Configuration["Services:CatalogBaseUrl"] ?? "http://catalogservice:8080");
}).AddHttpMessageHandler<CorrelationIdDelegatingHandler>();

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

builder.Services.AddAuthorization();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
	app.UseSwagger();
	app.UseSwaggerUI();
}

// Correlation ID before auth middleware
app.UseMiddleware<CorrelationIdMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "WebBff" }))
	.AllowAnonymous();

app.MapControllers();

app.Run();
