using Jellyfin.Data.Enums;
using Jellyfin.Plugin.KidsTagger.Helpers;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Querying;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Jellyfin.Plugin.KidsTagger.Tasks;

public class KidsTaggerTask : IScheduledTask
{
    private readonly ILibraryManager _libraryManager;
    private readonly ILogger<KidsTaggerTask> _logger;

    public KidsTaggerTask(
        ILibraryManager libraryManager,
        ILogger<KidsTaggerTask> logger)
    {
        _libraryManager = libraryManager;
        _logger = logger;
    }

    public string Name => "Kids Tagger Scan";
    public string Key => "KidsTaggerScan";
    public string Description => "Automatically manages Kids tags";
    public string Category => "Library";

    private static string GetReleaseFolder(BaseItem item)
    {
        if (string.IsNullOrWhiteSpace(item.Path))
            return "<unknown>";

        try
        {
            var dir = Path.GetDirectoryName(item.Path);

            if (string.IsNullOrWhiteSpace(dir))
                return "<unknown>";

            return new DirectoryInfo(dir).Name;
        }
        catch
        {
            return "<unknown>";
        }
    }

    public async Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
    {
        var config = Plugin.Instance!.Configuration;

        var selectedLibraries = new HashSet<string>(
            config.EnabledLibraries ?? [],
            StringComparer.OrdinalIgnoreCase);

        var forceLibraries = new HashSet<string>(
            config.ForceKidsLibraries ?? [],
            StringComparer.OrdinalIgnoreCase);

        var removeLibraries = new HashSet<string>(
            config.RemoveKidsLibraries ?? [],
            StringComparer.OrdinalIgnoreCase);

        string? logFile = null;
        int added = 0;
        int removed = 0;

        if (!config.DryRun)
        {
            Directory.CreateDirectory(config.LogFolder);

            logFile = Path.Combine(
                config.LogFolder,
                $"kids-tagger-{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.txt");

            File.AppendAllText(
                logFile!,
                $"=== KidsTagger Run {DateTime.Now:yyyy-MM-dd HH:mm:ss} ==={Environment.NewLine}{Environment.NewLine}");
        }

        var libraries = _libraryManager.GetVirtualFolders()
            .Where(l =>
                selectedLibraries.Contains(l.Name) ||
                forceLibraries.Contains(l.Name) ||
                removeLibraries.Contains(l.Name) ||
                (!selectedLibraries.Any() && !forceLibraries.Any() && !removeLibraries.Any()))
            .ToList();

        _logger.LogInformation(
            "KidsTagger: scanning libraries -> {Libraries}",
            string.Join(", ", libraries.Select(l => l.Name)));

        var totalCandidates = 0;
        var processed = 0;

        foreach (var library in libraries)
        {
            var query = new InternalItemsQuery
            {
                IncludeItemTypes =
                [
                    BaseItemKind.Movie,
                    BaseItemKind.Series,
                    BaseItemKind.BoxSet
                ],
                AncestorIds =
                [
                    Guid.Parse(library.ItemId)
                ]
            };

            var items = _libraryManager.GetItemList(query);

            foreach (var item in items)
            {
                cancellationToken.ThrowIfCancellationRequested();

                bool add = false;
                bool remove = false;

                var hasKidsTag = item.Tags != null &&
                                 item.Tags.Contains("Kids", StringComparer.OrdinalIgnoreCase);

                if (removeLibraries.Contains(library.Name))
                {
                    if (hasKidsTag)
                        remove = true;
                }
                else if (forceLibraries.Contains(library.Name))
                {
                    if (!hasKidsTag)
                        add = true;
                }
                else
                {
                    if (KidsFilter.Matches(item, _logger))
                        add = true;
                }

                if (!add && !remove)
                    continue;

                totalCandidates++;

                if (config.DryRun)
                {
                    if (add)
                    {
                        _logger.LogInformation(
                            "DRY-RUN ADD -> {Name} | Library:{Library}",
                            item.Name,
                            library.Name);
                    }
                    else
                    {
                        _logger.LogInformation(
                            "DRY-RUN REMOVE -> {Name} | Library:{Library}",
                            item.Name,
                            library.Name);
                    }

                    continue;
                }

                var tags = item.Tags?.ToList() ?? new List<string>();
                var releaseFolder = GetReleaseFolder(item);

                if (add)
                {
                    tags.Add("Kids");
                    added++;

                    File.AppendAllText(
                        logFile!,
                        $"ADD    | {library.Name,-10} | {item.Name} | {releaseFolder}{Environment.NewLine}");
                }

                if (remove)
                {
                    tags.RemoveAll(t =>
                        t.Equals("Kids", StringComparison.OrdinalIgnoreCase));

                    removed++;

                    File.AppendAllText(
                        logFile!,
                        $"REMOVE | {library.Name,-10} | {item.Name} | {releaseFolder}{Environment.NewLine}");
                }

                item.Tags = tags.ToArray();

                await _libraryManager.UpdateItemAsync(
                    item,
                    item.GetParent(),
                    ItemUpdateType.MetadataEdit,
                    cancellationToken);

                processed++;

                if (totalCandidates > 0)
                    progress.Report(processed * 100d / totalCandidates);
            }
        }

        if (!config.DryRun && logFile != null)
        {
            File.AppendAllText(
                logFile,
                $"{Environment.NewLine}=== Summary ==={Environment.NewLine}" +
                $"Added:   {added}{Environment.NewLine}" +
                $"Removed: {removed}{Environment.NewLine}");
        }

        _logger.LogInformation(
            "KidsTagger: {Count} item actions found (DryRun={DryRun})",
            totalCandidates,
            config.DryRun);

        _logger.LogInformation("KidsTagger: Scan finished");
    }

    public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
        => Array.Empty<TaskTriggerInfo>();
}
