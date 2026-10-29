using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SchoolBuffet.Api.Data.Migrations
{
    public partial class KitchenPaymentsInventory : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "LowStockThreshold",
                schema: "public",
                table: "Products",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "StockQuantity",
                schema: "public",
                table: "Products",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "TrackStock",
                schema: "public",
                table: "Products",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "PreparingAtUtc",
                schema: "public",
                table: "Orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReadyAtUtc",
                schema: "public",
                table: "Orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Payments",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OrderId = table.Column<int>(type: "integer", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    Method = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    Status = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    Note = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PaidAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PaidByUserId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Payments", x => x.Id);
                    table.CheckConstraint("CK_Payments_Amount", "\"Amount\" >= 0");
                    table.ForeignKey(
                        name: "FK_Payments_Orders_OrderId",
                        column: x => x.OrderId,
                        principalSchema: "public",
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Payments_Users_PaidByUserId",
                        column: x => x.PaidByUserId,
                        principalSchema: "public",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "StockMovements",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProductId = table.Column<int>(type: "integer", nullable: false),
                    Change = table.Column<int>(type: "integer", nullable: false),
                    StockAfter = table.Column<int>(type: "integer", nullable: false),
                    Reason = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Reference = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockMovements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockMovements_Products_ProductId",
                        column: x => x.ProductId,
                        principalSchema: "public",
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StockMovements_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalSchema: "public",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.AddCheckConstraint(
                name: "CK_Products_LowStockThreshold",
                schema: "public",
                table: "Products",
                sql: "\"LowStockThreshold\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Products_StockQuantity",
                schema: "public",
                table: "Products",
                sql: "\"StockQuantity\" >= 0");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_CreatedAtUtc",
                schema: "public",
                table: "Payments",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_OrderId",
                schema: "public",
                table: "Payments",
                column: "OrderId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Payments_PaidByUserId",
                schema: "public",
                table: "Payments",
                column: "PaidByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_Status",
                schema: "public",
                table: "Payments",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_CreatedAtUtc",
                schema: "public",
                table: "StockMovements",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_CreatedByUserId",
                schema: "public",
                table: "StockMovements",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_ProductId",
                schema: "public",
                table: "StockMovements",
                column: "ProductId");

            migrationBuilder.Sql(
                """
                UPDATE "Products"
                SET "TrackStock" = TRUE,
                    "StockQuantity" = 80,
                    "LowStockThreshold" = 10
                """);

            migrationBuilder.Sql(
                """
                INSERT INTO "Payments" ("OrderId", "Amount", "Method", "Status", "Note", "CreatedAtUtc", "PaidAtUtc", "PaidByUserId")
                SELECT o."Id",
                       o."TotalAmount",
                       'KarteKantine',
                       CASE WHEN o."Status" = 'Perfunduar' THEN 'Paguar' ELSE 'NePaguar' END,
                       'Menaxhim automatik i pageses',
                       o."CreatedAtUtc",
                       o."CompletedAtUtc",
                       o."MarkedByUserId"
                FROM "Orders" o
                LEFT JOIN "Payments" p ON p."OrderId" = o."Id"
                WHERE p."Id" IS NULL
                """);

            migrationBuilder.Sql(
                """
                INSERT INTO "StockMovements" ("ProductId", "Change", "StockAfter", "Reason", "Reference", "CreatedAtUtc", "CreatedByUserId")
                SELECT p."Id", 80, 80, 'Korrektim inventari', 'Hapja e gjurmimit', p."CreatedAtUtc", NULL
                FROM "Products" p
                WHERE NOT EXISTS (SELECT 1 FROM "StockMovements" m WHERE m."ProductId" = p."Id")
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Payments",
                schema: "public");

            migrationBuilder.DropTable(
                name: "StockMovements",
                schema: "public");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Products_LowStockThreshold",
                schema: "public",
                table: "Products");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Products_StockQuantity",
                schema: "public",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "LowStockThreshold",
                schema: "public",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "StockQuantity",
                schema: "public",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "TrackStock",
                schema: "public",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "PreparingAtUtc",
                schema: "public",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ReadyAtUtc",
                schema: "public",
                table: "Orders");
        }
    }
}
