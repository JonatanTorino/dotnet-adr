using System.Threading;
using System.Threading.Tasks;

namespace Endjin.Adr.Cli.Infrastructure.Workspace;

/// <summary>
/// Resolves ADR workspace information for CLI commands.
/// </summary>
public interface IAdrWorkspaceContextFactory
{
    Task<AdrWorkspaceContext> CreateAsync(string pathOption, CancellationToken cancellationToken = default);
}
