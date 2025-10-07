using Endjin.Adr.Cli.Domain.Models;

namespace Endjin.Adr.Cli.Domain.Contracts;

/// <summary>
/// Transforms raw ADR markdown content into structured domain objects.
/// </summary>
public interface IAdrDocumentParser
{
    Adr Parse(AdrFileReference reference, string content);
}
