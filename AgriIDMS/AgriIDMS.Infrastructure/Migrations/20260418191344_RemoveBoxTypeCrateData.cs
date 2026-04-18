using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgriIDMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveBoxTypeCrateData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // BoxType.Crate (=4) đã bỏ khỏi enum. Chuẩn hóa dữ liệu cũ trước khi deploy code mới.
            migrationBuilder.Sql(
                """
                UPDATE [Boxes] SET [BoxType] = 0 WHERE [BoxType] = 4;
                DELETE FROM [BoxTypeSpecs] WHERE [BoxType] = 4;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Không khôi phục giá trị enum đã xóa.
        }
    }
}
