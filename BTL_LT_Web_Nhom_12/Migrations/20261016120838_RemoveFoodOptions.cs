
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BTL_LT_Web_Nhom_12.Migrations
{
    public partial class RemoveFoodOptions : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Xóa bảng chi tiết tùy chọn trước
            migrationBuilder.DropTable(
                name: "OrderDetailOptions");

            // Xóa bảng tùy chọn món
            migrationBuilder.DropTable(
                name: "FoodOptions");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Khôi phục bảng FoodOptions
            migrationBuilder.CreateTable(
                name: "FoodOptions",
                columns: table => new
                {
                    FoodOptionId = table.Column<int>(
                        type: "int",
                        nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),

                    FoodId = table.Column<int>(
                        type: "int",
                        nullable: false),

                    AdditionalPrice = table.Column<decimal>(
                        type: "decimal(18,2)",
                        nullable: false),

                    Description = table.Column<string>(
                        type: "nvarchar(500)",
                        maxLength: 500,
                        nullable: true),

                    IsActive = table.Column<bool>(
                        type: "bit",
                        nullable: false),

                    Name = table.Column<string>(
                        type: "nvarchar(100)",
                        maxLength: 100,
                        nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey(
                        "PK_FoodOptions",
                        x => x.FoodOptionId);

                    table.ForeignKey(
                        name: "FK_FoodOptions_Foods_FoodId",
                        column: x => x.FoodId,
                        principalTable: "Foods",
                        principalColumn: "FoodId");
                });

            // Khôi phục bảng OrderDetailOptions
            migrationBuilder.CreateTable(
                name: "OrderDetailOptions",
                columns: table => new
                {
                    OrderDetailOptionId = table.Column<int>(
                        type: "int",
                        nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),

                    FoodOptionId = table.Column<int>(
                        type: "int",
                        nullable: false),

                    OrderDetailId = table.Column<int>(
                        type: "int",
                        nullable: false),

                    AdditionalPrice = table.Column<decimal>(
                        type: "decimal(18,2)",
                        nullable: false),

                    Quantity = table.Column<int>(
                        type: "int",
                        nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey(
                        "PK_OrderDetailOptions",
                        x => x.OrderDetailOptionId);

                    table.ForeignKey(
                        name: "FK_OrderDetailOptions_FoodOptions_FoodOptionId",
                        column: x => x.FoodOptionId,
                        principalTable: "FoodOptions",
                        principalColumn: "FoodOptionId");

                    table.ForeignKey(
                        name: "FK_OrderDetailOptions_OrderDetails_OrderDetailId",
                        column: x => x.OrderDetailId,
                        principalTable: "OrderDetails",
                        principalColumn: "OrderDetailId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_FoodOptions_FoodId",
                table: "FoodOptions",
                column: "FoodId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderDetailOptions_FoodOptionId",
                table: "OrderDetailOptions",
                column: "FoodOptionId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderDetailOptions_OrderDetailId",
                table: "OrderDetailOptions",
                column: "OrderDetailId");
        }
    }
}
