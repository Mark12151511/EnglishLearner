using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace EnglishLearner.Infrastructure.Data.Migrations;

[DbContext(typeof(AppDbContext))]
partial class AppDbContextModelSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder)
    {
#pragma warning disable 612, 618
        modelBuilder.Entity("EnglishLearner.Core.Models.Article", b =>
        {
            b.Property<int>("Id")
                .ValueGeneratedOnAdd()
                .HasColumnType("INTEGER");
            b.Property<string>("Content")
                .IsRequired()
                .HasColumnType("TEXT");
            b.Property<DateTime>("CreatedAt")
                .HasColumnType("TEXT");
            b.Property<string>("DifficultyLevel")
                .IsRequired()
                .HasMaxLength(50)
                .HasColumnType("TEXT");
            b.Property<string>("Source")
                .HasMaxLength(500)
                .HasColumnType("TEXT");
            b.Property<string>("Title")
                .IsRequired()
                .HasMaxLength(300)
                .HasColumnType("TEXT");
            b.HasKey("Id");
            b.HasIndex("CreatedAt");
            b.HasIndex("DifficultyLevel");
            b.ToTable("Articles");
        });

        modelBuilder.Entity("EnglishLearner.Core.Models.LearningRecord", b =>
        {
            b.Property<int>("Id")
                .ValueGeneratedOnAdd()
                .HasColumnType("INTEGER");
            b.Property<int?>("ArticleId")
                .HasColumnType("INTEGER");
            b.Property<string>("ActivityType")
                .IsRequired()
                .HasColumnType("TEXT");
            b.Property<bool>("IsCorrect")
                .HasColumnType("INTEGER");
            b.Property<DateTime>("PracticedAt")
                .HasColumnType("TEXT");
            b.Property<int>("WordId")
                .HasColumnType("INTEGER");
            b.HasKey("Id");
            b.HasIndex("ArticleId");
            b.HasIndex("PracticedAt");
            b.HasIndex("WordId");
            b.HasIndex("WordId", "ActivityType");
            b.ToTable("LearningRecords");
        });

        modelBuilder.Entity("EnglishLearner.Core.Models.UserSetting", b =>
        {
            b.Property<int>("Id")
                .ValueGeneratedOnAdd()
                .HasColumnType("INTEGER");
            b.Property<string>("Key")
                .IsRequired()
                .HasMaxLength(100)
                .HasColumnType("TEXT");
            b.Property<DateTime>("UpdatedAt")
                .HasColumnType("TEXT");
            b.Property<string>("Value")
                .IsRequired()
                .HasMaxLength(2000)
                .HasColumnType("TEXT");
            b.HasKey("Id");
            b.HasIndex("Key")
                .IsUnique();
            b.ToTable("UserSettings");
        });

        modelBuilder.Entity("EnglishLearner.Core.Models.Word", b =>
        {
            b.Property<int>("Id")
                .ValueGeneratedOnAdd()
                .HasColumnType("INTEGER");
            b.Property<string>("AudioPath")
                .HasMaxLength(500)
                .HasColumnType("TEXT");
            b.Property<DateTime>("CreatedAt")
                .HasColumnType("TEXT");
            b.Property<int>("DifficultyLevel")
                .HasColumnType("INTEGER");
            b.Property<string>("Example")
                .HasColumnType("TEXT");
            b.Property<string>("Meaning")
                .IsRequired()
                .HasMaxLength(1000)
                .HasColumnType("TEXT");
            b.Property<string>("Phonetic")
                .IsRequired()
                .HasMaxLength(100)
                .HasColumnType("TEXT");
            b.Property<string>("Text")
                .IsRequired()
                .HasMaxLength(200)
                .HasColumnType("TEXT");
            b.HasKey("Id");
            b.HasIndex("DifficultyLevel");
            b.HasIndex("Text")
                .IsUnique();
            b.ToTable("Words");
        });

        modelBuilder.Entity("EnglishLearner.Core.Models.LearningRecord", b =>
        {
            b.HasOne("EnglishLearner.Core.Models.Article", "Article")
                .WithMany("LearningRecords")
                .HasForeignKey("ArticleId")
                .OnDelete(DeleteBehavior.SetNull);
            b.HasOne("EnglishLearner.Core.Models.Word", "Word")
                .WithMany("LearningRecords")
                .HasForeignKey("WordId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();
            b.Navigation("Article");
            b.Navigation("Word");
        });

        modelBuilder.Entity("EnglishLearner.Core.Models.Article", b =>
        {
            b.Navigation("LearningRecords");
        });

        modelBuilder.Entity("EnglishLearner.Core.Models.Word", b =>
        {
            b.Navigation("LearningRecords");
        });
#pragma warning restore 612, 618
    }
}
