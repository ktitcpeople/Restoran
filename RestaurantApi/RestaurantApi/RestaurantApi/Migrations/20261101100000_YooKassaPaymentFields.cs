using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using RestaurantApi.Database;

#nullable disable

namespace RestaurantApi.Migrations;

[DbContext(typeof(ContextDb))]
[Migration("20261101100000_YooKassaPaymentFields")]
public sealed class YooKassaPaymentFields : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("ALTER TABLE \"PaymentTransactions\" ADD COLUMN IF NOT EXISTS \"ProviderPaymentID\" text NULL;");
        migrationBuilder.Sql("ALTER TABLE \"PaymentTransactions\" ADD COLUMN IF NOT EXISTS \"ConfirmationUrl\" text NULL;");
        migrationBuilder.Sql("ALTER TABLE \"PaymentTransactions\" ADD COLUMN IF NOT EXISTS \"IdempotenceKey\" text NULL;");
        migrationBuilder.Sql("CREATE UNIQUE INDEX IF NOT EXISTS \"IX_PaymentTransactions_ProviderPaymentID\" ON \"PaymentTransactions\" (\"ProviderPaymentID\") WHERE \"ProviderPaymentID\" IS NOT NULL;");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP INDEX IF EXISTS \"IX_PaymentTransactions_ProviderPaymentID\";");
        migrationBuilder.Sql("ALTER TABLE \"PaymentTransactions\" DROP COLUMN IF EXISTS \"IdempotenceKey\";");
        migrationBuilder.Sql("ALTER TABLE \"PaymentTransactions\" DROP COLUMN IF EXISTS \"ConfirmationUrl\";");
        migrationBuilder.Sql("ALTER TABLE \"PaymentTransactions\" DROP COLUMN IF EXISTS \"ProviderPaymentID\";");
    }
}
