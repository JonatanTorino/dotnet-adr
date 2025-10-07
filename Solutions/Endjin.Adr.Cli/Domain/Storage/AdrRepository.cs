using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Endjin.Adr.Cli.Domain.Contracts;
using Endjin.Adr.Cli.Domain.Models;

namespace Endjin.Adr.Cli.Domain.Storage;

/// <summary>
/// Provides access to ADR documents using file-system backed services.
/// </summary>
public class AdrRepository : IAdrRepository
{
    private readonly IAdrFileLocator locator;
    private readonly IAdrDocumentReader reader;
    private readonly IAdrDocumentParser parser;

    public AdrRepository(IAdrFileLocator locator, IAdrDocumentReader reader, IAdrDocumentParser parser)
    {
        this.locator = locator;
        this.reader = reader;
        this.parser = parser;
    }

    public async Task<IReadOnlyList<Adr>> GetAllAsync(AdrRepositoryOptions options, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<AdrFileReference> references = await this.locator.LocateAsync(options, cancellationToken).ConfigureAwait(false);

        List<Adr> documents = new(references.Count);

        foreach (AdrFileReference reference in references)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            string content = await this.reader.ReadAsync(reference, cancellationToken).ConfigureAwait(false);
            documents.Add(this.parser.Parse(reference, content));
        }

        return documents;
    }

    public async Task<Adr?> GetByIdAsync(int recordNumber, AdrRepositoryOptions options, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Adr> documents = await this.GetAllAsync(options, cancellationToken).ConfigureAwait(false);
        return documents.FirstOrDefault(doc => doc.RecordNumber == recordNumber);
    }
}
