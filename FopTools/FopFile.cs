using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace FopTools;

/// <summary>
/// Парсер и сериализатор файла общих параметров Revit (SharedParameters.txt).
/// Формат: UTF-16 LE с BOM, строки через CRLF.
/// </summary>
public sealed class FopFile
{
    public string MetaVersion { get; private set; } = "";
    public string MetaMinVersion { get; private set; } = "";
    public List<FopGroup> Groups { get; } = new();
    public List<FopParam> Params { get; } = new();

    public IReadOnlyDictionary<int, FopGroup> GroupsById =>
        Groups.ToDictionary(g => g.Id);

    public static FopFile Parse(string path)
    {
        var text = File.ReadAllText(path, Encoding.Unicode);
        var fop = new FopFile();
        var lineNo = 0;
        foreach (var raw in text.Split('\n'))
        {
            lineNo++;
            var line = raw.TrimEnd('\r');
            if (line.Length == 0 || line.StartsWith('#') || line.StartsWith('*'))
                continue;

            var parts = line.Split('\t');
            switch (parts[0])
            {
                case "META":
                    fop.MetaVersion = parts[1];
                    fop.MetaMinVersion = parts[2];
                    break;
                case "GROUP":
                    fop.Groups.Add(new FopGroup(int.Parse(parts[1]), parts[2]));
                    break;
                case "PARAM":
                    // PARAM GUID NAME DATATYPE DATACATEGORY GROUP VISIBLE DESCRIPTION USERMODIFIABLE HIDEWHENNOVALUE
                    if (parts.Length < 10)
                        throw new FormatException(
                            $"строка {lineNo}: PARAM имеет {parts.Length - 1} полей вместо 9");
                    fop.Params.Add(new FopParam
                    {
                        Guid = parts[1],
                        Name = parts[2],
                        Datatype = parts[3],
                        Datacategory = parts[4],
                        Group = int.Parse(parts[5]),
                        Visible = parts[6],
                        Description = parts[7],
                        Usermodifiable = parts[8],
                        Hidewhennovalue = parts[9],
                        Line = lineNo,
                    });
                    break;
                default:
                    throw new FormatException($"строка {lineNo}: неизвестная запись '{parts[0]}'");
            }
        }
        return fop;
    }

    /// <summary>Собирает файл из набора параметров с перенумерацией групп под выборку.</summary>
    public static string BuildText(FopFile central, IEnumerable<FopParam> selected)
    {
        var groupsInOrder = new List<int>();
        var mapping = new Dictionary<int, int>();
        foreach (var p in selected)
        {
            if (!mapping.ContainsKey(p.Group))
            {
                mapping[p.Group] = groupsInOrder.Count + 1;
                groupsInOrder.Add(p.Group);
            }
        }

        var sb = new StringBuilder();
        sb.Append("# This is a Revit shared parameter file.\r\n");
        sb.Append("# Do not edit manually.\r\n");
        sb.Append("*META\tVERSION\tMINVERSION\r\n");
        sb.Append($"META\t{central.MetaVersion}\t{central.MetaMinVersion}\r\n");
        sb.Append("*GROUP\tID\tNAME\r\n");
        foreach (var oldId in groupsInOrder)
        {
            var name = central.GroupsById.TryGetValue(oldId, out var g) ? g.Name : "";
            sb.Append($"GROUP\t{mapping[oldId]}\t{name}\r\n");
        }
        sb.Append("*PARAM\tGUID\tNAME\tDATATYPE\tDATACATEGORY\tGROUP\tVISIBLE\tDESCRIPTION\tUSERMODIFIABLE\tHIDEWHENNOVALUE\r\n");
        foreach (var p in selected)
        {
            sb.Append("PARAM\t").Append(p.Guid).Append('\t')
              .Append(p.Name).Append('\t')
              .Append(p.Datatype).Append('\t')
              .Append(p.Datacategory).Append('\t')
              .Append(mapping[p.Group]).Append('\t')
              .Append(p.Visible).Append('\t')
              .Append(p.Description).Append('\t')
              .Append(p.Usermodifiable).Append('\t')
              .Append(p.Hidewhennovalue).Append("\r\n");
        }
        return sb.ToString();
    }

    public static void Write(string path, string text) =>
        File.WriteAllText(path, text, Encoding.Unicode);
}

public sealed class FopGroup
{
    public int Id { get; }
    public string Name { get; }
    public FopGroup(int id, string name) => (Id, Name) = (id, name);
}

public sealed class FopParam
{
    public required string Guid { get; init; }
    public required string Name { get; init; }
    public required string Datatype { get; init; }
    public string Datacategory { get; init; } = "";
    public int Group { get; init; }
    public string Visible { get; init; } = "";
    public string Description { get; init; } = "";
    public string Usermodifiable { get; init; } = "";
    public string Hidewhennovalue { get; init; } = "";
    public int Line { get; init; }
}
