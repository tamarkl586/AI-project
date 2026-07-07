var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new
{
    status = "deprecated",
    service = "project1",
    message = "Monolith entrypoint is deprecated. Use ApiGateway + microservices stack."
})).AllowAnonymous();

app.Run();
