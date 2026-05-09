using EnglishLearner.Infrastructure.Services;
using Xunit;

namespace EnglishLearner.Tests;

public class Sm2ServiceTests
{
    // Calculate 是实例方法，但纯计算不依赖 DB，用 null 构造
    private static Sm2Service CreateService()
    {
        // Sm2Service(AppDbContext db)，Calculate 不用 _db，传 null 即可
        return (Sm2Service)Activator.CreateInstance(typeof(Sm2Service), (object?)null)!;
    }

    // ── EF 公式验证（默认 EF=2.5）──────────────────────────────

    [Theory]
    [InlineData(0, 1.7)]   // 2.5 + 0.1 - 5×0.18 = 1.7
    [InlineData(1, 1.96)]  // 2.5 + 0.1 - 4×0.16 = 1.96
    [InlineData(2, 2.18)]  // 2.5 + 0.1 - 3×0.14 = 2.18
    [InlineData(3, 2.36)]  // 2.5 + 0.1 - 2×0.12 = 2.36
    [InlineData(4, 2.5)]   // 2.5 + 0.1 - 1×0.10 = 2.5
    [InlineData(5, 2.6)]   // 2.5 + 0.1 - 0     = 2.6
    public void Calculate_AllQualityLevels_EFValues(int quality, double expectedEF)
    {
        var sut = CreateService();
        var result = sut.Calculate(0, 2.5, quality);
        Assert.Equal(expectedEF, result.EasinessFactor, 4);
    }

    // ── EF 下限 clamp ─────────────────────────────────────────

    [Fact]
    public void Calculate_Quality0_LowEF_ClampedTo1_3()
    {
        var sut = CreateService();
        // 1.3 + 0.1 - 5×0.18 = 0.5 → clamped to 1.3
        var result = sut.Calculate(0, 1.3, 0);
        Assert.Equal(1.3, result.EasinessFactor, 4);
    }

    [Fact]
    public void Calculate_Quality1_EF1_3_ClampedTo1_3()
    {
        var sut = CreateService();
        // 1.3 + 0.1 - 4×0.16 = 0.76 → clamped to 1.3
        var result = sut.Calculate(0, 1.3, 1);
        Assert.Equal(1.3, result.EasinessFactor, 4);
    }

    // ── 间隔递进（quality >= 3 通过）──────────────────────────

    [Fact]
    public void Calculate_FirstPass_Interval1()
    {
        var sut = CreateService();
        var result = sut.Calculate(0, 2.5, 4);
        Assert.Equal(1, result.IntervalDays);
        Assert.Equal(1, result.Repetition);
    }

    [Fact]
    public void Calculate_SecondPass_Interval6()
    {
        var sut = CreateService();
        var result = sut.Calculate(1, 2.5, 4);
        Assert.Equal(6, result.IntervalDays);
        Assert.Equal(2, result.Repetition);
    }

    [Fact]
    public void Calculate_ThirdPass_IntervalIsEFx6()
    {
        var sut = CreateService();
        // quality=5 → EF=2.6, interval = ceil(2.6 × 6) = 16
        var result = sut.Calculate(2, 2.5, 5);
        Assert.Equal(3, result.Repetition);
        Assert.Equal(16, result.IntervalDays);
        Assert.Equal(2.6, result.EasinessFactor, 4);
    }

    [Fact]
    public void Calculate_FourthPass_GrowsByEF()
    {
        var sut = CreateService();
        // prev interval(3, ef=2.6) = ceil(2.6×6) = 16
        // quality=5 → new EF = 2.7
        // new interval = ceil(2.7 × 16) = 44
        var result = sut.Calculate(3, 2.6, 5);
        Assert.Equal(4, result.Repetition);
        Assert.Equal(44, result.IntervalDays);
        Assert.Equal(2.7, result.EasinessFactor, 4);
    }

    // ── 失败重置（quality < 3）────────────────────────────────

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void Calculate_QualityLessThan3_ResetsRepetitionAndInterval(int quality)
    {
        var sut = CreateService();
        var result = sut.Calculate(5, 2.3, quality);
        Assert.Equal(0, result.Repetition);
        Assert.Equal(1, result.IntervalDays);
    }

    // ── 失败后 EF 仍被更新（SM-2 标准：EF 始终重算）───────────

    [Fact]
    public void Calculate_Fail_Quality0_EFStillUpdated()
    {
        var sut = CreateService();
        var result = sut.Calculate(3, 2.5, 0);
        // EF = 2.5 + 0.1 - 5×0.18 = 1.7
        Assert.Equal(1.7, result.EasinessFactor, 4);
        Assert.Equal(0, result.Repetition);
        Assert.Equal(1, result.IntervalDays);
    }

    // ── 通过→失败→再通过的恢复序列 ─────────────────────────────

    [Fact]
    public void Calculate_PassFailPass_RebuildsFromScratch()
    {
        var sut = CreateService();

        var r1 = sut.Calculate(0, 2.5, 4);   // pass → interval=1
        Assert.Equal(1, r1.Repetition);

        var r2 = sut.Calculate(r1.Repetition, r1.EasinessFactor, 1); // fail → reset
        Assert.Equal(0, r2.Repetition);
        Assert.Equal(1, r2.IntervalDays);

        var r3 = sut.Calculate(r2.Repetition, r2.EasinessFactor, 4); // pass again
        Assert.Equal(1, r3.Repetition);
        Assert.Equal(1, r3.IntervalDays);
    }

    // ── NextReviewAt 验证 ──────────────────────────────────────

    [Fact]
    public void Calculate_NextReviewAt_IsNowPlusInterval()
    {
        var sut = CreateService();
        var before = DateTime.UtcNow;
        var result = sut.Calculate(1, 2.5, 4);
        var after = DateTime.UtcNow;

        Assert.True(result.NextReviewAt >= before.AddDays(6));
        Assert.True(result.NextReviewAt <= after.AddDays(6));
    }

    [Fact]
    public void Calculate_Interval1Day_NextReviewAtIsAbout24Hours()
    {
        var sut = CreateService();
        var result = sut.Calculate(0, 2.5, 3);
        Assert.Equal(1, result.IntervalDays);

        var diff = result.NextReviewAt - DateTime.UtcNow;
        Assert.True(diff.TotalHours is >= 23 and <= 25);
    }

    // ── 输入边界验证 ───────────────────────────────────────────

    [Theory]
    [InlineData(-1)]
    [InlineData(6)]
    [InlineData(100)]
    public void Calculate_InvalidQuality_ThrowsArgumentOutOfRange(int quality)
    {
        var sut = CreateService();
        Assert.Throws<ArgumentOutOfRangeException>(() => sut.Calculate(0, 2.5, quality));
    }

    // ── SM-2 经典完整序列 ─────────────────────────────────────

    [Fact]
    public void Calculate_ClassicSequence_1_6_GrowByEF()
    {
        var sut = CreateService();
        const double ef = 2.5;

        var r1 = sut.Calculate(0, ef, 4);
        Assert.Equal(1, r1.IntervalDays);
        Assert.Equal(1, r1.Repetition);

        var r2 = sut.Calculate(r1.Repetition, r1.EasinessFactor, 4);
        Assert.Equal(6, r2.IntervalDays);
        Assert.Equal(2, r2.Repetition);

        var expected3 = (int)Math.Ceiling(r2.EasinessFactor * 6);
        var r3 = sut.Calculate(r2.Repetition, r2.EasinessFactor, 4);
        Assert.Equal(expected3, r3.IntervalDays);
        Assert.Equal(3, r3.Repetition);

        var expected4 = (int)Math.Ceiling(r3.EasinessFactor * r3.IntervalDays);
        var r4 = sut.Calculate(r3.Repetition, r3.EasinessFactor, 4);
        Assert.Equal(expected4, r4.IntervalDays);
        Assert.Equal(4, r4.Repetition);
    }
}
