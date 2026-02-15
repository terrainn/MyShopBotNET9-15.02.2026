using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyShopBotNET9.Migrations
{
    /// <inheritdoc />
    public partial class AddDeliveryCommentToOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DeliveryComment",
                table: "Orders",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeliveryComment",
                table: "Orders");
        }
    }
}
