using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookingHubAPI.Infrastructure.Data.Migrations
{
    /// <summary>
    /// One-time data conversion (T8b): before company time zones existed, reservations stored the
    /// company-local wall-clock time labelled as UTC. They are now real UTC instants.
    /// Runs exactly once through the migration history. The Reservations columns are either
    /// "timestamp with time zone" or "timestamp without time zone" depending on how the database was
    /// created, so each statement only touches the column type it is written for; the other one is a no-op.
    /// Companies already in UTC are unaffected and skipped.
    /// </summary>
    /// <inheritdoc />
    public partial class ConvertReservationTimesToUtc : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // timestamptz: the stored instant is the wall clock read as UTC -> re-read it in the company zone.
            migrationBuilder.Sql(ConvertForColumnType("timestamp with time zone",
                start: "(r.\"StartTime\" AT TIME ZONE 'UTC') AT TIME ZONE c.\"TimeZone\"",
                end: "(r.\"EndTime\" AT TIME ZONE 'UTC') AT TIME ZONE c.\"TimeZone\""));

            // timestamp (no zone): the stored value is the local wall clock -> convert it to a UTC clock reading.
            migrationBuilder.Sql(ConvertForColumnType("timestamp without time zone",
                start: "(r.\"StartTime\" AT TIME ZONE c.\"TimeZone\") AT TIME ZONE 'UTC'",
                end: "(r.\"EndTime\" AT TIME ZONE c.\"TimeZone\") AT TIME ZONE 'UTC'"));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(ConvertForColumnType("timestamp with time zone",
                start: "(r.\"StartTime\" AT TIME ZONE c.\"TimeZone\") AT TIME ZONE 'UTC'",
                end: "(r.\"EndTime\" AT TIME ZONE c.\"TimeZone\") AT TIME ZONE 'UTC'"));

            migrationBuilder.Sql(ConvertForColumnType("timestamp without time zone",
                start: "(r.\"StartTime\" AT TIME ZONE 'UTC') AT TIME ZONE c.\"TimeZone\"",
                end: "(r.\"EndTime\" AT TIME ZONE 'UTC') AT TIME ZONE c.\"TimeZone\""));
        }

        private static string ConvertForColumnType(string columnType, string start, string end) => $"""
            UPDATE "Reservations" r
            SET "StartTime" = {start},
                "EndTime" = {end}
            FROM "Companies" c
            WHERE c."Id" = r."CompanyId"
              AND c."TimeZone" <> 'UTC'
              AND EXISTS (
                SELECT 1 FROM information_schema.columns
                WHERE table_schema = current_schema()
                  AND table_name = 'Reservations'
                  AND column_name = 'StartTime'
                  AND data_type = '{columnType}');
            """;
    }
}
