using Nexora.Worker;

namespace Nexora.IntegrationTests;

public sealed class PeakAccumulatorTests
{
    [Fact]
    public void Produces_minimum_and_maximum_for_each_bucket()
    {
        var accumulator = new PeakAccumulator(2);
        accumulator.Add(short.MinValue);
        accumulator.Add(0);
        accumulator.Add(short.MaxValue);

        var peaks = accumulator.Finish();

        Assert.Equal(2, peaks.Count);
        Assert.Equal(-1f, peaks[0][0]);
        Assert.Equal(0f, peaks[0][1]);
        Assert.InRange(peaks[1][0], 0.99f, 1f);
        Assert.InRange(peaks[1][1], 0.99f, 1f);
    }
}
