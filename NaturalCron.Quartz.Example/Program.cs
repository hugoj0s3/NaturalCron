// See https://aka.ms/new-console-template for more information

using System.Collections.Specialized;
using NaturalCron.Builder;
using NaturalCron.Quartz;
using Quartz;
using Quartz.Impl.Triggers;

Console.WriteLine("Hello, World!");


// you can have base properties
var properties = new NameValueCollection();

// and override values via builder
IScheduler scheduler = await SchedulerBuilder.Create(properties).BuildScheduler();

await scheduler.Start();

var naturalCronTrigger = TriggerBuilder.Create()
    .WithIdentity("NaturalCronTrigger", "group1")
    .WithNaturalCronSchedule("every 10 seconds")
    .Build();

var naturalCronTrigger2 = TriggerBuilder.Create()
    .WithIdentity("NaturalCronTrigger", "group1")
    .WithNaturalCronSchedule(NaturalCronBuilder.Every(10).Seconds().Build())
    .Build();

var naturalCronTrigger3 = TriggerBuilder.Create()
    .WithIdentity("NaturalCronTrigger", "group1")
    .WithNaturalCronSchedule(builder => builder.Every(10).Seconds())
    .Build();

var simpleTrigger = TriggerBuilder.Create()
    .WithIdentity("SimpleTrigger", "group1")
    .WithSimpleSchedule(x => x.WithIntervalInSeconds(10).RepeatForever())
    .Build();

var cronTrigger = TriggerBuilder.Create()
    .WithIdentity("CronTrigger", "group1")
    .WithCronSchedule("0/10 * * * * ?")
    .Build();

var startNowTrigger = TriggerBuilder.Create()
    .WithIdentity("NowTrigger", "group1")
    .StartNow()
    .Build();

await scheduler.ScheduleJob(CreateJob("job1"), naturalCronTrigger);
await scheduler.ScheduleJob(CreateJob("job2"), simpleTrigger);
await scheduler.ScheduleJob(CreateJob("job3"), cronTrigger);
await scheduler.ScheduleJob(CreateJob("job4"), startNowTrigger);

while (true)
{
    var key = Console.ReadKey(true);
    if (key.Key == ConsoleKey.Escape)
    {
        break;
    }
}

IJobDetail CreateJob(string jobName, string groupName = "group1")
{
    var jobDetail = JobBuilder.Create<MyJob>()
        .WithIdentity(jobName, groupName)
        .Build();
    return jobDetail;
}

public class MyJob : IJob
{
    public Task Execute(IJobExecutionContext context)
    {
        var trigger = context.Trigger;
        if (trigger is ICronTrigger)
        {
            Console.WriteLine("Hey - I'm running with Cron! " + DateTime.Now);
            return Task.CompletedTask;
        }
        
        if (trigger is INaturalCronTrigger)
        {
            Console.WriteLine("Hey - I'm running with NaturalCron! " + DateTime.Now);
            return Task.CompletedTask;
        }
        
        if (trigger is ISimpleTrigger)
        {
            Console.WriteLine("Hey - I'm running with Simple! " + DateTime.Now);
            return Task.CompletedTask;
        }
        
        Console.WriteLine("Hey - I'm running! not with Cron, Simple or NaturalCron! " + DateTime.Now);
        
        return Task.CompletedTask;
    }
}

public class MyJob2 : IJob
{
    public Task Execute(IJobExecutionContext context)
    {
        Console.WriteLine("Hey - I'm running with NaturalCron! " + DateTime.Now);
        return Task.CompletedTask;
    }
}

