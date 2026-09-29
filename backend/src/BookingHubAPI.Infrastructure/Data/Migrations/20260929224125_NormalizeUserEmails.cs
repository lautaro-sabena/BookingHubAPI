using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookingHubAPI.Infrastructure.Data.Migrations
{
    /// <summary>
    /// The application stores and looks up e-mails lower-cased (T6c). This lower-cases existing rows so the
    /// existing unique index on "Email" (IX_Users_Email) enforces case-insensitive uniqueness and lookups
    /// can compare the column directly. It never merges accounts: if lower-casing would make two e-mails
    /// collide the migration fails with the offending addresses and nothing is changed.
    /// </summary>
    /// <inheritdoc />
    public partial class NormalizeUserEmails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Idempotent scripts wrap every migration statement in their own DO block, and DO blocks cannot
            // nest. The guard is therefore a session-local function invoked through plain SQL
            // (CREATE TABLE AS SELECT), which is valid both inside that wrapper and when run directly.
            migrationBuilder.Sql("""
                CREATE FUNCTION pg_temp.normalize_user_emails() RETURNS integer LANGUAGE plpgsql AS $normalize$
                DECLARE
                    collisions text;
                BEGIN
                    SELECT string_agg(t.email || ' (' || t.n || ' accounts)', ', ' ORDER BY t.email)
                    INTO collisions
                    FROM (
                        SELECT lower("Email") AS email, count(*) AS n
                        FROM "Users"
                        GROUP BY lower("Email")
                        HAVING count(*) > 1
                    ) t;

                    IF collisions IS NOT NULL THEN
                        RAISE EXCEPTION 'Cannot normalize user e-mails: these addresses differ only by case: %. Resolve them manually (merge or rename the duplicate accounts), then run the migration again. Find them with: SELECT "Id", "Email", "Role", "CreatedAt" FROM "Users" WHERE lower("Email") IN (SELECT lower("Email") FROM "Users" GROUP BY lower("Email") HAVING count(*) > 1) ORDER BY lower("Email"), "CreatedAt";', collisions;
                    END IF;

                    UPDATE "Users" SET "Email" = lower("Email") WHERE "Email" <> lower("Email");
                    RETURN 0;
                END
                $normalize$;

                CREATE TEMP TABLE normalize_user_emails_result AS SELECT pg_temp.normalize_user_emails() AS done;
                DROP TABLE normalize_user_emails_result;
                DROP FUNCTION pg_temp.normalize_user_emails();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // The original casing is not recoverable and lower-case e-mails remain valid: nothing to undo.
        }
    }
}
