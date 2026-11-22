using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using RestaurantApi.Database;

#nullable disable

namespace RestaurantApi.Migrations;

[DbContext(typeof(ContextDb))]
[Migration("20261027120000_MenuNutrition")]
public sealed class MenuNutrition : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("ALTER TABLE \"MenuItems\" ADD COLUMN IF NOT EXISTS \"ImageUrl\" text NOT NULL DEFAULT ''; ");
        migrationBuilder.Sql("ALTER TABLE \"MenuItems\" ADD COLUMN IF NOT EXISTS \"Description\" text NOT NULL DEFAULT ''; ");
        migrationBuilder.Sql("ALTER TABLE \"MenuItems\" ADD COLUMN IF NOT EXISTS \"Protein\" numeric NOT NULL DEFAULT 0; ");
        migrationBuilder.Sql("ALTER TABLE \"MenuItems\" ADD COLUMN IF NOT EXISTS \"Fat\" numeric NOT NULL DEFAULT 0; ");
        migrationBuilder.Sql("ALTER TABLE \"MenuItems\" ADD COLUMN IF NOT EXISTS \"Carbohydrates\" numeric NOT NULL DEFAULT 0; ");
        migrationBuilder.Sql("INSERT INTO \"Roles\" (\"RoleId\", \"Name\") VALUES (7, 'Supplier') ON CONFLICT (\"RoleId\") DO NOTHING;");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("ALTER TABLE \"MenuItems\" DROP COLUMN IF EXISTS \"ImageUrl\", DROP COLUMN IF EXISTS \"Description\", DROP COLUMN IF EXISTS \"Protein\", DROP COLUMN IF EXISTS \"Fat\", DROP COLUMN IF EXISTS \"Carbohydrates\";");
        migrationBuilder.Sql("DELETE FROM \"Roles\" WHERE \"RoleId\" = 7;");
    }
}
