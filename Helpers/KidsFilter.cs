using MediaBrowser.Controller.Entities;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;

namespace Jellyfin.Plugin.KidsTagger.Helpers;

public static class KidsFilter
{
    private static readonly string[] AllowedRatings =
    [
        "FSK-0",
        "FSK-6",
        "FSK-12"
    ];

    public static bool Matches(BaseItem item, ILogger logger)
    {
        if (item == null)
            return false;

        var rating = item.OfficialRating ?? string.Empty;
        var genres = item.Genres ?? Array.Empty<string>();
        var tags = item.Tags ?? Array.Empty<string>();

        if (string.IsNullOrWhiteSpace(rating))
            return false;

        if (!AllowedRatings.Contains(rating, StringComparer.OrdinalIgnoreCase))
            return false;

        if (tags.Contains("Kids", StringComparer.OrdinalIgnoreCase))
            return false;

        if (genres.Length == 0)
            return false;

        var hasFamily = genres.Any(g =>
            g.Contains("family", StringComparison.OrdinalIgnoreCase));

        var hasKids = genres.Any(g =>
            g.Contains("kids", StringComparison.OrdinalIgnoreCase));

        var hasAnimation = genres.Any(g =>
            g.Contains("animation", StringComparison.OrdinalIgnoreCase));

        if (rating.Equals("FSK-0", StringComparison.OrdinalIgnoreCase) ||
            rating.Equals("FSK-6", StringComparison.OrdinalIgnoreCase))
        {
            return hasFamily || hasKids || hasAnimation;
        }

        if (rating.Equals("FSK-12", StringComparison.OrdinalIgnoreCase))
        {
            return hasFamily || hasKids;
        }

        return false;
    }
}
