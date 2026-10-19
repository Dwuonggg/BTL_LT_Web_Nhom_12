using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BTL_LT_Web_Nhom_12.Migrations
{
    /// <inheritdoc />
    public partial class AddPasswordResetOtpForeignKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_PasswordResetOtps_CustomerId",
                table: "PasswordResetOtps",
                column: "CustomerId");

            migrationBuilder.AddForeignKey(
                name: "FK_PasswordResetOtps_Customers_CustomerId",
                table: "PasswordResetOtps",
                column: "CustomerId",
                principalTable: "Customers",
                principalColumn: "CustomerId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PasswordResetOtps_Customers_CustomerId",
                table: "PasswordResetOtps");

            migrationBuilder.DropIndex(
                name: "IX_PasswordResetOtps_CustomerId",
                table: "PasswordResetOtps");
        }
    }
}
