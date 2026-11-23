using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace alealamia.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateFreelancerProfileProperty : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastTrackChangeAt",
                table: "FreelancerProfiles",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "FreelancerProfiles",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastTrackChangeAt",
                table: "FreelancerProfiles");

            migrationBuilder.DropColumn(
                name: "Title",
                table: "FreelancerProfiles");
        }
    }
}
