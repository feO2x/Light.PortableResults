using System;

namespace Light.PortableResults.AspNetCore.OpenApi.ErrorContracts;

/// <summary>
/// Represents a metadata contract backed by a CLR type.
/// </summary>
public sealed class ErrorMetadataTypeContract : ErrorMetadataContract
{
    /// <summary>
    /// Initializes a new instance of <see cref="ErrorMetadataTypeContract" />.
    /// </summary>
    /// <param name="metadataType">The CLR metadata type.</param>
    public ErrorMetadataTypeContract(Type metadataType)
    {
        ArgumentNullException.ThrowIfNull(metadataType);
        MetadataType = metadataType;
    }

    /// <summary>
    /// Gets the CLR metadata type.
    /// </summary>
    public Type MetadataType { get; }

    /// <inheritdoc />
    public override bool Equals(object? obj) =>
        obj is ErrorMetadataTypeContract other && MetadataType == other.MetadataType;

    /// <inheritdoc />
    public override int GetHashCode() => MetadataType.GetHashCode();
}
