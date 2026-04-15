using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgriIDMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMissingBoxVolumeColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF COL_LENGTH('Boxes', 'VolumeM3') IS NULL
BEGIN
    ALTER TABLE [Boxes] ADD [VolumeM3] decimal(18,4) NOT NULL CONSTRAINT [DF_Boxes_VolumeM3] DEFAULT (0);
END
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF COL_LENGTH('Boxes', 'VolumeM3') IS NOT NULL
BEGIN
    DECLARE @dfName nvarchar(128);
    SELECT @dfName = dc.name
    FROM sys.default_constraints dc
    INNER JOIN sys.columns c ON c.default_object_id = dc.object_id
    WHERE dc.parent_object_id = OBJECT_ID('Boxes') AND c.name = 'VolumeM3';

    IF @dfName IS NOT NULL
        EXEC('ALTER TABLE [Boxes] DROP CONSTRAINT [' + @dfName + ']');

    ALTER TABLE [Boxes] DROP COLUMN [VolumeM3];
END
");
        }
    }
}
