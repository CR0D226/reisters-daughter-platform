using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ReistersDaughter.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddBiweeklyAvailability : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AvailabilityPeriods",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Deadline = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AvailabilityPeriods", x => x.Id);
                    table.CheckConstraint("CK_AvailabilityPeriod_Dates", "\"EndDate\" = \"StartDate\" + 13");
                });

            migrationBuilder.CreateTable(
                name: "AvailabilityEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AvailabilityPeriodId = table.Column<int>(type: "integer", nullable: false),
                    AppUserId = table.Column<int>(type: "integer", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    StartTime = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    EndTime = table.Column<TimeOnly>(type: "time without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AvailabilityEntries", x => x.Id);
                    table.CheckConstraint("CK_AvailabilityEntry_TimeRange", "\"EndTime\" > \"StartTime\"");
                    table.ForeignKey(
                        name: "FK_AvailabilityEntries_AppUsers_AppUserId",
                        column: x => x.AppUserId,
                        principalTable: "AppUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AvailabilityEntries_AvailabilityPeriods_AvailabilityPeriodId",
                        column: x => x.AvailabilityPeriodId,
                        principalTable: "AvailabilityPeriods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AvailabilitySubmissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AvailabilityPeriodId = table.Column<int>(type: "integer", nullable: false),
                    AppUserId = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AvailabilitySubmissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AvailabilitySubmissions_AppUsers_AppUserId",
                        column: x => x.AppUserId,
                        principalTable: "AppUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AvailabilitySubmissions_AvailabilityPeriods_AvailabilityPer~",
                        column: x => x.AvailabilityPeriodId,
                        principalTable: "AvailabilityPeriods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AvailabilityEntries_AppUserId",
                table: "AvailabilityEntries",
                column: "AppUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AvailabilityEntries_AvailabilityPeriodId_AppUserId_Date_Sta~",
                table: "AvailabilityEntries",
                columns: new[] { "AvailabilityPeriodId", "AppUserId", "Date", "StartTime" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AvailabilityPeriods_StartDate",
                table: "AvailabilityPeriods",
                column: "StartDate",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AvailabilitySubmissions_AppUserId",
                table: "AvailabilitySubmissions",
                column: "AppUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AvailabilitySubmissions_AvailabilityPeriodId_AppUserId",
                table: "AvailabilitySubmissions",
                columns: new[] { "AvailabilityPeriodId", "AppUserId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AvailabilityEntries");

            migrationBuilder.DropTable(
                name: "AvailabilitySubmissions");

            migrationBuilder.DropTable(
                name: "AvailabilityPeriods");
        }
    }
}
