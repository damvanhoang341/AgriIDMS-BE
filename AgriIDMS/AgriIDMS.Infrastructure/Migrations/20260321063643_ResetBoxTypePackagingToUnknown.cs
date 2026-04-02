using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgriIDMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ResetBoxTypePackagingToUnknown : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // BoxType = loại bao bì (enum mới). Dữ liệu cũ có thể đã map nhầm từ IsPartial (0/1) — đặt về Unknown.
            migrationBuilder.Sql("UPDATE Boxes SET BoxType = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
