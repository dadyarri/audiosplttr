using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using AudioSplttr.Models;
using Unidecode.NET;

namespace AudioSplttr.Splitter;

public class Injector
{
    public static async Task Inject(InjectorOptions configuration)
    {
        var definitions = new List<string> {"init:"};
        var scriptDir = Directory.GetParent(configuration.FilePath);
        var scriptDirPath = scriptDir?.ToString();

        foreach (var file in Directory.GetFiles(configuration.ChunksPath, "*.*", SearchOption.AllDirectories))
        {
            definitions.Add(
                $"""    $ {Path.GetFileNameWithoutExtension(file).Replace(" ", "").Unidecode()} = "{Path.GetRelativePath(scriptDirPath, file).Replace("""\""", "/")}" """
            );
        }

        await using (var writer = File.AppendText(configuration.FilePath))
        {
            await writer.WriteLineAsync();
            await writer.WriteLineAsync("#### Inserted with AudioSplttr (c) 2022 dadyarri");
        }

        await File.AppendAllLinesAsync(configuration.FilePath, definitions);
    }
}