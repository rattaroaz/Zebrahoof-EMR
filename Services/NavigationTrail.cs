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
        _current = Relative(navigation.Uri);
        navigation.LocationChanged += OnLocationChanged;
    }

    public string? PreviousPath { get; private set; }

    public void Dispose() => _navigation.LocationChanged -= OnLocationChanged;

    private void OnLocationChanged(object? sender, LocationChangedEventArgs args)
    {
        var next = Relative(args.Location);
        if (string.Equals(next, _current, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        PreviousPath = _current;
        _current = next;
    }

    private static string Relative(string uri)
    {
        if (Uri.TryCreate(uri, UriKind.Absolute, out var absolute))
        {
            return absolute.PathAndQuery;
        }

        return uri;
    }
}
