using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EPC.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCategoryCommissionRateAndSubCategoryManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "CommissionRate",
                table: "Categories",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CommissionRate",
                table: "Categories");
        }
    }
}
