using DrawReportService.DAL;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace DrawReportService.HealthChecks;

/// <summary>
/// Verifies Cassandra connectivity by executing a lightweight system query.
/// </summary>
public class CassandraHealthCheck : IHealthCheck
{
    private readonly ICassandraSessionFactory _sessionFactory;

    public CassandraHealthCheck(ICassandraSessionFactory sessionFactory)
    {
        _sessionFactory = sessionFactory;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var rs = await _sessionFactory.Session.ExecuteAsync(
                new Cassandra.SimpleStatement("SELECT release_version FROM system.local"));

            var row = rs.FirstOrDefault();
            var version = row?.GetValue<string>("release_version") ?? "unknown";
            return HealthCheckResult.Healthy($"Cassandra is reachable. Version: {version}");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Cassandra is unreachable.", ex);
        }
    }
}
