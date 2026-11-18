using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tripory.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Add_Itinerary_Likes_Comments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "itinerary_comments",
                schema: "itinerary",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ItineraryId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Content = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_itinerary_comments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_itinerary_comments_itineraries_ItineraryId",
                        column: x => x.ItineraryId,
                        principalSchema: "itinerary",
                        principalTable: "itineraries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "itinerary_likes",
                schema: "itinerary",
                columns: table => new
                {
                    ItineraryId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_itinerary_likes", x => new { x.ItineraryId, x.UserId });
                    table.ForeignKey(
                        name: "FK_itinerary_likes_itineraries_ItineraryId",
                        column: x => x.ItineraryId,
                        principalSchema: "itinerary",
                        principalTable: "itineraries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_itinerary_comments_ItineraryId_CreatedAt_Id",
                schema: "itinerary",
                table: "itinerary_comments",
                columns: new[] { "ItineraryId", "CreatedAt", "Id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "itinerary_comments",
                schema: "itinerary");

            migrationBuilder.DropTable(
                name: "itinerary_likes",
                schema: "itinerary");
        }
    }
}
