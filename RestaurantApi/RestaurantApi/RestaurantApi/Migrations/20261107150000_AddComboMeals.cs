using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using RestaurantApi.Database;

#nullable disable

namespace RestaurantApi.Migrations;

[DbContext(typeof(ContextDb))]
[Migration("20261107150000_AddComboMeals")]
public sealed class AddComboMeals : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Existing test data may have inserted explicit MenuItemID values without
        // advancing PostgreSQL's identity sequence. Bring it up to the current max.
        migrationBuilder.Sql("""
            SELECT setval(
                pg_get_serial_sequence('"MenuItems"', 'MenuItemID'),
                COALESCE((SELECT MAX("MenuItemID") FROM "MenuItems"), 1),
                EXISTS (SELECT 1 FROM "MenuItems")
            );
            """);

        migrationBuilder.Sql("""
            INSERT INTO "MenuItems" ("Name", "Price", "Category", "KitchenStation", "ImageUrl", "Description", "Protein", "Fat", "Carbohydrates", "MenuCategoryID", "IsAvailable")
            SELECT v."Name", v."Price", v."Category", 'Общая кухня', '', v."Description", 0, 0, 0, NULL, TRUE
            FROM (VALUES
                ('Комбо «Обед у очага»', 590::numeric, 'Комбо', 'Куриный суп, куриная котлета с картофельным пюре и морс.'),
                ('Комбо «Домашний ужин»', 890::numeric, 'Комбо', 'Салат с овощами, паста с курицей в сливочном соусе и ягодный морс.'),
                ('Бизнес-ланч «Классический»', 490::numeric, 'Бизнес-ланчи', 'Суп дня, горячее с гарниром и напиток. Состав супа уточняйте у сотрудника.')
            ) AS v("Name", "Price", "Category", "Description")
            WHERE NOT EXISTS (SELECT 1 FROM "MenuItems" m WHERE m."Name" = v."Name");
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DELETE FROM \"MenuItems\" WHERE \"Name\" IN ('Комбо «Обед у очага»', 'Комбо «Домашний ужин»', 'Бизнес-ланч «Классический»');");
    }
}
