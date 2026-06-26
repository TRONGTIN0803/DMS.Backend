using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace DMS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddInventoryBatchLedgerSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "IN_Batch",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BatchNo = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Type = table.Column<short>(type: "smallint", nullable: false),
                    SiteId = table.Column<long>(type: "bigint", nullable: false),
                    Status = table.Column<short>(type: "smallint", nullable: false),
                    RefType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    RefId = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IN_Batch", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IN_Batch_IN_Site_SiteId",
                        column: x => x.SiteId,
                        principalTable: "IN_Site",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "IN_StockTransaction",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SiteId = table.Column<long>(type: "bigint", nullable: false),
                    ItemId = table.Column<long>(type: "bigint", nullable: false),
                    TransType = table.Column<short>(type: "smallint", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    BalanceAfter = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    RefType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    RefId = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IN_StockTransaction", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IN_StockTransaction_IN_Item_ItemId",
                        column: x => x.ItemId,
                        principalTable: "IN_Item",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_IN_StockTransaction_IN_Site_SiteId",
                        column: x => x.SiteId,
                        principalTable: "IN_Site",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "IN_BatchDetail",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BatchId = table.Column<long>(type: "bigint", nullable: false),
                    ItemId = table.Column<long>(type: "bigint", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IN_BatchDetail", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IN_BatchDetail_IN_Batch_BatchId",
                        column: x => x.BatchId,
                        principalTable: "IN_Batch",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_IN_BatchDetail_IN_Item_ItemId",
                        column: x => x.ItemId,
                        principalTable: "IN_Item",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_IN_Batch_BatchNo",
                table: "IN_Batch",
                column: "BatchNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IN_Batch_RefType_RefId",
                table: "IN_Batch",
                columns: new[] { "RefType", "RefId" });

            migrationBuilder.CreateIndex(
                name: "IX_IN_Batch_SiteId",
                table: "IN_Batch",
                column: "SiteId");

            migrationBuilder.CreateIndex(
                name: "IX_IN_BatchDetail_BatchId",
                table: "IN_BatchDetail",
                column: "BatchId");

            migrationBuilder.CreateIndex(
                name: "IX_IN_BatchDetail_ItemId",
                table: "IN_BatchDetail",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_IN_StockTransaction_ItemId",
                table: "IN_StockTransaction",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_IN_StockTransaction_RefType_RefId",
                table: "IN_StockTransaction",
                columns: new[] { "RefType", "RefId" });

            migrationBuilder.CreateIndex(
                name: "IX_IN_StockTransaction_SiteId",
                table: "IN_StockTransaction",
                column: "SiteId");

            migrationBuilder.CreateIndex(
                name: "IX_IN_StockTransaction_SiteId_ItemId_CreatedAt",
                table: "IN_StockTransaction",
                columns: new[] { "SiteId", "ItemId", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IN_BatchDetail");

            migrationBuilder.DropTable(
                name: "IN_StockTransaction");

            migrationBuilder.DropTable(
                name: "IN_Batch");
        }
    }
}
