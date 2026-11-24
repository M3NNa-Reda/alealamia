using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace alealamia.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RenameFreelancerProfileBioToAbout : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Bio",
                table: "FreelancerProfiles",
                newName: "About");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "About",
                table: "FreelancerProfiles",
                newName: "Bio");
        }
    }
}