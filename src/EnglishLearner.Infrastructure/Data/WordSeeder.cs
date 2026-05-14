using CsvHelper;
using CsvHelper.Configuration;
using EnglishLearner.Core.Models;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System.Globalization;

namespace EnglishLearner.Infrastructure.Data;

public class WordSeeder
{
    private readonly AppDbContext _db;

    public WordSeeder(AppDbContext db) => _db = db;

    public async Task SeedAsync(string csvPath)
    {
        var existingCount = await _db.Words.CountAsync();
        if (existingCount > 100)
        {
            Log.Information("WordSeeder: 词库已存在 ({Count} 词)，跳过导入", existingCount);
            return;
        }

        // Migration 插入的种子数据只有 100 条，用 CSV 完整数据替换
        if (existingCount > 0)
        {
            _db.Words.RemoveRange(_db.Words);
            await _db.SaveChangesAsync();
            Log.Information("WordSeeder: 清除旧种子数据 ({Count} 条)", existingCount);
        }

        if (!File.Exists(csvPath))
        {
            Log.Warning("WordSeeder: 找不到词库文件 {Path}", csvPath);
            return;
        }

        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            MissingFieldFound = null,   // 容忍缺列
            BadDataFound = null,
        };

        using var reader = new StreamReader(csvPath);
        using var csv    = new CsvReader(reader, config);
        var records = csv.GetRecords<WordCsvRecord>().ToList();

        // 过滤掉没有释义的行
        var words = records
            .Where(r => !string.IsNullOrWhiteSpace(r.Text)
                     && !string.IsNullOrWhiteSpace(r.Meaning))
            .Select(r => new Word
            {
                Text            = r.Text.Trim(),
                Phonetic        = r.Phonetic?.Trim() ?? "",
                Meaning         = r.Meaning.Trim(),
                Example         = string.IsNullOrWhiteSpace(r.Example) ? null : r.Example.Trim(),
                DifficultyLevel = r.DifficultyLevel,
                CreatedAt       = DateTime.UtcNow,
            })
            .ToList();

        // 批量插入，每500条一批避免内存过大
        const int batchSize = 500;
        for (int i = 0; i < words.Count; i += batchSize)
        {
            var batch = words.Skip(i).Take(batchSize);
            await _db.Words.AddRangeAsync(batch);
            await _db.SaveChangesAsync();
            Log.Information("WordSeeder: 已导入 {Count}/{Total}", 
                Math.Min(i + batchSize, words.Count), words.Count);
        }

        Log.Information("WordSeeder: 词库导入完成，共 {Total} 个单词", words.Count);
    }

    // 对应 CSV 列名
    private class WordCsvRecord
    {
        public string  Text            { get; set; } = "";
        public string? Phonetic        { get; set; }
        public string  Meaning         { get; set; } = "";
        public string? Example         { get; set; }
        public int     DifficultyLevel { get; set; }
    }
}