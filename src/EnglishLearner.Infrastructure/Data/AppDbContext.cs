using EnglishLearner.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace EnglishLearner.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public DbSet<Word> Words => Set<Word>();
    public DbSet<Article> Articles => Set<Article>();
    public DbSet<LearningRecord> LearningRecords => Set<LearningRecord>();
    public DbSet<UserSetting> UserSettings => Set<UserSetting>();
    public DbSet<Sm2Profile> Sm2Profiles => Set<Sm2Profile>();

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ConfigureWord(modelBuilder);
        ConfigureArticle(modelBuilder);
        ConfigureLearningRecord(modelBuilder);
        ConfigureUserSetting(modelBuilder);
        ConfigureSm2Profile(modelBuilder);
    }

    private static void ConfigureWord(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Word>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Text).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Phonetic).HasMaxLength(100);
            entity.Property(e => e.Meaning).IsRequired().HasMaxLength(1000);
            entity.Property(e => e.AudioPath).HasMaxLength(500);

            entity.HasIndex(e => e.Text).IsUnique();
            entity.HasIndex(e => e.DifficultyLevel);
        });
    }

    private static void ConfigureArticle(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Article>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Title).IsRequired().HasMaxLength(300);
            entity.Property(e => e.Content).IsRequired();
            entity.Property(e => e.Source).HasMaxLength(500);
            entity.Property(e => e.DifficultyLevel).IsRequired().HasMaxLength(50);

            entity.HasIndex(e => e.DifficultyLevel);
            entity.HasIndex(e => e.CreatedAt);
        });
    }

    private static void ConfigureLearningRecord(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<LearningRecord>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.ActivityType).IsRequired().HasConversion<string>();

            entity.HasOne(e => e.Word)
                .WithMany(w => w.LearningRecords)
                .HasForeignKey(e => e.WordId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Article)
                .WithMany(a => a.LearningRecords)
                .HasForeignKey(e => e.ArticleId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasIndex(e => e.WordId);
            entity.HasIndex(e => e.PracticedAt);
            entity.HasIndex(e => new { e.WordId, e.ActivityType });
        });
    }

    private static void ConfigureUserSetting(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserSetting>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Key).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Value).IsRequired().HasMaxLength(2000);

            entity.HasIndex(e => e.Key).IsUnique();
        });
    }

    private static void ConfigureSm2Profile(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Sm2Profile>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.EasinessFactor).HasDefaultValue(2.5);

            entity.HasOne(e => e.Word)
                .WithOne()
                .HasForeignKey<Sm2Profile>(e => e.WordId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.WordId).IsUnique();
            entity.HasIndex(e => e.NextReviewAt);
        });
    }
}
