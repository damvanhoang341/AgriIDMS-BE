using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgriIDMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBoxTypeSpecsAndWarehouseDimensions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "FloorAreaM2",
                table: "Warehouses",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "LengthM",
                table: "Warehouses",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "WidthM",
                table: "Warehouses",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "FloorAreaM2",
                table: "Zones",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "LengthM",
                table: "Zones",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "WidthM",
                table: "Zones",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "FloorAreaM2",
                table: "Racks",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "LengthM",
                table: "Racks",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "WidthM",
                table: "Racks",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "HeightCm",
                table: "Slots",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "LengthCm",
                table: "Slots",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "VolumeM3",
                table: "Slots",
                type: "decimal(18,4)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "WidthCm",
                table: "Slots",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "BoxTypeSpecs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BoxType = table.Column<int>(type: "int", nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    LengthCm = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    WidthCm = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    HeightCm = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BoxTypeSpecs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BoxTypeSpecs_BoxType_IsActive",
                table: "BoxTypeSpecs",
                columns: new[] { "BoxType", "IsActive" });

            migrationBuilder.Sql(@"
                INSERT INTO [BoxTypeSpecs] ([BoxType], [DisplayName], [LengthCm], [WidthCm], [HeightCm], [IsActive], [CreatedAt])
                VALUES
                    (1, N'Mặc định', 0, 0, 0, 1, GETUTCDATE()),
                    (2, N'Mặc định', 0, 0, 0, 1, GETUTCDATE()),
                    (3, N'Mặc định', 0, 0, 0, 1, GETUTCDATE());
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BoxTypeSpecs");

            migrationBuilder.DropColumn(
                name: "FloorAreaM2",
                table: "Warehouses");

            migrationBuilder.DropColumn(
                name: "LengthM",
                table: "Warehouses");

            migrationBuilder.DropColumn(
                name: "WidthM",
                table: "Warehouses");

            migrationBuilder.DropColumn(
                name: "FloorAreaM2",
                table: "Zones");

            migrationBuilder.DropColumn(
                name: "LengthM",
                table: "Zones");

            migrationBuilder.DropColumn(
                name: "WidthM",
                table: "Zones");

            migrationBuilder.DropColumn(
                name: "FloorAreaM2",
                table: "Racks");

            migrationBuilder.DropColumn(
                name: "LengthM",
                table: "Racks");

            migrationBuilder.DropColumn(
                name: "WidthM",
                table: "Racks");

            migrationBuilder.DropColumn(
                name: "HeightCm",
                table: "Slots");

            migrationBuilder.DropColumn(
                name: "LengthCm",
                table: "Slots");

            migrationBuilder.DropColumn(
                name: "VolumeM3",
                table: "Slots");

            migrationBuilder.DropColumn(
                name: "WidthCm",
                table: "Slots");
        }
    }
}
