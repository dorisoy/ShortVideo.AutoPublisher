using Microsoft.Playwright;

namespace ShortVideo.Claw.Browser;

internal static class RefResolver
{
    private static readonly Dictionary<string, AriaRole> RoleMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["button"] = AriaRole.Button,
        ["link"] = AriaRole.Link,
        ["textbox"] = AriaRole.Textbox,
        ["searchbox"] = AriaRole.Searchbox,
        ["checkbox"] = AriaRole.Checkbox,
        ["radio"] = AriaRole.Radio,
        ["combobox"] = AriaRole.Combobox,
        ["listbox"] = AriaRole.Listbox,
        ["menuitem"] = AriaRole.Menuitem,
        ["tab"] = AriaRole.Tab,
        ["slider"] = AriaRole.Slider,
        ["switch"] = AriaRole.Switch,
        ["option"] = AriaRole.Option
    };

    public static ILocator? GetLocator(IPage page, string role, string? name, int nth)
    {
        if (!RoleMap.TryGetValue(role, out var ariaRole))
            return null;
        var opts = new PageGetByRoleOptions();
        if (!string.IsNullOrWhiteSpace(name))
            opts.Name = name;
        var loc = page.GetByRole(ariaRole, opts);
        return nth > 0 ? loc.Nth(nth) : loc.First;
    }

    public static async Task<ILocator?> GetLocatorIncludingFramesAsync(IPage page, string role, string? name, int nth, CancellationToken ct = default)
    {
        if (!RoleMap.TryGetValue(role, out var ariaRole))
            return null;
        var opts = new PageGetByRoleOptions();
        if (!string.IsNullOrWhiteSpace(name))
            opts.Name = name;

        var mainLoc = page.GetByRole(ariaRole, opts);
        var mainTarget = nth > 0 ? mainLoc.Nth(nth) : mainLoc.First;
        try
        {
            if (await mainTarget.CountAsync().ConfigureAwait(false) > 0)
                return mainTarget;
        }
        catch { }

        foreach (var frame in CollectFrames(page.MainFrame))
        {
            if (frame == page.MainFrame) continue;
            try
            {
                var roleName = ariaRole.ToString().ToLowerInvariant();
                var sel = string.IsNullOrWhiteSpace(opts.Name)
                    ? $"role={roleName}"
                    : $"role={roleName}[name=\"{opts.Name!.Replace("\\", "\\\\").Replace("\"", "\\\"")}\"]";
                var target = (nth > 0 ? frame.Locator(sel).Nth(nth) : frame.Locator(sel).First);
                if (await target.CountAsync().ConfigureAwait(false) > 0)
                    return target;
            }
            catch { }
        }
        return null;
    }

    private static List<IFrame> CollectFrames(IFrame root)
    {
        var list = new List<IFrame> { root };
        foreach (var c in root.ChildFrames)
            list.AddRange(CollectFrames(c));
        return list;
    }
}
