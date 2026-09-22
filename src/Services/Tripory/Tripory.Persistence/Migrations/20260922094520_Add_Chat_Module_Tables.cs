using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tripory.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Add_Chat_Module_Tables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "conversations",
                schema: "identity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    User1Id = table.Column<Guid>(type: "uuid", nullable: false),
                    User2Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LastMessageId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastMessageContent = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    LastMessageType = table.Column<int>(type: "integer", nullable: true),
                    LastMessageAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UnreadCountUser1 = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    UnreadCountUser2 = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_conversations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "chat_messages",
                schema: "identity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ConversationId = table.Column<Guid>(type: "uuid", nullable: false),
                    SenderId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Content = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    VoiceUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    VoiceDuration = table.Column<int>(type: "integer", nullable: true),
                    call_status = table.Column<int>(type: "integer", nullable: true),
                    call_duration_seconds = table.Column<int>(type: "integer", nullable: true),
                    call_direction = table.Column<int>(type: "integer", nullable: true),
                    IsRead = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_chat_messages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_chat_messages_conversations_ConversationId",
                        column: x => x.ConversationId,
                        principalSchema: "identity",
                        principalTable: "conversations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_chat_messages_ConversationId_CreatedAt",
                schema: "identity",
                table: "chat_messages",
                columns: new[] { "ConversationId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_conversations_User1Id_User2Id",
                schema: "identity",
                table: "conversations",
                columns: new[] { "User1Id", "User2Id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "chat_messages",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "conversations",
                schema: "identity");
        }
    }
}
