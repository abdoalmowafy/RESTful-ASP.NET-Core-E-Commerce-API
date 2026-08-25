using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECommerce.Infrastructure.Persistence.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260825190000_AddSearchExtensions")]
public partial class AddSearchExtensions : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Batch 1: extensions (own batch so later statements see installed objects)
        migrationBuilder.Sql(@"
CREATE EXTENSION IF NOT EXISTS unaccent;
CREATE EXTENSION IF NOT EXISTS pg_trgm;
");

        // Batch 2: immutable wrapper + indexes.
        // Single-arg unaccent(text) avoids regdictionary resolution failures during inlining.
        migrationBuilder.Sql(@"
CREATE OR REPLACE FUNCTION public.f_unaccent(text)
RETURNS text
LANGUAGE sql IMMUTABLE PARALLEL SAFE STRICT
AS $fn$ SELECT public.unaccent($1) $fn$;

CREATE INDEX IF NOT EXISTS ix_products_fts
    ON ""Products""
    USING GIN (to_tsvector('english', public.f_unaccent(""Name"") || ' ' || public.f_unaccent(""Description"")));

CREATE INDEX IF NOT EXISTS ix_products_name_trgm
    ON ""Products"" USING GIN (""Name"" gin_trgm_ops);

CREATE INDEX IF NOT EXISTS ix_products_description_trgm
    ON ""Products"" USING GIN (""Description"" gin_trgm_ops);
");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
DROP INDEX IF EXISTS ix_products_fts;
DROP INDEX IF EXISTS ix_products_name_trgm;
DROP INDEX IF EXISTS ix_products_description_trgm;
DROP FUNCTION IF EXISTS public.f_unaccent(text);
DROP EXTENSION IF EXISTS pg_trgm;
DROP EXTENSION IF EXISTS unaccent;");
    }
}
