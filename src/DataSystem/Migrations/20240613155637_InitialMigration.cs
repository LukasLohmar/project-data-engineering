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
                name: "Authorization",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Token = table.Column<Guid>(type: "uuid", nullable: false),
                    Locked = table.Column<bool>(type: "boolean", nullable: false),
                    AuthorizedFlags = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Authorization", x => x.Id);
                });

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
                    AdditionalData = table.Column<string>(type: "jsonb", nullable: true),
                    ProviderTokenId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SensorData", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SensorData_Authorization_ProviderTokenId",
                        column: x => x.ProviderTokenId,
                        principalTable: "Authorization",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Authorization_Token",
                table: "Authorization",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SensorData_ProviderTokenId",
                table: "SensorData",
                column: "ProviderTokenId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SensorData");

            migrationBuilder.DropTable(
                name: "Authorization");
        }
    }
}
