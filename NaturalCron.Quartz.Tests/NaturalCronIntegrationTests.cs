using System.Collections.Concurrent;
using System.Threading.Tasks;
using NaturalCron.Quartz;
using Quartz;
using Quartz.Impl;
using Xunit;

public class IntervalJob : IJob
{
    public static ConcurrentBag<DateTimeOffset> Executions = new();

    public Task Execute(IJobExecutionContext context)
    {
        Executions.Add(DateTimeOffset.UtcNow);
        return Task.CompletedTask;
    }
}

public class NaturalCronJob : IJob
{
    public static ConcurrentBag<DateTimeOffset> Executions = new();

    public Task Execute(IJobExecutionContext context)
    {
        Executions.Add(DateTimeOffset.UtcNow);
        return Task.CompletedTask;
    }
}

public class CronJob : IJob
{
    public static ConcurrentBag<DateTimeOffset> Executions = new();

    public Task Execute(IJobExecutionContext context)
    {
        Executions.Add(DateTimeOffset.UtcNow);
        return Task.CompletedTask;
    }
}

public class NaturalCronIntegrationTests
{
    [Fact]
    public async Task CronAndNaturalCronJobs_ShouldFireAtSameTimes()
    {
        const int count = 5;
        
        IntervalJob.Executions = new ConcurrentBag<DateTimeOffset>();
        NaturalCronJob.Executions = new ConcurrentBag<DateTimeOffset>();
        CronJob.Executions = new ConcurrentBag<DateTimeOffset>();

        var schedulerFactory = new StdSchedulerFactory();
        var scheduler = await schedulerFactory.GetScheduler();
        await scheduler.Start();

        var intervalJob = JobBuilder.Create<IntervalJob>().WithIdentity("intervalJob").Build();
        var naturalJob = JobBuilder.Create<NaturalCronJob>().WithIdentity("naturalJob").Build();
        var cronJob = JobBuilder.Create<CronJob>().WithIdentity("cronJob").Build();

        var startAt = DateTimeOffset.Now;
        startAt = startAt.AddSeconds(-startAt.Second); 
        
        var intervalTrigger = TriggerBuilder.Create()
            .WithIdentity("intervalTrigger")
            .WithSimpleSchedule(x => x.WithIntervalInSeconds(5).RepeatForever())
            .StartAt(startAt)
            .Build();

        var naturalTrigger = TriggerBuilder.Create()
            .WithIdentity("naturalTrigger")
            .WithNaturalCronSchedule("every 5 seconds")
            .StartAt(startAt)
            .Build();
        
        var cronTrigger = TriggerBuilder.Create()
            .WithIdentity("cronTrigger")
            .WithCronSchedule("0/5 * * * * ?")
            .StartAt(startAt)
            .Build();

        await scheduler.ScheduleJob(intervalJob, intervalTrigger);
        await scheduler.ScheduleJob(naturalJob, naturalTrigger);
        await scheduler.ScheduleJob(cronJob, cronTrigger);

        // Wait for 5 occurrences (plus a buffer for scheduling delays)
        await Task.Delay(TimeSpan.FromSeconds(count * 5 + 1));

        await scheduler.Shutdown();

        // Compare first 5 executions
        var intervalTimes = IntervalJob.Executions.OrderBy(x => x).Take(count).ToList();
        var naturalTimes = NaturalCronJob.Executions.OrderBy(x => x).Take(count).ToList();
        var cronTimes = CronJob.Executions.OrderBy(x => x).Take(count).ToList();

        Assert.Equal(count, intervalTimes.Count);
        Assert.Equal(count, naturalTimes.Count);
        Assert.Equal(count, cronTimes.Count);

        for (int i = 0; i < count; i++)
        {
            Assert.True(Math.Abs((intervalTimes[i] - naturalTimes[i]).TotalMilliseconds) < 1000, 
                $" IntervalTime: {intervalTimes[i]} vs NaturalTime: {naturalTimes[i]} started at {startAt}");
            Assert.True(Math.Abs((cronTimes[i] - naturalTimes[i]).TotalMilliseconds) < 1000, 
                $" CronTime: {cronTimes[i]} vs NaturalTime: {naturalTimes[i]} started at {startAt}");
        }
    }
}