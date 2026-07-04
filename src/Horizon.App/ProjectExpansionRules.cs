using Horizon.App.Models;

namespace Horizon.App;

public enum ProjectSectionKind
{
    Weekly,
    LongTerm
}

public static class ProjectExpansionRules
{
    public static bool IsExpanded(
        HorizonSettings settings,
        ProjectSectionKind sectionKind,
        string projectId)
    {
        var key = BuildKey(sectionKind, projectId);
        return EnsureStateDictionary(settings).TryGetValue(key, out var isExpanded) && isExpanded;
    }

    public static void SetExpanded(
        HorizonSettings settings,
        ProjectSectionKind sectionKind,
        string projectId,
        bool isExpanded)
    {
        var key = BuildKey(sectionKind, projectId);
        EnsureStateDictionary(settings)[key] = isExpanded;
    }

    public static bool Toggle(
        HorizonSettings settings,
        ProjectSectionKind sectionKind,
        string projectId)
    {
        var next = !IsExpanded(settings, sectionKind, projectId);
        SetExpanded(settings, sectionKind, projectId, next);
        return next;
    }

    public static bool RemoveProject(HorizonSettings settings, string projectId)
    {
        var states = EnsureStateDictionary(settings);
        var weeklyRemoved = states.Remove(
            BuildKey(ProjectSectionKind.Weekly, projectId));
        var longTermRemoved = states.Remove(
            BuildKey(ProjectSectionKind.LongTerm, projectId));
        return weeklyRemoved || longTermRemoved;
    }

    public static bool PruneUnknownProjects(
        HorizonSettings settings,
        IEnumerable<string> knownProjectIds)
    {
        var states = EnsureStateDictionary(settings);
        var knownKeys = knownProjectIds
            .SelectMany(projectId => new[]
            {
                BuildKey(ProjectSectionKind.Weekly, projectId),
                BuildKey(ProjectSectionKind.LongTerm, projectId)
            })
            .ToHashSet(StringComparer.Ordinal);

        var staleKeys = states.Keys
            .Where(key => !knownKeys.Contains(key))
            .ToList();

        foreach (var key in staleKeys)
        {
            states.Remove(key);
        }

        return staleKeys.Count > 0;
    }

    private static string BuildKey(ProjectSectionKind sectionKind, string projectId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectId);
        var prefix = sectionKind switch
        {
            ProjectSectionKind.Weekly => "weekly",
            ProjectSectionKind.LongTerm => "longterm",
            _ => throw new ArgumentOutOfRangeException(
                nameof(sectionKind),
                sectionKind,
                "Unknown project section kind.")
        };
        return $"{prefix}:{projectId}";
    }

    private static Dictionary<string, bool> EnsureStateDictionary(HorizonSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        return settings.ProjectExpansionStates ??= [];
    }
}
