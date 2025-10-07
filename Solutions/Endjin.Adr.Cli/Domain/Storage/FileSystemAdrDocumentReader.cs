using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Endjin.Adr.Cli.Domain.Contracts;
using Endjin.Adr.Cli.Domain.Models;

namespace Endjin.Adr.Cli.Domain.Storage;

/// <summary>
/// Reads ADR content from the file system.
/// </summary>
public class FileSystemAdrDocumentReader : IAdrDocumentReader
{
    public Task<string> ReadAsync(AdrFileReference reference, CancellationToken cancellationToken = default)
    {
        return File.ReadAllTextAsync(reference.FullPath, cancellationToken);
    }
}
