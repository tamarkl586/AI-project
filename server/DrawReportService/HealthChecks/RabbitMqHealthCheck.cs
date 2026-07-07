using Microsoft.Extensions.Diagnostics.HealthChecks;
using RabbitMQ.Client;

namespace DrawReportService.HealthChecks;

/// <summary>
/// Verifies RabbitMQ connectivity by opening and immediately closing a test connection.
/// </summary>
public class RabbitMqHealthCheck : IHealthCheck
{
    private readonly string _host;
    private readonly string _username;
    private readonly string _password;

    public RabbitMqHealthCheck(string host, string username, string password)
    {
        _host = host;
        _username = username;
        _password = password;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var factory = new ConnectionFactory
            {
                HostName = _host,
                UserName = _username,
                Password = _password,
                RequestedConnectionTimeout = TimeSpan.FromSeconds(3)
            };

            using var conn = factory.CreateConnection("health-probe");
            return Task.FromResult(conn.IsOpen
                ? HealthCheckResult.Healthy("RabbitMQ connection is open.")
                : HealthCheckResult.Unhealthy("RabbitMQ connection opened but IsOpen=false."));
        }
        catch (Exception ex)
        {
            return Task.FromResult(
                HealthCheckResult.Unhealthy("RabbitMQ is unreachable.", ex));
        }
    }
}
