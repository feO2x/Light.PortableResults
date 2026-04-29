using System.Collections.Generic;

namespace Light.PortableResults.AspNetCore.OpenApi.ErrorContracts;

internal sealed class PortableErrorMetadataContractEqualityComparer : IEqualityComparer<PortableErrorMetadataContract>
{
    internal static PortableErrorMetadataContractEqualityComparer Instance { get; } = new ();

    private PortableErrorMetadataContractEqualityComparer() { }

    public bool Equals(PortableErrorMetadataContract? x, PortableErrorMetadataContract? y)
    {
        if (ReferenceEquals(x, y))
        {
            return true;
        }

        if (x is null || y is null)
        {
            return false;
        }

        return x switch
        {
            PortableErrorMetadataTypeContract xType when y is PortableErrorMetadataTypeContract yType =>
                xType.MetadataType == yType.MetadataType,
            PortableErrorMetadataSchemaContract xSchema when y is PortableErrorMetadataSchemaContract ySchema =>
                ReferenceEquals(xSchema.SchemaFactory, ySchema.SchemaFactory),
            PortableNoMetadataContract when y is PortableNoMetadataContract => true,
            _ => false
        };
    }

    public int GetHashCode(PortableErrorMetadataContract obj)
    {
        return obj switch
        {
            PortableErrorMetadataTypeContract typeContract => typeContract.MetadataType.GetHashCode(),
            PortableErrorMetadataSchemaContract schemaContract => schemaContract.SchemaFactory.GetHashCode(),
            PortableNoMetadataContract => 0,
            _ => obj.GetHashCode()
        };
    }
}
