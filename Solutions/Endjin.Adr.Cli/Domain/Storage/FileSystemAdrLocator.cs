using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using Endjin.Adr.Cli.Domain.Contracts;
using Endjin.Adr.Cli.Domain.Models;

namespace Endjin.Adr.Cli.Domain.Storage;

/// <summary>
/// Locates ADR files on disk based on the record naming convention.
/// </summary>
public class FileSystemAdrLocator : IAdrFileLocator
{
    private static readonly Regex AdrFilePattern = new("^(?<number>\\d{4})-.*\\.md$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public Task<IReadOnlyList<AdrFileReference>> LocateAsync(AdrRepositoryOptions options, CancellationToken cancellationToken = default)
    {
        List<AdrFileReference> results = new();

        if (!Directory.Exists(options.RootPath))
        {
            return Task.FromResult<IReadOnlyList<AdrFileReference>>(results);
        }

        IEnumerable<string> files = Directory.EnumerateFiles(options.RootPath, "*.md", options.SearchScope);

        foreach (string file in files)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            string fileName = Path.GetFileName(file);
            Match match = AdrFilePattern.Match(fileName);

            if (!match.Success)
            {
                continue;
            }

            if (int.TryParse(match.Groups["number"].Value, out int recordNumber))
            {
                results.Add(new AdrFileReference(recordNumber, fileName, file));
            }
        }

        IReadOnlyList<AdrFileReference> ordered = results
            .OrderBy(r => r.RecordNumber)
            .ThenBy(r => r.FileName)
            .ToList();

        return Task.FromResult(ordered);
    }
}
