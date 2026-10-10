using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FopTools;

/// <summary>Генерация фирменных ФОПов из центрального по правилам fop_rules.json.</summary>
public static class BuildCommand
{
    public static int Run(string repoRoot)
    {
        var rulesPath = Path.Combine(repoRoot, "fop_rules.json");
        if (!File.Exists(rulesPath))
        {
            Console.Error.WriteLine($"Не найден файл правил: {rulesPath}");
            return 1;
        }
        var rules = FopRules.Load(rulesPath);
        var centralPath = Path.Combine(repoRoot, rules.Central);
        if (!File.Exists(centralPath))
        {
            Console.Error.WriteLine($"Не найден центральный ФОП: {centralPath}");
            return 1;
        }

        var central = FopFile.Parse(centralPath);
        Console.WriteLine($"Центральный ФОП: {central.Params.Count} параметров, {central.Groups.Count} групп");
        Console.WriteLine();

        var outDir = Path.Combine(repoRoot, rules.OutputDir);
        Directory.CreateDirectory(outDir);

        foreach (var (fileName, conditions) in rules.Fops)
        {
            var selected = central.Params
                .Where(p => conditions.Any(c => c.Matches(p, central.GroupsById)))
                .ToList();
            var text = FopFile.BuildText(central, selected);
            var outPath = Path.Combine(outDir, fileName);
            FopFile.Write(outPath, text);
            Console.WriteLine($"  fops/{fileName}: {selected.Count} параметров");
        }
        return 0;
    }
}
