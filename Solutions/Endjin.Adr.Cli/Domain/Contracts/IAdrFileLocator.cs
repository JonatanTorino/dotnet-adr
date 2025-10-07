using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Endjin.Adr.Cli.Domain.Models;

namespace Endjin.Adr.Cli.Domain.Contracts;

/// <summary>
/// Discovers ADR markdown files within a repository.
/// </summary>
public interface IAdrFileLocator
{
    Task<IReadOnlyList<AdrFileReference>> LocateAsync(AdrRepositoryOptions options, CancellationToken cancellationToken = default);
}
