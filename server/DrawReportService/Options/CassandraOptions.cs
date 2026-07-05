namespace DrawReportService.Options;

public class CassandraOptions
{
    public List<string> ContactPoints { get; set; } = new();
    public int Port { get; set; } = 9042;
    public string Keyspace { get; set; } = "insights_db";
    public string Datacenter { get; set; } = "datacenter1";
}
