using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using RestaurantApi.Database;

#nullable disable

namespace RestaurantApi.Migrations;

[DbContext(typeof(ContextDb))]
[Migration("20261107120000_AddLoyaltyProgram")]
public sealed class AddLoyaltyProgram : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTime>(name: "BirthDate", table: "Users", type: "date", nullable: true);
        migrationBuilder.AddColumn<int>(name: "CustomerUserID", table: "Orders", type: "integer", nullable: true);
        migrationBuilder.AddColumn<decimal>(name: "DiscountAmount", table: "Orders", type: "numeric", nullable: false, defaultValue: 0m);
        migrationBuilder.AddColumn<decimal>(name: "DiscountPercent", table: "Orders", type: "numeric", nullable: false, defaultValue: 0m);
        migrationBuilder.AddColumn<string>(name: "DiscountReason", table: "Orders", type: "text", nullable: false, defaultValue: "");
        migrationBuilder.AddColumn<int>(name: "BirthdayDiscountYear", table: "Orders", type: "integer", nullable: true);
        migrationBuilder.CreateIndex(name: "IX_Orders_CustomerUserID", table: "Orders", column: "CustomerUserID");
        migrationBuilder.CreateIndex(name: "IX_Orders_CustomerUserID_BirthdayDiscountYear", table: "Orders", columns: new[] { "CustomerUserID", "BirthdayDiscountYear" }, unique: true, filter: "\"BirthdayDiscountYear\" IS NOT NULL");
        migrationBuilder.AddForeignKey(name: "FK_Orders_Users_CustomerUserID", table: "Orders", column: "CustomerUserID", principalTable: "Users", principalColumn: "UserID", onDelete: ReferentialAction.Restrict);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(name: "FK_Orders_Users_CustomerUserID", table: "Orders");
        migrationBuilder.DropIndex(name: "IX_Orders_CustomerUserID", table: "Orders");
        migrationBuilder.DropIndex(name: "IX_Orders_CustomerUserID_BirthdayDiscountYear", table: "Orders");
        migrationBuilder.DropColumn(name: "CustomerUserID", table: "Orders");
        migrationBuilder.DropColumn(name: "DiscountAmount", table: "Orders");
        migrationBuilder.DropColumn(name: "DiscountPercent", table: "Orders");
        migrationBuilder.DropColumn(name: "DiscountReason", table: "Orders");
        migrationBuilder.DropColumn(name: "BirthdayDiscountYear", table: "Orders");
        migrationBuilder.DropColumn(name: "BirthDate", table: "Users");
    }
}
