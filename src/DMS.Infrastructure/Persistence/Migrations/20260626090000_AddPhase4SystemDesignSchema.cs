using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace DMS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPhase4SystemDesignSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SYS_AuditLog",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    OccurredOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EntityName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    EntityId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Action = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    OldValue = table.Column<string>(type: "jsonb", nullable: true),
                    NewValue = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SYS_AuditLog", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SYS_OutboxMessage",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    Payload = table.Column<string>(type: "jsonb", nullable: false),
                    OccurredOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ProcessedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Error = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SYS_OutboxMessage", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SYS_ProcessedMessage",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Handler = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    ProcessedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SYS_ProcessedMessage", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SYS_AuditLog_EntityName_EntityId",
                table: "SYS_AuditLog",
                columns: new[] { "EntityName", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_SYS_AuditLog_OccurredOn",
                table: "SYS_AuditLog",
                column: "OccurredOn");

            migrationBuilder.CreateIndex(
                name: "IX_SYS_OutboxMessage_OccurredOn",
                table: "SYS_OutboxMessage",
                column: "OccurredOn");

            migrationBuilder.CreateIndex(
                name: "IX_SYS_OutboxMessage_ProcessedOn",
                table: "SYS_OutboxMessage",
                column: "ProcessedOn");

            migrationBuilder.CreateIndex(
                name: "IX_SYS_ProcessedMessage_Id_Handler",
                table: "SYS_ProcessedMessage",
                columns: new[] { "Id", "Handler" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "SYS_AuditLog");
            migrationBuilder.DropTable(name: "SYS_OutboxMessage");
            migrationBuilder.DropTable(name: "SYS_ProcessedMessage");
        }
    }
}
