using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using RestaurantApi.Database;

#nullable disable

namespace RestaurantApi.Migrations;

[DbContext(typeof(ContextDb))]
[Migration("20261027170000_SupplierAssignment")]
public sealed class SupplierAssignment : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("ALTER TABLE \"SupplyRequests\" ADD COLUMN IF NOT EXISTS \"SupplierUserID\" integer NULL;");
        migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS \"IX_SupplyRequests_SupplierUserID\" ON \"SupplyRequests\" (\"SupplierUserID\");");
        migrationBuilder.Sql("""
            DO $$ BEGIN
                IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_SupplyRequests_Users_SupplierUserID') THEN
                    ALTER TABLE "SupplyRequests" ADD CONSTRAINT "FK_SupplyRequests_Users_SupplierUserID"
                        FOREIGN KEY ("SupplierUserID") REFERENCES "Users" ("UserID") ON DELETE SET NULL;
                END IF;
            END $$;
            """);
        migrationBuilder.Sql("INSERT INTO \"Roles\" (\"RoleId\", \"Name\") VALUES (7, 'Supplier') ON CONFLICT (\"RoleId\") DO NOTHING;");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("ALTER TABLE \"SupplyRequests\" DROP CONSTRAINT IF EXISTS \"FK_SupplyRequests_Users_SupplierUserID\";");
        migrationBuilder.Sql("DROP INDEX IF EXISTS \"IX_SupplyRequests_SupplierUserID\";");
        migrationBuilder.Sql("ALTER TABLE \"SupplyRequests\" DROP COLUMN IF EXISTS \"SupplierUserID\";");
        migrationBuilder.Sql("DELETE FROM \"Roles\" WHERE \"RoleId\" = 7 AND \"Name\" = 'Supplier';");
    }
}
