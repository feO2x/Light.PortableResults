using Xunit;

namespace Light.PortableResults.Tests;

/// <summary>
/// Groups the test classes that assert exact <c>GC.GetAllocatedBytesForCurrentThread()</c> deltas.
/// </summary>
/// <remarks>
/// DisableParallelization is load-bearing, not tidiness. The counter is per-thread, but a GC triggered by any other
/// thread retires and refills the measuring thread's allocation context, and that accounting boundary shifts the
/// observed value by up to the size of a context. Deviations therefore stay below 8 KB and can fall on either side,
/// because the perturbation lands in the baseline measurement as easily as in the measured one. A plain Collection
/// attribute would not help: it only serializes the tests inside the collection, while the GC pressure comes from
/// everything scheduled beside it. Measured in Debug, where the suite reproduces this most readily: four failures in
/// twelve runs without this attribute, none in twenty-four runs with it, for about three percent of the runtime.
/// </remarks>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class AllocationMeasurementCollection
{
    public const string Name = "Allocation measurement";
}
