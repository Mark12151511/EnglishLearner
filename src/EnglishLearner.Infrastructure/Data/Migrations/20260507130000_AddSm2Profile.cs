using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnglishLearner.Infrastructure.Data.Migrations;

public partial class AddSm2Profile : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Sm2Profiles",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                WordId = table.Column<int>(type: "INTEGER", nullable: false),
                Repetition = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                EasinessFactor = table.Column<double>(type: "REAL", nullable: false, defaultValue: 2.5),
                IntervalDays = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                NextReviewAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                LastReviewedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Sm2Profiles", x => x.Id);
                table.ForeignKey(
                    name: "FK_Sm2Profiles_Words_WordId",
                    column: x => x.WordId,
                    principalTable: "Words",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Sm2Profiles_NextReviewAt",
            table: "Sm2Profiles",
            column: "NextReviewAt");

        migrationBuilder.CreateIndex(
            name: "IX_Sm2Profiles_WordId",
            table: "Sm2Profiles",
            column: "WordId",
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "Sm2Profiles");
    }
}
