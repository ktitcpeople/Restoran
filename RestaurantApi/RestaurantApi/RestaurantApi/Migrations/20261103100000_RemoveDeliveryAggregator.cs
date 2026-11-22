using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using RestaurantApi.Database;

#nullable disable

namespace RestaurantApi.Migrations;

[DbContext(typeof(ContextDb))]
[Migration("20261103100000_RemoveDeliveryAggregator")]
public sealed class RemoveDeliveryAggregator : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("ALTER TABLE \"Deliveries\" DROP COLUMN IF EXISTS \"ExternalOrderID\", DROP COLUMN IF EXISTS \"Latitude\", DROP COLUMN IF EXISTS \"Longitude\";");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("ALTER TABLE \"Deliveries\" ADD COLUMN IF NOT EXISTS \"ExternalOrderID\" text NULL;");
        migrationBuilder.Sql("ALTER TABLE \"Deliveries\" ADD COLUMN IF NOT EXISTS \"Latitude\" double precision NULL;");
        migrationBuilder.Sql("ALTER TABLE \"Deliveries\" ADD COLUMN IF NOT EXISTS \"Longitude\" double precision NULL;");
    }
}
