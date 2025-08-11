using FluentAssertions;
using Quartz;

namespace NaturalCron.Quartz.Tests;

public class NaturalCronTriggerUnitTests
{
    [Theory]
    [InlineData("0 0 * * * ?", "hourly at 0 sec at 0 min")]
    [InlineData("0 0 0 * * ?", "every day at 00:00:00")]
    [InlineData("0 0 0 3 * ?", "every month on 3rd at 00:00:00")]
    [InlineData("0 30 16 1/2 * ?", "every 2 days at 16:30:00")]
    public void CronTriggerXNaturalCronTrigger_ShouldBeEqual(string cronExpression, string naturalCronExpression)
    {
        var baseTime = new DateTimeOffset(new DateOnly(2025, 8, 1), new TimeOnly(13, 00), DateTimeOffset.Now.Offset);
        var cronBaseTime = baseTime;
        var naturalCronBaseTime = baseTime;
        
        var cronTrigger = TriggerBuilder.Create()
            .WithIdentity("CronTrigger", "group1")
            .WithCronSchedule(cronExpression)
            .StartAt(baseTime)
            .Build(); 
        
        var naturalCronTrigger = TriggerBuilder.Create()
            .WithIdentity("NaturalCronTrigger", "group1")
            .WithNaturalCronSchedule(naturalCronExpression)
            .StartAt(baseTime)
            .Build();
        
        for (int i = 0; i < 10; i++)
        {
            var cronNextTime = cronTrigger.GetFireTimeAfter(cronBaseTime);
            var naturalCronNextTime = naturalCronTrigger.GetFireTimeAfter(naturalCronBaseTime);
            
            cronNextTime.Should().NotBeNull();
            naturalCronNextTime.Should().NotBeNull();
            
            cronNextTime.Should().Be(naturalCronNextTime, $" cronNextTime: {cronNextTime}, naturalCronNextTime: {naturalCronNextTime}, Iteration: {i}");
            
            cronBaseTime = naturalCronNextTime.Value;
            naturalCronBaseTime = naturalCronNextTime.Value;
        }
    }
}
