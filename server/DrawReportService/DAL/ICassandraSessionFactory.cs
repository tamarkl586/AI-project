using Cassandra;

namespace DrawReportService.DAL;

public interface ICassandraSessionFactory
{
    Cassandra.ISession Session { get; }
    Task EnsureSchemaAsync();
}
