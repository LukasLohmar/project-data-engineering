using System;
using System.Net.NetworkInformation;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace DataSystem.Migrations
{
    /// <inheritdoc />
    public partial class InitialMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SensorData",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TimeStamp = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    DeviceId = table.Column<PhysicalAddress>(type: "macaddr", nullable: false),
                    CarbonDioxide = table.Column<decimal>(type: "numeric", nullable: true),
                    Humidity = table.Column<decimal>(type: "numeric", nullable: true),
                    Light = table.Column<bool>(type: "boolean", nullable: true),
                    Lpg = table.Column<decimal>(type: "numeric", nullable: true),
                    Motion = table.Column<bool>(type: "boolean", nullable: true),
                    Smoke = table.Column<decimal>(type: "numeric", nullable: true),
                    Temperature = table.Column<decimal>(type: "numeric", nullable: true),
                    AdditionalData = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SensorData", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SensorData");
        }
    }
}
