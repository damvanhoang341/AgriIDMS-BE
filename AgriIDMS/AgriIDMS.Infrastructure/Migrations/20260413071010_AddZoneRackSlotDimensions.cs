using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgriIDMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddZoneRackSlotDimensions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // No-op:
            // The columns in this migration were already added by the previous
            // migration 20260413064654_AddBoxTypeSpecsAndWarehouseDimensions.
            // Keep this migration empty to preserve migration history order.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No-op for paired Up() no-op.
        }
    }
}
