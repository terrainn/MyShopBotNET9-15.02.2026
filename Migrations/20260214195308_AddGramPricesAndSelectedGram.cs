using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyShopBotNET9.Migrations
{
    /// <inheritdoc />
    public partial class AddGramPricesAndSelectedGram : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PricesJson",
                table: "Products",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SelectedGram",
                table: "CartItems",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PricesJson",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "SelectedGram",
                table: "CartItems");
        }
    }
}
