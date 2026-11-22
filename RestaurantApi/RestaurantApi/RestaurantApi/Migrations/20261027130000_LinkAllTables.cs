using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using RestaurantApi.Database;

#nullable disable

namespace RestaurantApi.Migrations;

[DbContext(typeof(ContextDb))]
[Migration("20261027130000_LinkAllTables")]
public sealed class LinkAllTables : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "CreatedByUserID",
            table: "KnowledgeArticles",
            type: "integer",
            nullable: true);

        migrationBuilder.Sql("""
            DO $check$
            DECLARE invalid_refs text := '';
            BEGIN
                IF EXISTS (SELECT 1 FROM "Orders" o LEFT JOIN "Users" u ON u."UserID"=o."WaiterID" WHERE u."UserID" IS NULL) THEN invalid_refs := invalid_refs || 'Orders.WaiterID; '; END IF;
                IF EXISTS (SELECT 1 FROM "Orders" o LEFT JOIN "Users" u ON u."UserID"=o."PreparingByUserID" WHERE o."PreparingByUserID" IS NOT NULL AND u."UserID" IS NULL) THEN invalid_refs := invalid_refs || 'Orders.PreparingByUserID; '; END IF;
                IF EXISTS (SELECT 1 FROM "Orders" o LEFT JOIN "Users" u ON u."UserID"=o."ReadyByUserID" WHERE o."ReadyByUserID" IS NOT NULL AND u."UserID" IS NULL) THEN invalid_refs := invalid_refs || 'Orders.ReadyByUserID; '; END IF;
                IF EXISTS (SELECT 1 FROM "SupportConversations" c LEFT JOIN "Users" u ON u."UserID"=c."UserID" WHERE u."UserID" IS NULL) THEN invalid_refs := invalid_refs || 'SupportConversations.UserID; '; END IF;
                IF EXISTS (SELECT 1 FROM "SupportMessages" m LEFT JOIN "SupportConversations" c ON c."ConversationID"=m."ConversationID" WHERE c."ConversationID" IS NULL) THEN invalid_refs := invalid_refs || 'SupportMessages.ConversationID; '; END IF;
                IF EXISTS (SELECT 1 FROM "SupportMessages" m LEFT JOIN "Users" u ON u."UserID"=m."UserID" WHERE m."UserID" IS NOT NULL AND u."UserID" IS NULL) THEN invalid_refs := invalid_refs || 'SupportMessages.UserID; '; END IF;
                IF EXISTS (SELECT 1 FROM "SupportTickets" t LEFT JOIN "SupportConversations" c ON c."ConversationID"=t."ConversationID" WHERE c."ConversationID" IS NULL) THEN invalid_refs := invalid_refs || 'SupportTickets.ConversationID; '; END IF;
                IF EXISTS (SELECT 1 FROM "SupportTickets" t LEFT JOIN "Users" u ON u."UserID"=t."UserID" WHERE u."UserID" IS NULL) THEN invalid_refs := invalid_refs || 'SupportTickets.UserID; '; END IF;
                IF EXISTS (SELECT 1 FROM "SupportTickets" t LEFT JOIN "Users" u ON u."UserID"=t."OperatorID" WHERE t."OperatorID" IS NOT NULL AND u."UserID" IS NULL) THEN invalid_refs := invalid_refs || 'SupportTickets.OperatorID; '; END IF;
                IF EXISTS (SELECT 1 FROM "SupplyRequests" s LEFT JOIN "Ingredients" i ON i."IngredientID"=s."IngredientID" WHERE i."IngredientID" IS NULL) THEN invalid_refs := invalid_refs || 'SupplyRequests.IngredientID; '; END IF;
                IF EXISTS (SELECT 1 FROM "Stocktakes" s LEFT JOIN "Users" u ON u."UserID"=s."CreatedByUserID" WHERE u."UserID" IS NULL) THEN invalid_refs := invalid_refs || 'Stocktakes.CreatedByUserID; '; END IF;
                IF EXISTS (SELECT 1 FROM "StocktakeItems" x LEFT JOIN "Stocktakes" s ON s."StocktakeID"=x."StocktakeID" WHERE s."StocktakeID" IS NULL) THEN invalid_refs := invalid_refs || 'StocktakeItems.StocktakeID; '; END IF;
                IF EXISTS (SELECT 1 FROM "StocktakeItems" x LEFT JOIN "Ingredients" i ON i."IngredientID"=x."IngredientID" WHERE i."IngredientID" IS NULL) THEN invalid_refs := invalid_refs || 'StocktakeItems.IngredientID; '; END IF;
                IF EXISTS (SELECT 1 FROM "StockTransactions" x LEFT JOIN "Ingredients" i ON i."IngredientID"=x."IngredientID" WHERE i."IngredientID" IS NULL) THEN invalid_refs := invalid_refs || 'StockTransactions.IngredientID; '; END IF;
                IF EXISTS (SELECT 1 FROM "StockTransactions" x LEFT JOIN "Users" u ON u."UserID"=x."UserID" WHERE x."UserID" IS NOT NULL AND u."UserID" IS NULL) THEN invalid_refs := invalid_refs || 'StockTransactions.UserID; '; END IF;
                IF EXISTS (SELECT 1 FROM "PaymentTransactions" p LEFT JOIN "Orders" o ON o."OrderID"=p."OrderID" WHERE o."OrderID" IS NULL) THEN invalid_refs := invalid_refs || 'PaymentTransactions.OrderID; '; END IF;
                IF EXISTS (SELECT 1 FROM "PaymentTransactions" p LEFT JOIN "Users" u ON u."UserID"=p."UserID" WHERE u."UserID" IS NULL) THEN invalid_refs := invalid_refs || 'PaymentTransactions.UserID; '; END IF;
                IF invalid_refs <> '' THEN
                    RAISE EXCEPTION 'Foreign keys not added because orphan records exist in: % Correct those references and retry the migration.', invalid_refs;
                END IF;
            END
            $check$;
            """);

        migrationBuilder.CreateIndex("IX_Orders_WaiterID", "Orders", "WaiterID");
        migrationBuilder.CreateIndex("IX_Orders_PreparingByUserID", "Orders", "PreparingByUserID");
        migrationBuilder.CreateIndex("IX_Orders_ReadyByUserID", "Orders", "ReadyByUserID");
        migrationBuilder.CreateIndex("IX_SupportConversations_UserID", "SupportConversations", "UserID");
        migrationBuilder.CreateIndex("IX_SupportMessages_UserID", "SupportMessages", "UserID");
        migrationBuilder.CreateIndex("IX_SupportTickets_ConversationID", "SupportTickets", "ConversationID");
        migrationBuilder.CreateIndex("IX_SupportTickets_UserID", "SupportTickets", "UserID");
        migrationBuilder.CreateIndex("IX_SupportTickets_OperatorID", "SupportTickets", "OperatorID");
        migrationBuilder.CreateIndex("IX_KnowledgeArticles_CreatedByUserID", "KnowledgeArticles", "CreatedByUserID");
        migrationBuilder.CreateIndex("IX_SupplyRequests_IngredientID", "SupplyRequests", "IngredientID");
        migrationBuilder.CreateIndex("IX_Stocktakes_CreatedByUserID", "Stocktakes", "CreatedByUserID");
        migrationBuilder.CreateIndex("IX_StocktakeItems_StocktakeID", "StocktakeItems", "StocktakeID");
        migrationBuilder.CreateIndex("IX_StocktakeItems_IngredientID", "StocktakeItems", "IngredientID");
        migrationBuilder.CreateIndex("IX_StockTransactions_UserID", "StockTransactions", "UserID");
        migrationBuilder.CreateIndex("IX_PaymentTransactions_OrderID", "PaymentTransactions", "OrderID");
        migrationBuilder.CreateIndex("IX_PaymentTransactions_UserID", "PaymentTransactions", "UserID");

        migrationBuilder.AddForeignKey(name: "FK_Orders_Users_WaiterID", table: "Orders", column: "WaiterID", principalTable: "Users", principalColumn: "UserID", onDelete: ReferentialAction.Restrict);
        migrationBuilder.AddForeignKey(name: "FK_Orders_Users_PreparingByUserID", table: "Orders", column: "PreparingByUserID", principalTable: "Users", principalColumn: "UserID", onDelete: ReferentialAction.SetNull);
        migrationBuilder.AddForeignKey(name: "FK_Orders_Users_ReadyByUserID", table: "Orders", column: "ReadyByUserID", principalTable: "Users", principalColumn: "UserID", onDelete: ReferentialAction.SetNull);
        migrationBuilder.AddForeignKey(name: "FK_SupportConversations_Users_UserID", table: "SupportConversations", column: "UserID", principalTable: "Users", principalColumn: "UserID", onDelete: ReferentialAction.Restrict);
        migrationBuilder.AddForeignKey(name: "FK_SupportMessages_SupportConversations_ConversationID", table: "SupportMessages", column: "ConversationID", principalTable: "SupportConversations", principalColumn: "ConversationID", onDelete: ReferentialAction.Restrict);
        migrationBuilder.AddForeignKey(name: "FK_SupportMessages_Users_UserID", table: "SupportMessages", column: "UserID", principalTable: "Users", principalColumn: "UserID", onDelete: ReferentialAction.SetNull);
        migrationBuilder.AddForeignKey(name: "FK_SupportTickets_SupportConversations_ConversationID", table: "SupportTickets", column: "ConversationID", principalTable: "SupportConversations", principalColumn: "ConversationID", onDelete: ReferentialAction.Restrict);
        migrationBuilder.AddForeignKey(name: "FK_SupportTickets_Users_UserID", table: "SupportTickets", column: "UserID", principalTable: "Users", principalColumn: "UserID", onDelete: ReferentialAction.Restrict);
        migrationBuilder.AddForeignKey(name: "FK_SupportTickets_Users_OperatorID", table: "SupportTickets", column: "OperatorID", principalTable: "Users", principalColumn: "UserID", onDelete: ReferentialAction.SetNull);
        migrationBuilder.AddForeignKey(name: "FK_KnowledgeArticles_Users_CreatedByUserID", table: "KnowledgeArticles", column: "CreatedByUserID", principalTable: "Users", principalColumn: "UserID", onDelete: ReferentialAction.SetNull);
        migrationBuilder.AddForeignKey(name: "FK_SupplyRequests_Ingredients_IngredientID", table: "SupplyRequests", column: "IngredientID", principalTable: "Ingredients", principalColumn: "IngredientID", onDelete: ReferentialAction.Restrict);
        migrationBuilder.AddForeignKey(name: "FK_Stocktakes_Users_CreatedByUserID", table: "Stocktakes", column: "CreatedByUserID", principalTable: "Users", principalColumn: "UserID", onDelete: ReferentialAction.Restrict);
        migrationBuilder.AddForeignKey(name: "FK_StocktakeItems_Stocktakes_StocktakeID", table: "StocktakeItems", column: "StocktakeID", principalTable: "Stocktakes", principalColumn: "StocktakeID", onDelete: ReferentialAction.Restrict);
        migrationBuilder.AddForeignKey(name: "FK_StocktakeItems_Ingredients_IngredientID", table: "StocktakeItems", column: "IngredientID", principalTable: "Ingredients", principalColumn: "IngredientID", onDelete: ReferentialAction.Restrict);
        migrationBuilder.AddForeignKey(name: "FK_StockTransactions_Ingredients_IngredientID", table: "StockTransactions", column: "IngredientID", principalTable: "Ingredients", principalColumn: "IngredientID", onDelete: ReferentialAction.Restrict);
        migrationBuilder.AddForeignKey(name: "FK_StockTransactions_Users_UserID", table: "StockTransactions", column: "UserID", principalTable: "Users", principalColumn: "UserID", onDelete: ReferentialAction.SetNull);
        migrationBuilder.AddForeignKey(name: "FK_PaymentTransactions_Orders_OrderID", table: "PaymentTransactions", column: "OrderID", principalTable: "Orders", principalColumn: "OrderID", onDelete: ReferentialAction.Restrict);
        migrationBuilder.AddForeignKey(name: "FK_PaymentTransactions_Users_UserID", table: "PaymentTransactions", column: "UserID", principalTable: "Users", principalColumn: "UserID", onDelete: ReferentialAction.Restrict);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey("FK_PaymentTransactions_Users_UserID", "PaymentTransactions");
        migrationBuilder.DropForeignKey("FK_PaymentTransactions_Orders_OrderID", "PaymentTransactions");
        migrationBuilder.DropForeignKey("FK_StockTransactions_Users_UserID", "StockTransactions");
        migrationBuilder.DropForeignKey("FK_StockTransactions_Ingredients_IngredientID", "StockTransactions");
        migrationBuilder.DropForeignKey("FK_StocktakeItems_Ingredients_IngredientID", "StocktakeItems");
        migrationBuilder.DropForeignKey("FK_StocktakeItems_Stocktakes_StocktakeID", "StocktakeItems");
        migrationBuilder.DropForeignKey("FK_Stocktakes_Users_CreatedByUserID", "Stocktakes");
        migrationBuilder.DropForeignKey("FK_SupplyRequests_Ingredients_IngredientID", "SupplyRequests");
        migrationBuilder.DropForeignKey("FK_KnowledgeArticles_Users_CreatedByUserID", "KnowledgeArticles");
        migrationBuilder.DropForeignKey("FK_SupportTickets_Users_OperatorID", "SupportTickets");
        migrationBuilder.DropForeignKey("FK_SupportTickets_Users_UserID", "SupportTickets");
        migrationBuilder.DropForeignKey("FK_SupportTickets_SupportConversations_ConversationID", "SupportTickets");
        migrationBuilder.DropForeignKey("FK_SupportMessages_Users_UserID", "SupportMessages");
        migrationBuilder.DropForeignKey("FK_SupportMessages_SupportConversations_ConversationID", "SupportMessages");
        migrationBuilder.DropForeignKey("FK_SupportConversations_Users_UserID", "SupportConversations");
        migrationBuilder.DropForeignKey("FK_Orders_Users_ReadyByUserID", "Orders");
        migrationBuilder.DropForeignKey("FK_Orders_Users_PreparingByUserID", "Orders");
        migrationBuilder.DropForeignKey("FK_Orders_Users_WaiterID", "Orders");

        migrationBuilder.DropIndex("IX_PaymentTransactions_UserID", "PaymentTransactions");
        migrationBuilder.DropIndex("IX_PaymentTransactions_OrderID", "PaymentTransactions");
        migrationBuilder.DropIndex("IX_StockTransactions_UserID", "StockTransactions");
        migrationBuilder.DropIndex("IX_StocktakeItems_IngredientID", "StocktakeItems");
        migrationBuilder.DropIndex("IX_StocktakeItems_StocktakeID", "StocktakeItems");
        migrationBuilder.DropIndex("IX_Stocktakes_CreatedByUserID", "Stocktakes");
        migrationBuilder.DropIndex("IX_SupplyRequests_IngredientID", "SupplyRequests");
        migrationBuilder.DropIndex("IX_KnowledgeArticles_CreatedByUserID", "KnowledgeArticles");
        migrationBuilder.DropIndex("IX_SupportTickets_OperatorID", "SupportTickets");
        migrationBuilder.DropIndex("IX_SupportTickets_UserID", "SupportTickets");
        migrationBuilder.DropIndex("IX_SupportTickets_ConversationID", "SupportTickets");
        migrationBuilder.DropIndex("IX_SupportMessages_UserID", "SupportMessages");
        migrationBuilder.DropIndex("IX_SupportConversations_UserID", "SupportConversations");
        migrationBuilder.DropIndex("IX_Orders_ReadyByUserID", "Orders");
        migrationBuilder.DropIndex("IX_Orders_PreparingByUserID", "Orders");
        migrationBuilder.DropIndex("IX_Orders_WaiterID", "Orders");
        migrationBuilder.DropColumn("CreatedByUserID", "KnowledgeArticles");
    }
}
