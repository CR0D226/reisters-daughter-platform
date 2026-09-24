using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ReistersDaughter.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialPostgres : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Inquiries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    EventType = table.Column<string>(type: "text", nullable: false),
                    Services = table.Column<List<string>>(type: "text[]", nullable: false),
                    FirstName = table.Column<string>(type: "text", nullable: false),
                    LastName = table.Column<string>(type: "text", nullable: false),
                    Company = table.Column<string>(type: "text", nullable: true),
                    Email = table.Column<string>(type: "text", nullable: false),
                    Phone = table.Column<string>(type: "text", nullable: false),
                    EventDate = table.Column<string>(type: "text", nullable: false),
                    EventTime = table.Column<string>(type: "text", nullable: false),
                    GuestCount = table.Column<int>(type: "integer", nullable: false),
                    CateringType = table.Column<string>(type: "text", nullable: true),
                    ServiceType = table.Column<string>(type: "text", nullable: true),
                    DeliveryAddress = table.Column<string>(type: "text", nullable: true),
                    Packaging = table.Column<string>(type: "text", nullable: true),
                    DietaryNeeds = table.Column<List<string>>(type: "text[]", nullable: false),
                    OtherDietaryNeeds = table.Column<string>(type: "text", nullable: true),
                    Recurring = table.Column<string>(type: "text", nullable: true),
                    Details = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Inquiries", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Inquiries");
        }
    }
}
