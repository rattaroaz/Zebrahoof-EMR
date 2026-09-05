using Zebrahoof_EMR.Services;

namespace Zebrahoof.EMR.UnitTests;

public class ChartSummaryWindowsTests
{
    [Fact]
    public void ResolveVisible_UsesDefaultsWhenNothingSaved()
    {
        var visible = ChartSummaryWindows.ResolveVisible(null);
        Assert.Equal(ChartSummaryWindows.DefaultVisible, visible);
    }

    [Fact]
    public void ResolveVisible_KeepsSavedOrderAndDropsUnknown()
    {
        var visible = ChartSummaryWindows.ResolveVisible(["labs", "nope", "problems", "labs"]);
        Assert.Equal(new[] { "labs", "problems" }, visible);
    }

    [Fact]
    public void ResolveVisible_AllowsEmptyLayout()
    {
        Assert.Empty(ChartSummaryWindows.ResolveVisible([]));
    }

    [Fact]
    public void Hidden_AreCatalogMinusVisible()
    {
        var hidden = ChartSummaryWindows.Hidden(["problems", "medications"]);
        Assert.DoesNotContain(hidden, w => w.Key is "problems" or "medications");
        Assert.Contains(hidden, w => w.Key == "orders");
        Assert.Contains(hidden, w => w.Key == "risk");
    }

    [Fact]
    public void Move_InsertsDraggedBeforeTarget()
    {
        Assert.Equal(new[] { "c", "a", "b" }, ChartSummaryWindows.Move(["a", "b", "c"], "c", "a"));
        Assert.Equal(new[] { "b", "a", "c" }, ChartSummaryWindows.Move(["a", "b", "c"], "a", "c"));
    }

    [Fact]
    public void AddAndRemove_UpdateVisibleSet()
    {
        var added = ChartSummaryWindows.Add(["problems"], "orders");
        Assert.Equal(new[] { "problems", "orders" }, added);

        var dup = ChartSummaryWindows.Add(added, "orders");
        Assert.Equal(added, dup);

        var unknown = ChartSummaryWindows.Add(added, "not-a-window");
        Assert.Equal(added, unknown);

        var removed = ChartSummaryWindows.Remove(added, "problems");
        Assert.Equal(new[] { "orders" }, removed);
    }

    [Fact]
    public void ParseLayout_ReadsKeyArrayAndFreeformWindows()
    {
        Assert.Equal(new[] { "labs", "problems" }, ChartSummaryWindows.ParseLayout("""["labs","problems"]"""));
        Assert.Equal(
            new[] { "vitals", "labs" },
            ChartSummaryWindows.ParseLayout("""{"windows":[{"key":"vitals","x":40,"y":90},{"key":"labs","x":10,"y":20}]}"""));
    }

    [Fact]
    public void ParseAndSerialize_RoundTripOrder()
    {
        var original = new[] { "orders", "problems" };
        Assert.Equal(original, ChartSummaryWindows.ParseLayout(ChartSummaryWindows.SerializeLayout(original)));
    }

    [Fact]
    public void ParseLayout_EmptyOrInvalidUsesDefaults()
    {
        Assert.Equal(ChartSummaryWindows.DefaultVisible, ChartSummaryWindows.ParseLayout(null));
        Assert.Equal(ChartSummaryWindows.DefaultVisible, ChartSummaryWindows.ParseLayout("{not-json"));
    }

    [Fact]
    public void Catalog_CoversChartTabsPlusRisk()
    {
        Assert.Contains(ChartSummaryWindows.Catalog, w => w.Key == "risk" && !w.HasTab);
        Assert.Contains(ChartSummaryWindows.Catalog, w => w.Key == "orders" && w.HasTab);
        Assert.Contains(ChartSummaryWindows.Catalog, w => w.Key == "demographics");
    }
}
