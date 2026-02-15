using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyShopBotNET9.Migrations
{
    /// <inheritdoc />
    public partial class AddSupportMessages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SupportMessages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    OrderId = table.Column<int>(type: "INTEGER", nullable: false),
                    SenderId = table.Column<long>(type: "INTEGER", nullable: false),
                    SenderType = table.Column<int>(type: "INTEGER", nullable: false),
                    MessageText = table.Column<string>(type: "TEXT", nullable: true),
                    PhotoFileId = table.Column<string>(type: "TEXT", nullable: true),
                    SentAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsReadByAdmin = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsReadByClient = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupportMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupportMessages_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SupportMessages_IsReadByAdmin",
                table: "SupportMessages",
                column: "IsReadByAdmin");

            migrationBuilder.CreateIndex(
                name: "IX_SupportMessages_IsReadByClient",
                table: "SupportMessages",
                column: "IsReadByClient");

            migrationBuilder.CreateIndex(
                name: "IX_SupportMessages_OrderId",
                table: "SupportMessages",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_SupportMessages_SenderType",
                table: "SupportMessages",
                column: "SenderType");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SupportMessages");
        }
    }
}
