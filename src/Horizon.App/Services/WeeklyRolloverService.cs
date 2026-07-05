using Horizon.App.Models;

namespace Horizon.App.Services;

internal static class WeeklyRolloverService
{
    internal static bool Reconcile(
        HorizonDataFile data,
        DateTime localToday,
        DateTime nowUtc)
    {
        var changed = false;
        var currentWeekStart = GetStartOfWeek(localToday);

        foreach (var task in data.WeeklyTasks)
        {
            changed |= ReconcileCompletion(
                task.Status == WeeklyTaskStatus.Done,
                task.UpdatedAt,
                () => task.CompletedAt,
                value => task.CompletedAt = value,
                nowUtc);

            if (task.Status != WeeklyTaskStatus.Done && task.WeekStartDate.Date < currentWeekStart)
            {
                task.WeekStartDate = currentWeekStart;
                task.UpdatedAt = nowUtc;
                changed = true;
            }
        }

        foreach (var task in data.LongTermTasks)
        {
            changed |= ReconcileCompletion(
                task.Status == LongTermTaskStatus.Completed,
                task.UpdatedAt,
                () => task.CompletedAt,
                value => task.CompletedAt = value,
                nowUtc);
        }

        return changed;
    }

    internal static bool IsWeeklyInCurrentView(WeeklyTaskItem task, DateTime localToday)
    {
        return task.Status != WeeklyTaskStatus.Done ||
               task.CompletedAt is null ||
               task.CompletedAt.Value.ToLocalTime() >= GetStartOfWeek(localToday);
    }

    internal static bool IsLongTermInCurrentView(LongTermTaskItem task, DateTime localToday)
    {
        return task.Status != LongTermTaskStatus.Completed ||
               task.CompletedAt is null ||
               task.CompletedAt.Value.ToLocalTime() >= GetStartOfWeek(localToday);
    }

    internal static DateTime GetLongTermHistoryWeek(LongTermTaskItem task)
    {
        return task.CompletedAt is null
            ? DateTime.MinValue
            : GetStartOfWeek(task.CompletedAt.Value.ToLocalTime());
    }

    internal static DateTime GetStartOfWeek(DateTime date)
    {
        var localDate = date.Date;
        var delta = ((int)localDate.DayOfWeek + 6) % 7;
        return localDate.AddDays(-delta);
    }

    private static bool ReconcileCompletion(
        bool isCompleted,
        DateTime updatedAt,
        Func<DateTime?> getCompletedAt,
        Action<DateTime?> setCompletedAt,
        DateTime nowUtc)
    {
        var completedAt = getCompletedAt();
        if (isCompleted && completedAt is null)
        {
            var migrated = updatedAt == default
                ? nowUtc
                : updatedAt.Kind == DateTimeKind.Utc
                    ? updatedAt
                    : updatedAt.ToUniversalTime();
            setCompletedAt(migrated);
            return true;
        }

        if (!isCompleted && completedAt is not null)
        {
            setCompletedAt(null);
            return true;
        }

        return false;
    }
}
