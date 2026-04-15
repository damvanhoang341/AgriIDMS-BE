using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgriIDMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RefactorNearExpiryDiscountDesign : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "NearExpiryDiscountRules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    MinDaysLeft = table.Column<int>(type: "int", nullable: true),
                    MaxDaysLeft = table.Column<int>(type: "int", nullable: false),
                    DiscountPercent = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false, defaultValue: 100),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    StartAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EndAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NearExpiryDiscountRules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProductVariantDiscountOverrides",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductVariantId = table.Column<int>(type: "int", nullable: false),
                    OverrideNearExpiryDiscountPercent = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    StartAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EndAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductVariantDiscountOverrides", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductVariantDiscountOverrides_ProductVariants_ProductVariantId",
                        column: x => x.ProductVariantId,
                        principalTable: "ProductVariants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NearExpiryDiscountRules_IsActive_Priority_MaxDaysLeft",
                table: "NearExpiryDiscountRules",
                columns: new[] { "IsActive", "Priority", "MaxDaysLeft" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariantDiscountOverrides_ProductVariantId_IsActive",
                table: "ProductVariantDiscountOverrides",
                columns: new[] { "ProductVariantId", "IsActive" });

            migrationBuilder.Sql(@"
                INSERT INTO [NearExpiryDiscountRules]
                    ([Name], [MinDaysLeft], [MaxDaysLeft], [DiscountPercent], [Priority], [IsActive], [StartAtUtc], [EndAtUtc], [CreatedAt], [CreatedBy], [UpdatedAt], [UpdatedBy])
                SELECT
                    ISNULL([Name], CONCAT(N'Near-expiry <= ', [MaxDaysLeft], N' day(s)')),
                    NULL,
                    ISNULL([MaxDaysLeft], 0),
                    [DiscountPercent],
                    [Priority],
                    [IsActive],
                    NULL,
                    NULL,
                    [CreatedAt],
                    [CreatedBy],
                    [UpdatedAt],
                    [UpdatedBy]
                FROM [DiscountRules]
                WHERE [RuleType] = 1;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NearExpiryDiscountRules");

            migrationBuilder.DropTable(
                name: "ProductVariantDiscountOverrides");
        }
    }
}
