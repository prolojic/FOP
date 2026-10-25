using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FopTools;

/// <summary>
/// Валидация ФОПов: формат, дубли GUID/имён, ссылки на группы,
/// соответствие производных ФОПов центральному.
/// Код выхода: 0 — ошибок нет, 1 — есть ошибки.
/// </summary>
public static class CheckCommand
{
    public static int Run(string repoRoot, IReadOnlyList<string> targets)
    {
        var rulesPath = Path.Combine(repoRoot, "fop_rules.json");
        var rules = File.Exists(rulesPath) ? FopRules.Load(rulesPath) : null;
        var centralPath = Path.Combine(repoRoot, rules?.Central ?? "FOP_SP_v1.txt");
        var central = FopFile.Parse(centralPath);
        var centralIndex = central.Params.ToDictionary(p => p.Guid);

        var allOk = Validate(centralPath, central, centralIndex,
                             strictDuplicates: true, out _);

        var files = targets.Count > 0
            ? targets.Select(t => Path.GetFullPath(Path.Combine(repoRoot, t))).ToList()
            : Directory.GetFiles(Path.Combine(repoRoot, rules?.OutputDir ?? "fops"), "*.txt").ToList();

        foreach (var file in files)
        {
            if (Path.GetFullPath(file) == Path.GetFullPath(centralPath))
                continue;
            var isStandalone = rules?.Standalone.Contains(Path.GetFileName(file)) == true;
            var fop = FopFile.Parse(file);
            var index = isStandalone ? NoCentralCheck : centralIndex;
            if (!Validate(file, fop, index, strictDuplicates: !isStandalone, out _))
                allOk = false;
        }
        return allOk ? 0 : 1;
    }

    private static readonly IReadOnlyDictionary<string, FopParam> NoCentralCheck =
        new Dictionary<string, FopParam>();

    private static bool Validate(
        string path, FopFile fop, IReadOnlyDictionary<string, FopParam> centralIndex,
        bool strictDuplicates, out int warnings)
    {
        var errors = 0;
        warnings = 0;
        var name = Path.GetFileName(path);
        var seenGuid = new Dictionary<string, int>();
        var seenName = new Dictionary<string, int>();

        foreach (var p in fop.Params)
        {
            if (seenGuid.TryGetValue(p.Guid, out var firstLine))
            {
                errors++;
                Console.WriteLine($"  [ERR] {name} строка {p.Line}: дубль GUID {p.Guid} (уже на строке {firstLine})");
            }
            seenGuid[p.Guid] = p.Line;

            if (seenName.TryGetValue(p.Name, out firstLine))
            {
                var msg = $"строка {p.Line}: дубль имени '{p.Name}' (уже на строке {firstLine}, " +
                          "другой GUID — в Revit это два разных параметра!)";
                if (strictDuplicates) { errors++; Console.WriteLine($"  [ERR] {name}: {msg}"); }
                else { warnings++; Console.WriteLine($"  [WARN] {name}: {msg}"); }
            }
            seenName[p.Name] = p.Line;

            if (!fop.GroupsById.ContainsKey(p.Group))
            {
                errors++;
                Console.WriteLine($"  [ERR] {name} строка {p.Line}: параметр ссылается на несуществующую группу {p.Group}");
            }

            if (centralIndex.Count > 0)
            {
                if (!centralIndex.TryGetValue(p.Guid, out var refParam))
                {
                    errors++;
                    Console.WriteLine($"  [ERR] {name} строка {p.Line}: '{p.Name}' нет в центральном ФОПе");
                }
                else if (refParam.Name != p.Name || refParam.Datatype != p.Datatype)
                {
                    errors++;
                    Console.WriteLine($"  [ERR] {name} строка {p.Line}: '{p.Name}' расходится с центральным ФОПом по имени/типу");
                }
            }
        }

        if (errors == 0)
        {
            var suffix = warnings > 0 ? $", предупреждений: {warnings}" : "";
            Console.WriteLine($"  [OK] {name}: {fop.Params.Count} параметров, {fop.Groups.Count} групп — ошибок нет{suffix}");
        }
        return errors == 0;
    }
}
