using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgriIDMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPriorityToProductVariantDiscountOverride : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Priority",
                table: "ProductVariantDiscountOverrides",
                type: "int",
                nullable: true);

            migrationBuilder.Sql(
                "UPDATE ProductVariantDiscountOverrides SET Priority = Id WHERE Priority IS NULL;");

            migrationBuilder.AlterColumn<int>(
                name: "Priority",
                table: "ProductVariantDiscountOverrides",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true,
                oldDefaultValue: null);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Priority",
                table: "ProductVariantDiscountOverrides");
        }
    }
}
