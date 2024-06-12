using System.ComponentModel.DataAnnotations.Schema;
using System.Net.NetworkInformation;
using Microsoft.EntityFrameworkCore;

namespace DataSystem.Database;

public class ApplicationContext(string connectionString) : DbContext
{
    public DbSet<SensorData> SensorData { get; set; }

    private string ConnectionString { get; set; } = connectionString;

    protected override void OnConfiguring(DbContextOptionsBuilder options) => options.UseNpgsql(ConnectionString);
}

public class SensorData
{
    public int Id { get; set; }
    [Column(TypeName = "timestamp without time zone")]
    public DateTime TimeStamp { get; set; }
    [Column(TypeName = "macaddr")]
    public PhysicalAddress DeviceId { get; set; }
    [Column(TypeName = "numeric")]
    public decimal? CarbonDioxide { get; set; }
    [Column(TypeName = "numeric")]
    public decimal? Humidity { get; set; }
    [Column(TypeName = "boolean")]
    public bool? Light { get; set; }
    [Column(TypeName = "numeric")]
    public decimal? Lpg { get; set; }
    [Column(TypeName = "boolean")]
    public bool? Motion { get; set; }
    [Column(TypeName = "numeric")]
    public decimal? Smoke { get; set; }
    [Column(TypeName = "numeric")]
    public decimal? Temperature { get; set; }
    // for NOSQL-like data
    [Column(TypeName = "jsonb")]
    public string? AdditionalData { get; set; }
}