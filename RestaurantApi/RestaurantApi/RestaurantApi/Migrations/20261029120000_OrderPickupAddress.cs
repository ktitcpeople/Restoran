using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using RestaurantApi.Database;

#nullable disable

namespace RestaurantApi.Migrations;

[DbContext(typeof(ContextDb))]
[Migration("20261029120000_OrderPickupAddress")]
public sealed class OrderPickupAddress : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("ALTER TABLE \"Orders\" ADD COLUMN IF NOT EXISTS \"PickupAddress\" text NOT NULL DEFAULT 'ул. Баумана, 54, Казань';");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("ALTER TABLE \"Orders\" DROP COLUMN IF EXISTS \"PickupAddress\";");
    }
}
