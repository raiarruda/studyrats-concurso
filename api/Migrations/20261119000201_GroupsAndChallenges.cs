using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudyRats.Api.Migrations
{
    /// <summary>
    /// Separa "grupo" (permanente, sem data) de "desafio" (edital + data da prova).
    /// Os grupos que já tinham período viram um desafio com o mesmo nome e as mesmas datas,
    /// então nenhum ranking existente muda.
    /// </summary>
    public partial class GroupsAndChallenges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MinDaysPerWeek",
                table: "Groups",
                type: "integer",
                nullable: false,
                defaultValue: 3);

            migrationBuilder.CreateTable(
                name: "Challenges",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GroupId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ExamDate = table.Column<DateOnly>(type: "date", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Challenges", x => x.Id);
                    table.CheckConstraint("ck_challenge_dates", "\"StartDate\" <= \"ExamDate\"");
                    table.ForeignKey(
                        name: "FK_Challenges_Groups_GroupId",
                        column: x => x.GroupId,
                        principalTable: "Groups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Copia o período de cada grupo existente para um desafio ANTES de apagar as colunas.
            migrationBuilder.Sql(@"
                INSERT INTO ""Challenges"" (""Id"", ""GroupId"", ""Name"", ""StartDate"", ""ExamDate"", ""CreatedAt"")
                SELECT gen_random_uuid(), ""Id"", ""Name"", ""StartDate"", ""EndDate"", ""CreatedAt""
                FROM ""Groups"";");

            migrationBuilder.DropColumn(
                name: "EndDate",
                table: "Groups");

            migrationBuilder.DropColumn(
                name: "StartDate",
                table: "Groups");

            migrationBuilder.AddCheckConstraint(
                name: "ck_group_min_days",
                table: "Groups",
                sql: "\"MinDaysPerWeek\" BETWEEN 1 AND 7");

            migrationBuilder.CreateIndex(
                name: "IX_Challenges_GroupId",
                table: "Challenges",
                column: "GroupId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "EndDate",
                table: "Groups",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.AddColumn<DateOnly>(
                name: "StartDate",
                table: "Groups",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            // Devolve ao grupo as datas do desafio mais recente (grupos sem desafio ficam com 0001-01-01).
            migrationBuilder.Sql(@"
                UPDATE ""Groups"" g
                SET ""StartDate"" = c.""StartDate"", ""EndDate"" = c.""ExamDate""
                FROM (
                    SELECT DISTINCT ON (""GroupId"") ""GroupId"", ""StartDate"", ""ExamDate""
                    FROM ""Challenges""
                    ORDER BY ""GroupId"", ""ExamDate"" DESC
                ) c
                WHERE c.""GroupId"" = g.""Id"";");

            migrationBuilder.DropTable(
                name: "Challenges");

            migrationBuilder.DropCheckConstraint(
                name: "ck_group_min_days",
                table: "Groups");

            migrationBuilder.DropColumn(
                name: "MinDaysPerWeek",
                table: "Groups");
        }
    }
}
