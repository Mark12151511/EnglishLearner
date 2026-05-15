using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnglishLearner.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSentenceTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Sentences",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Text = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Translation = table.Column<string>(type: "TEXT", nullable: true),
                    DifficultyLevel = table.Column<int>(type: "INTEGER", nullable: false),
                    Source = table.Column<string>(type: "TEXT", nullable: false),
                    WordCount = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sentences", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Sm2Profiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    WordId = table.Column<int>(type: "INTEGER", nullable: false),
                    Repetition = table.Column<int>(type: "INTEGER", nullable: false),
                    EasinessFactor = table.Column<double>(type: "REAL", nullable: false, defaultValue: 2.5),
                    IntervalDays = table.Column<int>(type: "INTEGER", nullable: false),
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
                name: "IX_Sentences_DifficultyLevel",
                table: "Sentences",
                column: "DifficultyLevel");

            migrationBuilder.CreateIndex(
                name: "IX_Sentences_Source",
                table: "Sentences",
                column: "Source");

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Sentences");

            migrationBuilder.DropTable(
                name: "Sm2Profiles");
        }
    }
}
