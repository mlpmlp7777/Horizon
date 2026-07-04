using System.IO;
using System.Text.Json;
using Horizon.App.Models;

namespace Horizon.App.Services;

public sealed class HorizonDataStore
{
    private readonly string _dataDirectory;
    private readonly string _dataFilePath;

    public HorizonDataStore()
        : this(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Horizon"))
    {
    }

    internal HorizonDataStore(string baseDirectory)
    {
        _dataDirectory = Path.Combine(baseDirectory, "data");
        _dataFilePath = Path.Combine(_dataDirectory, "horizon-data.json");
    }

    public string DataFilePath => _dataFilePath;

    public HorizonDataFile Load()
    {
        Directory.CreateDirectory(_dataDirectory);

        if (!File.Exists(_dataFilePath))
        {
            var empty = CreateEmpty();
            Save(empty);
            return empty;
        }

        try
        {
            var json = File.ReadAllText(_dataFilePath);
            var data = JsonSerializer.Deserialize<HorizonDataFile>(json, JsonOptions.Default);
            var normalized = data ?? CreateEmpty();
            foreach (var task in normalized.WeeklyTasks)
            {
                task.Annotations ??= [];
            }

            foreach (var task in normalized.LongTermTasks)
            {
                task.Annotations ??= [];
            }

            return normalized;
        }
        catch
        {
            return CreateEmpty();
        }
    }

    public void Save(HorizonDataFile data)
    {
        Directory.CreateDirectory(_dataDirectory);
        data.Meta.LastSavedAt = DateTime.UtcNow;

        var json = JsonSerializer.Serialize(data, JsonOptions.Default);
        File.WriteAllText(_dataFilePath, json);
    }

    private static HorizonDataFile CreateEmpty() => new();
}
