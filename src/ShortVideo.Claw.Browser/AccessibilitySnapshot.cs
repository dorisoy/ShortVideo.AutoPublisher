using System.Text.Json;
using Microsoft.Playwright;

namespace ShortVideo.Claw.Browser;

internal static class AccessibilitySnapshot
{
    private static readonly HashSet<string> InteractiveRoles = new(StringComparer.OrdinalIgnoreCase)
    {
        "button", "link", "textbox", "searchbox", "checkbox", "radio", "combobox",
        "listbox", "menuitem", "tab", "slider", "switch", "option"
    };

    public static async Task<(string Snapshot, Dictionary<string, (string Role, string? Name, int Nth)> RefMap)> GetSnapshotAsync(
        IPage page,
        int maxChars = 15000,
        int maxNodes = 500,
        CancellationToken ct = default)
    {
        var cdp = await page.Context.NewCDPSessionAsync(page).ConfigureAwait(false);
        try
        {
            await cdp.SendAsync("Accessibility.enable").ConfigureAwait(false);
            var res = await cdp.SendAsync("Accessibility.getFullAXTree").ConfigureAwait(false);
            var nodes = ParseAxNodes(res);
            var (snapshot, refMap) = FormatSnapshot(nodes, maxNodes, maxChars);
            return (snapshot, refMap);
        }
        finally
        {
            await cdp.DetachAsync().ConfigureAwait(false);
        }
    }

    private static List<AxNode> ParseAxNodes(JsonElement? response)
    {
        var list = new List<AxNode>();
        if (response is null) return list;
        if (!response.Value.TryGetProperty("nodes", out var nodesEl) || nodesEl.ValueKind != JsonValueKind.Array)
            return list;

        foreach (var n in nodesEl.EnumerateArray())
        {
            var role = GetAxValueString(n, "role");
            var name = GetAxValueString(n, "name");
            var value = GetAxValueString(n, "value");
            var nodeId = n.TryGetProperty("nodeId", out var id) ? id.GetString() ?? "" : "";
            var ign = n.TryGetProperty("ignored", out var ig) && ig.GetBoolean();
            list.Add(new AxNode(nodeId, role ?? "", name, value, ign));
        }
        return list;
    }

    private static string? GetAxValueString(JsonElement node, string prop)
    {
        if (!node.TryGetProperty(prop, out var el)) return null;
        if (el.ValueKind == JsonValueKind.String) return el.GetString();
        if (el.TryGetProperty("value", out var v)) return v.GetString();
        return null;
    }

    private static (string Snapshot, Dictionary<string, (string Role, string? Name, int Nth)> RefMap) FormatSnapshot(
        List<AxNode> nodes,
        int maxNodes,
        int maxChars)
    {
        var sb = new System.Text.StringBuilder();
        var refMap = new Dictionary<string, (string Role, string? Name, int Nth)>();
        var keyCount = new Dictionary<string, int>(StringComparer.Ordinal);
        int refCounter = 0, charCount = 0, nodeCount = 0;

        foreach (var n in nodes)
        {
            if (nodeCount >= maxNodes || charCount >= maxChars) break;
            if (n.Ignored || string.IsNullOrWhiteSpace(n.Role)) continue;

            var role = n.Role.ToLowerInvariant();
            if (!InteractiveRoles.Contains(role) && role != "heading" && role != "paragraph")
                continue;

            var name = n.Name?.Trim() ?? "";
            var value = n.Value?.Trim();
            var key = $"{role}\0{name}";
            var nth = keyCount.GetValueOrDefault(key, 0);
            keyCount[key] = nth + 1;

            var isInteractive = InteractiveRoles.Contains(role);
            string? refId = null;
            if (isInteractive)
            {
                refCounter++;
                refId = refCounter.ToString();
                refMap[refId] = (role, string.IsNullOrEmpty(name) ? null : name, nth);
            }

            var line = refId != null
                ? $"[{refId}] {role}" + (string.IsNullOrEmpty(name) ? "" : $" \"{Escape(name)}\"") +
                  (string.IsNullOrEmpty(value) ? "" : $" (value: {Escape(value)})")
                : $"  {role}" + (string.IsNullOrEmpty(name) ? "" : $" \"{Escape(name)}\"");
            sb.AppendLine(line);
            charCount += line.Length + 1;
            nodeCount++;
        }

        if (charCount >= maxChars)
            sb.AppendLine("\n[... truncated]");

        return (sb.ToString().TrimEnd(), refMap);
    }

    private static string Escape(string s) =>
        s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", " ");

    private record AxNode(string NodeId, string Role, string? Name, string? Value, bool Ignored);
}
