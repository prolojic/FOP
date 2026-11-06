using System.Collections.Generic;
using System.IO;
using System.Linq;
using System;

namespace FopTools;

/// <summary>
/// FopTools — валидация центрального ФОПа (общих параметров Revit).
///
/// Запуск из корня репозитория:
///   dotnet run --project FopTools -- check          — проверить центральный ФОП
///   dotnet run --project FopTools -- check &lt;файл&gt; — проверить дополнительные файлы
///
/// После публикации:
///   FopTools check
/// </summary>
public static class Program
{
    public static int Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        if (args.Length == 0)
        {
            Console.WriteLine("Использование: FopTools check [файлы...]");
            return 2;
        }

        // Корень репозитория = текущая директория; можно переопределить через FOP_ROOT
        var repoRoot = Environment.GetEnvironmentVariable("FOP_ROOT") ?? Directory.GetCurrentDirectory();

        return args[0].ToLowerInvariant() switch
        {
            "check" => CheckCommand.Run(repoRoot, args.Skip(1).ToList()),
            _ => Unknown(args[0]),
        };

        static int Unknown(string cmd)
        {
            Console.Error.WriteLine($"Неизвестная команда: {cmd} (ожидается check)");
            return 2;
        }
    }
}
