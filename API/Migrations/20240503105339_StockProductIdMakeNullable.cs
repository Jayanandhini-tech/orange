using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CMS.API.Migrations
{
    /// <inheritdoc />
    public partial class StockProductIdMakeNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StockMachines_Products_ProductId",
                table: "StockMachines");

            migrationBuilder.AlterColumn<string>(
                name: "ProductId",
                table: "StockMachines",
                type: "varchar(35)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(35)")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddForeignKey(
                name: "FK_StockMachines_Products_ProductId",
                table: "StockMachines",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StockMachines_Products_ProductId",
                table: "StockMachines");

            migrationBuilder.UpdateData(
                table: "StockMachines",
                keyColumn: "ProductId",
                keyValue: null,
                column: "ProductId",
                value: "");

            migrationBuilder.AlterColumn<string>(
                name: "ProductId",
                table: "StockMachines",
                type: "varchar(35)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(35)",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddForeignKey(
                name: "FK_StockMachines_Products_ProductId",
                table: "StockMachines",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
