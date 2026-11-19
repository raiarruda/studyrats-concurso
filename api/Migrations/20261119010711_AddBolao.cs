using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudyRats.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddBolao : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PenaltyCents",
                table: "Challenges",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int[]>(
                name: "PrizePercents",
                table: "Challenges",
                type: "integer[]",
                nullable: false,
                defaultValue: new int[0]);

            migrationBuilder.CreateTable(
                name: "ChallengeBettors",
                columns: table => new
                {
                    ChallengeId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    JoinedOn = table.Column<DateOnly>(type: "date", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChallengeBettors", x => new { x.ChallengeId, x.UserId });
                    table.ForeignKey(
                        name: "FK_ChallengeBettors_Challenges_ChallengeId",
                        column: x => x.ChallengeId,
                        principalTable: "Challenges",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChallengeBettors_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.AddCheckConstraint(
                name: "ck_challenge_penalty",
                table: "Challenges",
                sql: "\"PenaltyCents\" IS NULL OR \"PenaltyCents\" > 0");

            migrationBuilder.CreateIndex(
                name: "IX_ChallengeBettors_UserId",
                table: "ChallengeBettors",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChallengeBettors");

            migrationBuilder.DropCheckConstraint(
                name: "ck_challenge_penalty",
                table: "Challenges");

            migrationBuilder.DropColumn(
                name: "PenaltyCents",
                table: "Challenges");

            migrationBuilder.DropColumn(
                name: "PrizePercents",
                table: "Challenges");
        }
    }
}
