using Cassandra;
using DrawReportService.Options;
using Microsoft.Extensions.Options;

namespace DrawReportService.DAL;

public class CassandraSessionFactory : ICassandraSessionFactory
{
    private readonly CassandraOptions _options;
    private readonly ILogger<CassandraSessionFactory> _logger;
    private readonly ICluster _cluster;

    public Cassandra.ISession Session { get; }

    public CassandraSessionFactory(IOptions<CassandraOptions> options, ILogger<CassandraSessionFactory> logger)
    {
        _options = options.Value;
        _logger = logger;

        if (_options.ContactPoints.Count == 0)
        {
            throw new InvalidOperationException("At least one Cassandra contact point must be configured.");
        }

        _cluster = Cluster.Builder()
            .AddContactPoints(_options.ContactPoints)
            .WithPort(_options.Port)
            .WithLoadBalancingPolicy(new DCAwareRoundRobinPolicy(_options.Datacenter))
            .Build();

        Session = _cluster.Connect();
    }

    public async Task EnsureSchemaAsync()
    {
        _logger.LogInformation("Ensuring Cassandra keyspace and reporting tables exist.");

        await Session.ExecuteAsync(new SimpleStatement($@"
            CREATE KEYSPACE IF NOT EXISTS {_options.Keyspace}
            WITH replication = {{ 'class': 'SimpleStrategy', 'replication_factor': 1 }};"));

        await Session.ExecuteAsync(new SimpleStatement($"USE {_options.Keyspace};"));

        await Session.ExecuteAsync(new SimpleStatement(@"
            CREATE TABLE IF NOT EXISTS winner_by_gift (
                gift_id int PRIMARY KEY,
                gift_name text,
                picture text,
                winner_id int,
                winner_name text,
                winner_email text,
                drawn_at timestamp
            );"));

        await Session.ExecuteAsync(new SimpleStatement(@"
            CREATE TABLE IF NOT EXISTS draw_audit_by_gift (
                gift_id int,
                draw_time timestamp,
                gift_name text,
                winner_id int,
                winner_name text,
                winner_email text,
                total_tickets int,
                email_sent boolean,
                PRIMARY KEY (gift_id, draw_time)
            ) WITH CLUSTERING ORDER BY (draw_time DESC);"));

        await Session.ExecuteAsync(new SimpleStatement(@"
            CREATE TABLE IF NOT EXISTS gift_purchases_by_gift (
                gift_id int,
                user_id int,
                user_name text,
                user_email text,
                quantity int,
                PRIMARY KEY (gift_id, user_id)
            );"));

        await Session.ExecuteAsync(new SimpleStatement(@"
            CREATE TABLE IF NOT EXISTS gift_summary_by_gift (
                gift_id int PRIMARY KEY,
                gift_name text,
                picture text,
                price int,
                total_tickets int,
                total_earned decimal,
                winner_name text
            );"));

        await Session.ExecuteAsync(new SimpleStatement(@"
            CREATE TABLE IF NOT EXISTS purchaser_analytics_by_user (
                user_id int PRIMARY KEY,
                name text,
                email text,
                phone text,
                total_tickets int,
                grand_total_spent int
            );"));

        await Session.ExecuteAsync(new SimpleStatement(@"
            CREATE TABLE IF NOT EXISTS purchaser_items_by_user (
                user_id int,
                purchased_at timestamp,
                gift_name text,
                quantity int,
                price_per_unit int,
                total_price int,
                PRIMARY KEY (user_id, purchased_at, gift_name)
            ) WITH CLUSTERING ORDER BY (purchased_at DESC, gift_name ASC);"));
    }
}
