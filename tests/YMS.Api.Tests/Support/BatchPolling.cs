using System.Net.Http.Json;
using System.Text.Json;

namespace YMS.Api.Tests.Support;

public static class BatchPolling
{
    /// <summary>Polls <c>GET /batches/{id}</c> until the Batch is completed, failing after a time limit.</summary>
    public static async Task<JsonElement> WaitUntilCompletedAsync(HttpClient erp, Uri batchLocation, TimeSpan? timeout = null)
    {
        var deadline = DateTime.UtcNow + (timeout ?? TimeSpan.FromSeconds(10));
        while (true)
        {
            var batch = await erp.GetFromJsonAsync<JsonElement>(batchLocation);
            if (batch.GetProperty("status").GetString() == "completed")
                return batch;
            if (DateTime.UtcNow > deadline)
                throw new TimeoutException($"Batch {batchLocation} did not complete. Last state: {batch}");
            await Task.Delay(50);
        }
    }

    public static JsonElement Item(JsonElement batch, int position) =>
        batch.GetProperty("items").EnumerateArray().Single(i => i.GetProperty("position").GetInt32() == position);
}
