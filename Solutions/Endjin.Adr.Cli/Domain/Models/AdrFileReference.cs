using System.IO;

namespace Endjin.Adr.Cli.Domain.Models;

/// <summary>
/// Describes an ADR file discovered in the repository.
/// </summary>
public class AdrFileReference
{
    public AdrFileReference(int recordNumber, string fileName, string fullPath)
    {
        this.RecordNumber = recordNumber;
        this.FileName = fileName;
        this.FullPath = fullPath;
    }

    public int RecordNumber { get; }

    public string FileName { get; }

    public string FullPath { get; }

}
