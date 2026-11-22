using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using RestaurantApi.Database;

#nullable disable

namespace RestaurantApi.Migrations;

[DbContext(typeof(ContextDb))]
[Migration("20261110100000_AddBranchesAndDishNutrition")]
public sealed class AddBranchesAndDishNutrition : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Branches",
            columns: table => new
            {
                BranchID = table.Column<int>(type: "integer", nullable: false).Annotation("Npgsql:ValueGenerationStrategy", Npgsql.EntityFrameworkCore.PostgreSQL.Metadata.NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                Name = table.Column<string>(type: "text", nullable: false),
                Address = table.Column<string>(type: "text", nullable: false),
                IsActive = table.Column<bool>(type: "boolean", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_Branches", x => x.BranchID));

        migrationBuilder.Sql("""
            INSERT INTO "Branches" ("BranchID","Name","Address","IsActive") VALUES
            (1,'Баумана','ул. Баумана, 54, Казань',TRUE),
            (2,'Петербургская','ул. Петербургская, 55, Казань',TRUE),
            (3,'Ямашева','просп. Ямашева, 54, Казань',TRUE),
            (4,'Чистопольская','ул. Чистопольская, 20/12, Казань',TRUE),
            (5,'Декабристов','ул. Декабристов, 85, Казань',TRUE),
            (6,'Проспект Победы','просп. Победы, 139, Казань',TRUE),
            (7,'Татарстан','ул. Татарстан, 7, Казань',TRUE);
            SELECT setval(pg_get_serial_sequence('public."Branches"','BranchID'), (SELECT MAX("BranchID") FROM "Branches"), TRUE);
            """);

        migrationBuilder.AddColumn<int>(name: "BranchID", table: "Users", type: "integer", nullable: true);
        foreach (var table in new[] { "Tables", "MenuCategories", "MenuItems", "Orders", "Ingredients", "SupportConversations", "Stocktakes" })
            migrationBuilder.AddColumn<int>(name: "BranchID", table: table, type: "integer", nullable: false, defaultValue: 1);
        migrationBuilder.Sql("UPDATE \"Users\" SET \"BranchID\"=1 WHERE \"BranchID\" IS NULL;");
        migrationBuilder.AlterColumn<int>(name: "BranchID", table: "Users", type: "integer", nullable: false, oldClrType: typeof(int), oldType: "integer", oldNullable: true);

        migrationBuilder.AddColumn<decimal>(name: "Calories", table: "MenuItems", type: "numeric", nullable: false, defaultValue: 0m);
        migrationBuilder.AddColumn<int>(name: "WeightGrams", table: "MenuItems", type: "integer", nullable: false, defaultValue: 0);
        migrationBuilder.AddColumn<byte[]>(name: "ImageData", table: "MenuItems", type: "bytea", nullable: true);
        migrationBuilder.AddColumn<string>(name: "ImageMimeType", table: "MenuItems", type: "text", nullable: false, defaultValue: "");

        foreach (var pair in new[]
        {
            ("Users", "BranchID"), ("Tables", "BranchID"), ("MenuCategories", "BranchID"),
            ("MenuItems", "BranchID"), ("Orders", "BranchID"), ("Ingredients", "BranchID"),
            ("SupportConversations", "BranchID"), ("Stocktakes", "BranchID")
        })
        {
            migrationBuilder.CreateIndex(
                name: $"IX_{pair.Item1}_{pair.Item2}",
                table: pair.Item1,
                column: pair.Item2);
            migrationBuilder.AddForeignKey(
                name: $"FK_{pair.Item1}_Branches_{pair.Item2}",
                table: pair.Item1,
                column: pair.Item2,
                principalTable: "Branches",
                principalColumn: "BranchID",
                onDelete: ReferentialAction.Restrict);
        }

        foreach (var table in new[] { "Tables", "MenuCategories", "MenuItems", "Orders", "Ingredients", "SupportConversations", "Stocktakes" })
            migrationBuilder.Sql($"ALTER TABLE \"{table}\" ALTER COLUMN \"BranchID\" DROP DEFAULT;");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        foreach (var table in new[] { "Users", "Tables", "MenuCategories", "MenuItems", "Orders", "Ingredients", "SupportConversations", "Stocktakes" })
        {
            migrationBuilder.DropForeignKey($"FK_{table}_Branches_BranchID", table);
            migrationBuilder.DropIndex($"IX_{table}_BranchID", table);
            migrationBuilder.DropColumn("BranchID", table);
        }
        migrationBuilder.DropColumn("Calories", "MenuItems");
        migrationBuilder.DropColumn("WeightGrams", "MenuItems");
        migrationBuilder.DropColumn("ImageData", "MenuItems");
        migrationBuilder.DropColumn("ImageMimeType", "MenuItems");
        migrationBuilder.DropTable("Branches");
    }
}
