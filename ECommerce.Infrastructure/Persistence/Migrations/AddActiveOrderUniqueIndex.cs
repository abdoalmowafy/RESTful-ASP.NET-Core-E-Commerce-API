using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECommerce.Infrastructure.Persistence.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260825210000_AddActiveOrderUniqueIndex")]
public partial class AddActiveOrderUniqueIndex : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Guarantees at most one active (non-terminal) order per user even under
        // concurrent checkouts — the application-level pre-check is advisory only.
        migrationBuilder.Sql(@"
CREATE UNIQUE INDEX IF NOT EXISTS ux_orders_one_active_per_user
    ON ""Orders"" (""UserId"")
    WHERE ""DeletedAt"" IS NULL
      AND ""Status"" IN ('Paying', 'Processing', 'OnTheWay');");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"DROP INDEX IF EXISTS ux_orders_one_active_per_user;");
    }
}
