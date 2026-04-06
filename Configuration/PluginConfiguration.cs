using MediaBrowser.Model.Plugins;
using System.Collections.Generic;

namespace Jellyfin.Plugin.KidsTagger;

public class PluginConfiguration : BasePluginConfiguration
{
    public List<string> EnabledLibraries { get; set; } = new();

    public List<string> ForceKidsLibraries { get; set; } = new();

    public List<string> RemoveKidsLibraries { get; set; } = new();

    public bool DryRun { get; set; } = true;

    // Ordner für Logdateien
    public string LogFolder { get; set; } = "/mnt/sda/glftpd/site/HIDDEN";
}
