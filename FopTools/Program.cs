using System.Collections.Generic;
using System.IO;
using System.Linq;
using System;

namespace FopTools;

/// <summary>
/// FopTools — генерация и валидация фирменных ФОПов (общих параметров Revit).
///
/// Запуск из корня репозитория:
///   dotnet run --project FopTools -- build          — пересобрать производные ФОПы
///   dotnet run --project FopTools -- check          — проверить все ФОПы в fops/
///   dotnet run --project FopTools -- check &lt;файл&gt; — проверить один файл
///
/// После публикации:
///   FopTools build | FopTools check
/// </summary>
public static class Program
{
    public static int Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        if (args.Length == 0)
        {
            Console.WriteLine("Использование: FopTools <build|check> [файлы...]");
            return 2;
        }

        // Корень репозитория = текущая директория; можно переопределить через FOP_ROOT
        var repoRoot = Environment.GetEnvironmentVariable("FOP_ROOT") ?? Directory.GetCurrentDirectory();

        return args[0].ToLowerInvariant() switch
        {
            "build" => BuildCommand.Run(repoRoot),
            "check" => CheckCommand.Run(repoRoot, args.Skip(1).ToList()),
            _ => Unknown(args[0]),
        };

        static int Unknown(string cmd)
        {
            Console.Error.WriteLine($"Неизвестная команда: {cmd} (ожидается build или check)");
            return 2;
        }
    }
}
