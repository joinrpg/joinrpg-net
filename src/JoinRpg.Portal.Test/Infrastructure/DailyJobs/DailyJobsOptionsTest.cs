using JoinRpg.Interfaces;
using JoinRpg.Portal.Infrastructure.DailyJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace JoinRpg.Portal.Test.Infrastructure.DailyJobs;

public class DailyJobsOptionsTest
{
    private class SampleJob : IDailyJob
    {
        public Task RunOnce(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private class OtherJob : IDailyJob
    {
        public Task RunOnce(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private static DailyJobsOptions Disabled(params string[] names) => new() { Disabled = names };

    [Fact]
    public void EmptyByDefault() => new DailyJobsOptions().IsDisabled(typeof(SampleJob)).ShouldBeFalse();

    [Fact]
    public void MatchesShortName() => Disabled("SampleJob").IsDisabled(typeof(SampleJob)).ShouldBeTrue();

    [Fact]
    public void MatchesFullName() => Disabled(typeof(SampleJob).FullName!).IsDisabled(typeof(SampleJob)).ShouldBeTrue();

    [Fact]
    public void IgnoresCase() => Disabled("samplejob").IsDisabled(typeof(SampleJob)).ShouldBeTrue();

    [Fact]
    public void IgnoresCaseOfFullName() => Disabled(typeof(SampleJob).FullName!.ToUpperInvariant()).IsDisabled(typeof(SampleJob)).ShouldBeTrue();

    [Fact]
    public void IgnoresSurroundingSpaces() => Disabled(" SampleJob ").IsDisabled(typeof(SampleJob)).ShouldBeTrue();

    [Fact]
    public void DoesNotMatchOtherJob() => Disabled("SampleJob").IsDisabled(typeof(OtherJob)).ShouldBeFalse();

    [Fact]
    public void DoesNotMatchPartialName() => Disabled("Sample").IsDisabled(typeof(SampleJob)).ShouldBeFalse();

    [Fact]
    public void ReportsUnknownNames()
        => Disabled("SampleJob", "NoSuchJob", typeof(OtherJob).FullName!)
            .GetUnknownNames([typeof(SampleJob), typeof(OtherJob)])
            .ShouldBe(["NoSuchJob"]);

    [Fact]
    public void NoUnknownNamesWhenEmpty() => new DailyJobsOptions().GetUnknownNames([typeof(SampleJob)]).ShouldBeEmpty();

    [Fact]
    public void BindsFromIndexedKeys()
    {
        // Переменная окружения DailyJobs__Disabled__0 превращается провайдером в ключ DailyJobs:Disabled:0.
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DailyJobs:Disabled:0"] = "SampleJob",
                ["DailyJobs:Disabled:1"] = "OtherJob",
            })
            .Build();

        var options = new ServiceCollection()
            .AddSingleton<IConfiguration>(configuration)
            .AddOptions<DailyJobsOptions>().BindConfiguration(DailyJobsOptions.SectionName).Services
            .BuildServiceProvider()
            .GetRequiredService<IOptions<DailyJobsOptions>>()
            .Value;

        options.Disabled.ShouldBe(["SampleJob", "OtherJob"]);
    }
}
