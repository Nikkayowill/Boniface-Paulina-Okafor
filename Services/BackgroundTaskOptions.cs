namespace Okafor_.NET.Services;

public sealed class BackgroundTaskOptions
{
    public const string SectionName = "BackgroundTasks";

    public bool AppointmentRemindersEnabled { get; set; } = true;

    public int AppointmentReminderIntervalMinutes { get; set; } = 60;

    public bool PushSubscriptionCleanupEnabled { get; set; } = true;
}
