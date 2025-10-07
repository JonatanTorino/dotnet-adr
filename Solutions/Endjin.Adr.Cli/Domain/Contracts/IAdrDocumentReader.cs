using System.Threading;
using System.Threading.Tasks;

using Endjin.Adr.Cli.Domain.Models;

namespace Endjin.Adr.Cli.Domain.Contracts;

/// <summary>
/// Loads ADR markdown content from storage.
/// </summary>
public interface IAdrDocumentReader
{
    Task<string> ReadAsync(AdrFileReference reference, CancellationToken cancellationToken = default);
}
