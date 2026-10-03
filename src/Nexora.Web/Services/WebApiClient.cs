using System.Text.Json;
using Microsoft.JSInterop;

namespace Nexora.Web.Services;

public sealed class WebApiClient(IJSRuntime js) : IAsyncDisposable
{
    private IJSObjectReference? module;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private async Task<IJSObjectReference> ModuleAsync() =>
        module ??= await js.InvokeAsync<IJSObjectReference>("import", "/_content/Nexora.Web/nexora.js");

    public async Task<T?> GetAsync<T>(string path)
    {
        var value = await (await ModuleAsync()).InvokeAsync<JsonElement>("get", path);
        return value.ValueKind == JsonValueKind.Null ? default : value.Deserialize<T>(JsonOptions);
    }

    public async Task<T?> SendAsync<T>(string method, string path, object? body = null)
    {
        var value = await (await ModuleAsync()).InvokeAsync<JsonElement>("send", method, path, body);
        return value.ValueKind == JsonValueKind.Null ? default : value.Deserialize<T>(JsonOptions);
    }

    public async Task SendAsync(string method, string path, object? body = null) =>
        _ = await SendAsync<JsonElement>(method, path, body);

    public async Task UploadAsync(Guid assetId, string inputId, IProgress<int> progress)
    {
        using var callback = DotNetObjectReference.Create(new UploadProgress(progress));
        await (await ModuleAsync()).InvokeVoidAsync("uploadFile", assetId.ToString(), inputId, callback);
    }

    public async Task DrawWaveformAsync(Guid assetId, Guid fileId) =>
        await (await ModuleAsync()).InvokeVoidAsync("drawWaveform", assetId.ToString(), fileId.ToString());

    public async ValueTask DisposeAsync()
    {
        if (module is not null)
        {
            try { await module.DisposeAsync(); }
            catch (JSDisconnectedException) { }
        }
    }

    private sealed class UploadProgress(IProgress<int> progress)
    {
        [JSInvokable]
        public void Report(int percent) => progress.Report(percent);
    }
}
