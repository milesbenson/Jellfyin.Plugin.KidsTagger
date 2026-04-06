using System;
using System.Collections.Generic;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;

namespace Jellyfin.Plugin.KidsTagger;

public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
{
    private readonly ILibraryManager _libraryManager;

    public static Plugin? Instance { get; private set; }

    public override string Name => "Kids Tagger";

    public override Guid Id => Guid.Parse("b2d7c4f3-3b9e-4b4a-8f5c-3f7c2f5f6c12");

    public Plugin(
        IApplicationPaths applicationPaths,
        IXmlSerializer xmlSerializer,
        ILibraryManager libraryManager)
        : base(applicationPaths, xmlSerializer)
    {
        _libraryManager = libraryManager;
        Instance = this;
    }

    public IEnumerable<PluginPageInfo> GetPages()
    {
        return new[]
        {
            new PluginPageInfo
            {
                Name = "kidstagger",
                EmbeddedResourcePath = "Jellyfin.Plugin.KidsTagger.Web.configPage.html"
            }
        };
    }

    public List<string> GetAvailableLibraryNames()
    {
        return _libraryManager.GetVirtualFolders()
            .Select(x => x.Name)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
