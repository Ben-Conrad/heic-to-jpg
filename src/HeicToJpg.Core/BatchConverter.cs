namespace HeicToJpg.Core;

public sealed record ConversionProgress(int Completed, int Total, ConversionResult LatestResult);

public sealed class BatchConverter
{
    private readonly HeicConverter _converter = new();

    public async Task<IReadOnlyList<ConversionResult>> ConvertAsync(
        IReadOnlyList<string> filePaths,
        ConversionOptions options,
        IProgress<ConversionProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var results = new ConversionResult[filePaths.Count];
        var completed = 0;
        var maxDegreeOfParallelism = Math.Max(1, Environment.ProcessorCount - 1);

        await Parallel.ForEachAsync(
            Enumerable.Range(0, filePaths.Count),
            new ParallelOptions { MaxDegreeOfParallelism = maxDegreeOfParallelism, CancellationToken = cancellationToken },
            async (index, ct) =>
            {
                var result = await Task.Run(() => _converter.ConvertFile(filePaths[index], options), ct);
                results[index] = result;

                var done = Interlocked.Increment(ref completed);
                progress?.Report(new ConversionProgress(done, filePaths.Count, result));
            });

        return results;
    }
}
