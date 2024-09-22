using Microsoft.JSInterop;

namespace Reportobello.Blazor;

/// <summary>
/// DOM helper functions for interacting with Reportobello.
/// </summary>
public class ReportobelloUtil : IAsyncDisposable
{
    private readonly Lazy<Task<IJSObjectReference>> moduleTask;

    public ReportobelloUtil(IJSRuntime jsRuntime)
    {
        moduleTask = new(() => jsRuntime.InvokeAsync<IJSObjectReference>(
            "import", "./_content/Reportobello.Blazor/interop.js").AsTask());
    }

    /// <summary>
    /// Open the given PDF in a new tab.
    /// </summary>
    /// <param name="url">URL of the PDF to open</param>
    /// <param name="downloadAs">Change the name of the PDF when downloaded from the browser</param>
    /// <param name="download">Automatically download the PDF in addition to opening a new tab</param>
    public async Task OpenInNewTab(Uri url, string? downloadAs=null, bool download=false)
    {
        await OpenInNewTab(url.ToString(), downloadAs, download);
    }

    /// <summary>
    /// Open the given PDF in a new tab.
    /// </summary>
    /// <param name="url">URL of the PDF to open</param>
    /// <param name="downloadAs">Change the name of the PDF when downloaded from the browser</param>
    /// <param name="download">Automatically download the PDF in addition to opening a new tab</param>
    public async Task OpenInNewTab(string url, string? downloadAs=null, bool download=false)
    {
        var module = await moduleTask.Value;

        if (downloadAs == null)
        {
            await module.InvokeVoidAsync("openInNewTab", url);
        }
        else
        {
            if (download)
            {
                await module.InvokeVoidAsync("openInNewTab", url, downloadAs, true);
            }
            else
            {
                await module.InvokeVoidAsync("openInNewTab", url, downloadAs);
            }
        }
    }

    /// <summary>
    /// Download the PDF.
    /// </summary>
    /// <param name="url">URL of the PDF to open</param>
    /// <param name="downloadAs">Change the name of the PDF when downloaded from the browser</param>
    public async Task Download(Uri url, string downloadAs="report.pdf")
    {
        await Download(url.ToString(), downloadAs);
    }

    /// <summary>
    /// Download the PDF.
    /// </summary>
    /// <param name="url">URL of the PDF to open</param>
    /// <param name="downloadAs">Change the name of the PDF when downloaded from the browser</param>
    public async Task Download(string url, string downloadAs="report.pdf")
    {
        await OpenInNewTab(url.ToString(), downloadAs, true);
    }

    /// <summary>
    /// Open the PDF in an existing iframe.
    /// </summary>
    /// <param name="url">URL of the PDF to open</param>
    /// <param name="elementRef">CSS selector of the iframe element</param>
    /// <param name="downloadAs">Change the name of the PDF when downloaded from the browser</param>
    public async Task OpenInIframe(Uri url, object elementRef, string? downloadAs=null)
    {
        await OpenInIframe(url.ToString(), elementRef, downloadAs);
    }

    /// <summary>
    /// Open the PDF in an existing iframe.
    /// </summary>
    /// <param name="url">URL of the PDF to open</param>
    /// <param name="elementRef">CSS selector of the iframe element</param>
    /// <param name="downloadAs">Change the name of the PDF when downloaded from the browser</param>
    public async Task OpenInIframe(string url, object elementRef, string? downloadAs=null)
    {
        var module = await moduleTask.Value;

        if (downloadAs == null)
        {
            await module.InvokeVoidAsync("openInIframe", url, elementRef);
        }
        else
        {
            await module.InvokeVoidAsync("openInIframe", url, elementRef, downloadAs);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (moduleTask.IsValueCreated)
        {
            var module = await moduleTask.Value;
            await module.DisposeAsync();
        }
    }
}
