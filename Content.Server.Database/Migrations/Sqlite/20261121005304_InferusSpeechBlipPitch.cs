using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Content.Server.Database.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class InferusSpeechBlipPitch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<float>(
                name: "speech_blip_pitch",
                table: "profile",
                type: "REAL",
                nullable: false,
                defaultValue: 1f); // Inferus - existing characters keep normal pitch
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "speech_blip_pitch",
                table: "profile");
        }
    }
}
