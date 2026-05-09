using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnglishLearner.Infrastructure.Data.Migrations;

public partial class InitialCreate : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Articles",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                Title = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                Content = table.Column<string>(type: "TEXT", nullable: false),
                Source = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                DifficultyLevel = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Articles", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "UserSettings",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                Key = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                Value = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_UserSettings", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Words",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                Text = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                Phonetic = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                Meaning = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                Example = table.Column<string>(type: "TEXT", nullable: true),
                AudioPath = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                DifficultyLevel = table.Column<int>(type: "INTEGER", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Words", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "LearningRecords",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                WordId = table.Column<int>(type: "INTEGER", nullable: false),
                ArticleId = table.Column<int>(type: "INTEGER", nullable: true),
                ActivityType = table.Column<string>(type: "TEXT", nullable: false),
                IsCorrect = table.Column<bool>(type: "INTEGER", nullable: false),
                PracticedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_LearningRecords", x => x.Id);
                table.ForeignKey(
                    name: "FK_LearningRecords_Articles_ArticleId",
                    column: x => x.ArticleId,
                    principalTable: "Articles",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.SetNull);
                table.ForeignKey(
                    name: "FK_LearningRecords_Words_WordId",
                    column: x => x.WordId,
                    principalTable: "Words",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Articles_CreatedAt",
            table: "Articles",
            column: "CreatedAt");

        migrationBuilder.CreateIndex(
            name: "IX_Articles_DifficultyLevel",
            table: "Articles",
            column: "DifficultyLevel");

        migrationBuilder.CreateIndex(
            name: "IX_LearningRecords_PracticedAt",
            table: "LearningRecords",
            column: "PracticedAt");

        migrationBuilder.CreateIndex(
            name: "IX_LearningRecords_WordId_ActivityType",
            table: "LearningRecords",
            columns: new[] { "WordId", "ActivityType" });

        migrationBuilder.CreateIndex(
            name: "IX_LearningRecords_WordId",
            table: "LearningRecords",
            column: "WordId");

        migrationBuilder.CreateIndex(
            name: "IX_UserSettings_Key",
            table: "UserSettings",
            column: "Key",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Words_DifficultyLevel",
            table: "Words",
            column: "DifficultyLevel");

        migrationBuilder.CreateIndex(
            name: "IX_Words_Text",
            table: "Words",
            column: "Text",
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "LearningRecords");
        migrationBuilder.DropTable(name: "UserSettings");
        migrationBuilder.DropTable(name: "Articles");
        migrationBuilder.DropTable(name: "Words");
    }
}
