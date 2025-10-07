using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Endjin.Adr.Cli.Domain.Models;

namespace Endjin.Adr.Cli.Domain.Contracts;

/// <summary>
/// Provides access to ADR documents stored on disk.
/// </summary>
public interface IAdrRepository
{
    Task<IReadOnlyList<Adr>> GetAllAsync(AdrRepositoryOptions options, CancellationToken cancellationToken = default);

    Task<Adr?> GetByIdAsync(int recordNumber, AdrRepositoryOptions options, CancellationToken cancellationToken = default);
}
