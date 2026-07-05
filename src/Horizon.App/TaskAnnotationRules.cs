using Horizon.App.Models;

namespace Horizon.App;

internal static class TaskAnnotationRules
{
    internal const int MaxLength = 500;

    internal static bool IsValid(string? content)
    {
        var cleaned = Clean(content);
        return cleaned.Length is > 0 and <= MaxLength;
    }

    internal static string Clean(string? content) => (content ?? string.Empty).Trim();

    internal static string FormatLocalTime(DateTime localTime, DateTime localNow)
    {
        if (localTime.Date == localNow.Date)
        {
            return $"今天 {localTime:HH:mm}";
        }

        if (localTime.Date == localNow.Date.AddDays(-1))
        {
            return $"昨天 {localTime:HH:mm}";
        }

        return localTime.ToString("yyyy-MM-dd HH:mm");
    }

    internal static TaskAnnotation Add(
        ICollection<TaskAnnotation> annotations,
        string content,
        DateTime nowUtc)
    {
        var annotation = new TaskAnnotation
        {
            Content = Clean(content),
            CreatedAt = nowUtc,
            UpdatedAt = nowUtc
        };
        annotations.Add(annotation);
        return annotation;
    }

    internal static bool Update(
        IEnumerable<TaskAnnotation> annotations,
        string annotationId,
        string content,
        DateTime nowUtc)
    {
        var annotation = annotations.FirstOrDefault(item => item.Id == annotationId);
        if (annotation is null)
        {
            return false;
        }

        annotation.Content = Clean(content);
        annotation.UpdatedAt = nowUtc;
        return true;
    }

    internal static bool Delete(ICollection<TaskAnnotation> annotations, string annotationId)
    {
        var annotation = annotations.FirstOrDefault(item => item.Id == annotationId);
        return annotation is not null && annotations.Remove(annotation);
    }
}
