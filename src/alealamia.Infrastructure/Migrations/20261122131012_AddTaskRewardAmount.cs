using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace alealamia.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTaskRewardAmount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "RewardAmount",
                table: "Tasks",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RewardAmount",
                table: "Tasks");
        }
    }
}
