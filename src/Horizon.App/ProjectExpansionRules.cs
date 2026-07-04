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
        return settings.ProjectExpansionStates.TryGetValue(
            BuildKey(sectionKind, projectId),
            out var isExpanded) && isExpanded;
    }

    public static void SetExpanded(
        HorizonSettings settings,
        ProjectSectionKind sectionKind,
        string projectId,
        bool isExpanded)
    {
        settings.ProjectExpansionStates[BuildKey(sectionKind, projectId)] = isExpanded;
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
        var weeklyRemoved = settings.ProjectExpansionStates.Remove(
            BuildKey(ProjectSectionKind.Weekly, projectId));
        var longTermRemoved = settings.ProjectExpansionStates.Remove(
            BuildKey(ProjectSectionKind.LongTerm, projectId));
        return weeklyRemoved || longTermRemoved;
    }

    public static bool PruneUnknownProjects(
        HorizonSettings settings,
        IEnumerable<string> knownProjectIds)
    {
        var knownKeys = knownProjectIds
            .SelectMany(projectId => new[]
            {
                BuildKey(ProjectSectionKind.Weekly, projectId),
                BuildKey(ProjectSectionKind.LongTerm, projectId)
            })
            .ToHashSet(StringComparer.Ordinal);

        var staleKeys = settings.ProjectExpansionStates.Keys
            .Where(key => !knownKeys.Contains(key))
            .ToList();

        foreach (var key in staleKeys)
        {
            settings.ProjectExpansionStates.Remove(key);
        }

        return staleKeys.Count > 0;
    }

    private static string BuildKey(ProjectSectionKind sectionKind, string projectId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectId);
        var prefix = sectionKind == ProjectSectionKind.Weekly ? "weekly" : "longterm";
        return $"{prefix}:{projectId}";
    }
}
