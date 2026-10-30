using JoinRpg.Common.WebInfrastructure.Job;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace JoinRpg.Common.WebInfrastructure.Test.Job;

public class JobsOptionsTest
{
    private class SampleJob : IJob
    {
        public Task RunOnce(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private class OtherJob : IJob
    {
        public Task RunOnce(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private static JobsOptions Disabled(params string[] names) => new() { Disabled = names };

    [Fact]
    public void EmptyByDefault() => new JobsOptions().IsDisabled(typeof(SampleJob)).ShouldBeFalse();

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
    public void NoUnknownNamesWhenEmpty() => new JobsOptions().GetUnknownNames([typeof(SampleJob)]).ShouldBeEmpty();

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
            .AddOptions<JobsOptions>().BindConfiguration(JobsOptions.SectionName).Services
            .BuildServiceProvider()
            .GetRequiredService<IOptions<JobsOptions>>()
            .Value;

        options.Disabled.ShouldBe(["SampleJob", "OtherJob"]);
    }
}
