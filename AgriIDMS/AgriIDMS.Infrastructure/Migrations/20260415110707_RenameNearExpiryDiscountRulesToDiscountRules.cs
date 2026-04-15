using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgriIDMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RenameNearExpiryDiscountRulesToDiscountRules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_NearExpiryDiscountRules",
                table: "NearExpiryDiscountRules");

            migrationBuilder.RenameTable(
                name: "NearExpiryDiscountRules",
                newName: "DiscountRules");

            migrationBuilder.AddPrimaryKey(
                name: "PK_DiscountRules",
                table: "DiscountRules",
                column: "Id");

            migrationBuilder.AlterColumn<int>(
                name: "MaxDaysLeft",
                table: "DiscountRules",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<string>(
                name: "ConditionsJson",
                table: "DiscountRules",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "DiscountRules",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Priority",
                table: "DiscountRules",
                type: "int",
                nullable: false,
                defaultValue: 100);

            migrationBuilder.AddColumn<int>(
                name: "RuleType",
                table: "DiscountRules",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateIndex(
                name: "IX_DiscountRules_RuleType_IsActive_Priority",
                table: "DiscountRules",
                columns: new[] { "RuleType", "IsActive", "Priority" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DiscountRules_RuleType_IsActive_Priority",
                table: "DiscountRules");

            migrationBuilder.DropColumn(
                name: "ConditionsJson",
                table: "DiscountRules");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "DiscountRules");

            migrationBuilder.DropColumn(
                name: "Priority",
                table: "DiscountRules");

            migrationBuilder.DropColumn(
                name: "RuleType",
                table: "DiscountRules");

            migrationBuilder.AlterColumn<int>(
                name: "MaxDaysLeft",
                table: "DiscountRules",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.DropPrimaryKey(
                name: "PK_DiscountRules",
                table: "DiscountRules");

            migrationBuilder.RenameTable(
                name: "DiscountRules",
                newName: "NearExpiryDiscountRules");

            migrationBuilder.AddPrimaryKey(
                name: "PK_NearExpiryDiscountRules",
                table: "NearExpiryDiscountRules",
                column: "Id");
        }
    }
}
