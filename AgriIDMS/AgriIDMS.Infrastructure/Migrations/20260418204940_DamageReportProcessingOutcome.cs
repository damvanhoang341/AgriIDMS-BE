using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgriIDMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class DamageReportProcessingOutcome : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "ApprovedDamagedWeightKg",
                table: "DamageReports",
                type: "decimal(18,3)",
                precision: 18,
                scale: 3,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "BoxWeightSnapshotKg",
                table: "DamageReports",
                type: "decimal(18,3)",
                precision: 18,
                scale: 3,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProcessingOutcome",
                table: "DamageReports",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ApprovedDamagedWeightKg",
                table: "DamageReports");

            migrationBuilder.DropColumn(
                name: "BoxWeightSnapshotKg",
                table: "DamageReports");

            migrationBuilder.DropColumn(
                name: "ProcessingOutcome",
                table: "DamageReports");
        }
    }
}
