using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VM.Migrations
{
    /// <inheritdoc />
    public partial class PaymentSettingsTableAdded : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FaceDeviceSettings",
                columns: table => new
                {
                    MachineId = table.Column<string>(type: "TEXT", maxLength: 35, nullable: false),
                    IpAddress = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FaceDeviceSettings", x => x.MachineId);
                });

            migrationBuilder.CreateTable(
                name: "PaymentAccountSettings",
                columns: table => new
                {
                    MachineId = table.Column<string>(type: "TEXT", maxLength: 35, nullable: false),
                    AccountPlan = table.Column<string>(type: "TEXT", maxLength: 15, nullable: false),
                    AuthType = table.Column<string>(type: "TEXT", maxLength: 15, nullable: false),
                    MonthlyLimit = table.Column<double>(type: "REAL", nullable: false),
                    DailyLimit = table.Column<double>(type: "REAL", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentAccountSettings", x => x.MachineId);
                });

            migrationBuilder.CreateTable(
                name: "PaymentSettings",
                columns: table => new
                {
                    MachineId = table.Column<string>(type: "TEXT", maxLength: 35, nullable: false),
                    Cash = table.Column<bool>(type: "INTEGER", nullable: false),
                    Upi = table.Column<bool>(type: "INTEGER", nullable: false),
                    Account = table.Column<bool>(type: "INTEGER", nullable: false),
                    Card = table.Column<bool>(type: "INTEGER", nullable: false),
                    Direct = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentSettings", x => x.MachineId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FaceDeviceSettings");

            migrationBuilder.DropTable(
                name: "PaymentAccountSettings");

            migrationBuilder.DropTable(
                name: "PaymentSettings");
        }
    }
}
