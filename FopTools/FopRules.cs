using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace FopTools;

/// <summary>Условие отбора параметра в фирменный ФОП.</summary>
public sealed class RuleCondition
{
    public string? GroupPrefix { get; set; }
    public string? GroupExact { get; set; }
    public string? NamePrefix { get; set; }

    public bool Matches(FopParam p, IReadOnlyDictionary<int, FopGroup> groups)
    {
        var gname = groups.TryGetValue(p.Group, out var g) ? g.Name : "";
        if (GroupPrefix is not null) return gname.StartsWith(GroupPrefix, StringComparison.Ordinal);
        if (GroupExact is not null) return gname == GroupExact;
        if (NamePrefix is not null) return p.Name.StartsWith(NamePrefix, StringComparison.Ordinal);
        return false;
    }
}

public sealed class FopRules
{
    public required string Central { get; set; }
    public required string OutputDir { get; set; }
    public List<string> Standalone { get; set; } = new();
    public Dictionary<string, List<RuleCondition>> Fops { get; set; } = new();

    public static FopRules Load(string path)
    {
        var json = File.ReadAllText(path);
        var rules = JsonSerializer.Deserialize<FopRules>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? throw new InvalidDataException($"Не удалось разобрать {path}");
        return rules;
    }
}
