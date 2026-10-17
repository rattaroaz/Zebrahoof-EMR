using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;

namespace Zebrahoof_EMR.Services;

/// <summary>Remembers the previous in-app path so "Keep open" can return there.</summary>
public sealed class NavigationTrail : IDisposable
{
    private readonly NavigationManager _navigation;
    private string _current;

    public NavigationTrail(NavigationManager navigation)
    {
        _navigation = navigation;
        _current = ToRelative(navigation.Uri);
        navigation.LocationChanged += OnLocationChanged;
    }

    public string? PreviousPath { get; private set; }

    public static bool IsPatientChart(string? relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            return false;
        }

        var path = relativePath.Split('?', 2)[0].Trim('/');
        var parts = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length >= 2
            && parts[0].Equals("patients", StringComparison.OrdinalIgnoreCase)
            && int.TryParse(parts[1], out _);
    }

    public static string ToRelative(string uri)
    {
        if (Uri.TryCreate(uri, UriKind.Absolute, out var absolute))
        {
            return absolute.PathAndQuery;
        }

        return uri.StartsWith('/') ? uri : "/" + uri;
    }

    public void Dispose() => _navigation.LocationChanged -= OnLocationChanged;

    private void OnLocationChanged(object? sender, LocationChangedEventArgs args)
    {
        var next = ToRelative(args.Location);
        if (string.Equals(next, _current, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        PreviousPath = _current;
        _current = next;
    }
}
