using System.Text.Json;
using System.Text.Json.Serialization;

namespace Horizon.App.Models;

public enum ProjectStatus
{
    Active,
    OnHold,
    Completed
}

public enum WeeklyTaskStatus
{
    Todo,
    InProgress,
    Done
}

public enum LongTermTaskStatus
{
    Planned,
    Active,
    Paused,
    Completed
}

public sealed class HorizonDataFile
{
    public HorizonMeta Meta { get; set; } = new();
    public HorizonSettings Settings { get; set; } = new();
    public List<ProjectItem> Projects { get; set; } = [];
    public List<WeeklyTaskItem> WeeklyTasks { get; set; } = [];
    public List<LongTermTaskItem> LongTermTasks { get; set; } = [];
}

public sealed class HorizonMeta
{
    public int Version { get; set; } = 1;
    public DateTime LastSavedAt { get; set; } = DateTime.UtcNow;
}

public sealed class HorizonSettings
{
    public List<string> WeeklyProjectNames { get; set; } = [];
    public List<string> LongTermProjectNames { get; set; } = [];
    public double CollapsedButtonTop { get; set; } = 160;
    public bool IsPinned { get; set; }
    public bool StartWithWindows { get; set; } = true;
    public Dictionary<string, bool> ProjectExpansionStates { get; set; } = [];
}

public sealed class ProjectItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = string.Empty;
    public ProjectStatus Status { get; set; } = ProjectStatus.Active;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public sealed class WeeklyTaskItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string ProjectId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public WeeklyTaskStatus Status { get; set; } = WeeklyTaskStatus.Todo;
    public DateTime WeekStartDate { get; set; } = DateTime.Today;
    public DateTime? DueDate { get; set; }
    public string? Notes { get; set; }
    public int Progress { get; set; }
    public List<TaskAnnotation> Annotations { get; set; } = [];
    public DateTime? CompletedAt { get; set; }
    public bool Archived { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public sealed class LongTermTaskItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string ProjectId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public LongTermTaskStatus Status { get; set; } = LongTermTaskStatus.Planned;
    public DateTime StartDate { get; set; } = DateTime.Today;
    public DateTime EndDate { get; set; } = DateTime.Today;
    public string? Notes { get; set; }
    public int Progress { get; set; }
    public List<TaskAnnotation> Annotations { get; set; } = [];
    public DateTime? CompletedAt { get; set; }
    public bool Archived { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public sealed class TaskAnnotation
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public static class JsonOptions
{
    public static readonly JsonSerializerOptions Default = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };
}
