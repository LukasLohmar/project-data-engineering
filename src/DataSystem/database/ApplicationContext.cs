using System.ComponentModel.DataAnnotations.Schema;
using System.Net.NetworkInformation;
using Microsoft.EntityFrameworkCore;

namespace DataSystem.Database;

public class ApplicationContext : DbContext
{
    // needed for testing
    public ApplicationContext()
    {
    }

    public ApplicationContext(DbContextOptions<ApplicationContext> options) : base(options)
    {
    }

    public virtual DbSet<SensorData> SensorData { get; set; } = null!;
    public virtual DbSet<Authorization> Authorization { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // create index and guarantee uniqueness on UUID field
        modelBuilder.Entity<Authorization>()
            .HasIndex(p => new { p.Token })
            .IsUnique();
    }
}

public class SensorData
{
    public int Id { get; set; }

    [Column(TypeName = "timestamp without time zone")]
    public DateTime TimeStamp { get; set; }

    [Column(TypeName = "macaddr")] public PhysicalAddress DeviceId { get; set; }

    [Column(TypeName = "numeric")] public decimal? CarbonDioxide { get; set; }

    [Column(TypeName = "numeric")] public decimal? Humidity { get; set; }

    [Column(TypeName = "boolean")] public bool? Light { get; set; }

    [Column(TypeName = "numeric")] public decimal? Lpg { get; set; }

    [Column(TypeName = "boolean")] public bool? Motion { get; set; }

    [Column(TypeName = "numeric")] public decimal? Smoke { get; set; }

    [Column(TypeName = "numeric")] public decimal? Temperature { get; set; }

    // for NOSQL-like data
    [Column(TypeName = "jsonb")] public string? AdditionalData { get; set; }

    public Authorization? ProviderToken { get; set; }
}

public class Authorization
{
    public int Id { get; set; }

    [Column(TypeName = "uuid")] public Guid Token { get; set; }

    [Column(TypeName = "boolean")] public bool Locked { get; set; } = true;

    public AuthorizeFlags AuthorizedFlags { get; set; }

    [Column(TypeName = "timestamp without time zone")]
    public DateTime CreatedAt { get; set; }
    
    public static async Task<Authorization?> CheckForAuthorizationFlags(ApplicationContext context, string token, AuthorizeFlags flags, bool lockState)
    {
        // check for token
        if (string.IsNullOrEmpty(token) ||
            !Guid.TryParse(token.AsSpan(), out var guid))
            return null;
        
        return await context.Authorization.FirstOrDefaultAsync(i =>
                i.Token == guid && i.Locked == lockState && i.AuthorizedFlags.HasFlag(flags));
    }
}

[Flags]
public enum AuthorizeFlags
{
    None = 1,
    Read = 2,
    Write = 4,
    Delete = 8
}