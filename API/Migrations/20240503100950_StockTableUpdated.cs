using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CMS.API.Migrations
{
    /// <inheritdoc />
    public partial class StockTableUpdated : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "TrayNumber",
                table: "StockMachines",
                newName: "MotorNumber");

            migrationBuilder.RenameColumn(
                name: "Cabin",
                table: "StockMachines",
                newName: "CabinId");

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedOn",
                table: "StockMachines",
                type: "datetime(0)",
                precision: 0,
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime(0)",
                oldPrecision: 0)
                .OldAnnotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.ComputedColumn);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "MotorNumber",
                table: "StockMachines",
                newName: "TrayNumber");

            migrationBuilder.RenameColumn(
                name: "CabinId",
                table: "StockMachines",
                newName: "Cabin");

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedOn",
                table: "StockMachines",
                type: "datetime(0)",
                precision: 0,
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime(0)",
                oldPrecision: 0)
                .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.ComputedColumn);
        }
    }
}
