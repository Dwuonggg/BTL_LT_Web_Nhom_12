using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BTL_LT_Web_Nhom_12.Migrations
{
    /// <inheritdoc />
    public partial class AddDeliveryStartedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DeliveryStartedAt",
                table: "Orders",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeliveryStartedAt",
                table: "Orders");
        }
    }
}
