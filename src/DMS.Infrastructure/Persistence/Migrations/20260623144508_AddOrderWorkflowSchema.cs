using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace DMS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderWorkflowSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OM_SalesOrd",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OrderNo = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    CompanyId = table.Column<long>(type: "bigint", nullable: false),
                    CustomerId = table.Column<long>(type: "bigint", nullable: false),
                    SalesPersonId = table.Column<long>(type: "bigint", nullable: true),
                    SiteId = table.Column<long>(type: "bigint", nullable: false),
                    OrderDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<short>(type: "smallint", nullable: false),
                    SubTotal = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    VatAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    GrandTotal = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Note = table.Column<string>(type: "text", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OM_SalesOrd", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OM_SalesOrd_AR_Customer_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "AR_Customer",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OM_SalesOrd_AR_SalesPerson_SalesPersonId",
                        column: x => x.SalesPersonId,
                        principalTable: "AR_SalesPerson",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_OM_SalesOrd_IN_Site_SiteId",
                        column: x => x.SiteId,
                        principalTable: "IN_Site",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OM_SalesOrd_SYS_Company_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "SYS_Company",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OM_Invoice",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    InvoiceNo = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    SalesOrderId = table.Column<long>(type: "bigint", nullable: false),
                    CustomerId = table.Column<long>(type: "bigint", nullable: false),
                    InvoiceDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    SubTotal = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    VatAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    GrandTotal = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Status = table.Column<short>(type: "smallint", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OM_Invoice", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OM_Invoice_AR_Customer_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "AR_Customer",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OM_Invoice_OM_SalesOrd_SalesOrderId",
                        column: x => x.SalesOrderId,
                        principalTable: "OM_SalesOrd",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OM_SalesOrdDet",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SalesOrderId = table.Column<long>(type: "bigint", nullable: false),
                    ItemId = table.Column<long>(type: "bigint", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    VatRate = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    LineAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    LineVatAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OM_SalesOrdDet", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OM_SalesOrdDet_IN_Item_ItemId",
                        column: x => x.ItemId,
                        principalTable: "IN_Item",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OM_SalesOrdDet_OM_SalesOrd_SalesOrderId",
                        column: x => x.SalesOrderId,
                        principalTable: "OM_SalesOrd",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OM_Invoice_CustomerId",
                table: "OM_Invoice",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_OM_Invoice_InvoiceNo",
                table: "OM_Invoice",
                column: "InvoiceNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OM_Invoice_SalesOrderId",
                table: "OM_Invoice",
                column: "SalesOrderId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OM_SalesOrd_CompanyId",
                table: "OM_SalesOrd",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_OM_SalesOrd_CustomerId",
                table: "OM_SalesOrd",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_OM_SalesOrd_OrderNo",
                table: "OM_SalesOrd",
                column: "OrderNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OM_SalesOrd_SalesPersonId",
                table: "OM_SalesOrd",
                column: "SalesPersonId");

            migrationBuilder.CreateIndex(
                name: "IX_OM_SalesOrd_SiteId",
                table: "OM_SalesOrd",
                column: "SiteId");

            migrationBuilder.CreateIndex(
                name: "IX_OM_SalesOrdDet_ItemId",
                table: "OM_SalesOrdDet",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_OM_SalesOrdDet_SalesOrderId",
                table: "OM_SalesOrdDet",
                column: "SalesOrderId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OM_Invoice");

            migrationBuilder.DropTable(
                name: "OM_SalesOrdDet");

            migrationBuilder.DropTable(
                name: "OM_SalesOrd");
        }
    }
}
