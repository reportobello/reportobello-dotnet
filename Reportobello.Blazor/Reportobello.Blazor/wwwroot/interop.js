export function download(url, filename) {
    openInNewTab(new URL(url), filename, true);
}

export function openInNewTab(url, downloadAs = undefined, download = false) {
    url = addDownloadOptionsToUrl(new URL(url), downloadAs, download);

    window.open(url);
}

export function openInIframe(url, ref, downloadAs = undefined) {
    url = addDownloadOptionsToUrl(new URL(url), downloadAs);
    url.hash = "#zoom=47&toolbar=0&navpanes=0&view=FitH";

    if (typeof ref === "string") {
        ref = document.querySelector(ref);
    }
    ref.setAttribute("src", url.toString());
}

function addDownloadOptionsToUrl(url, name = undefined, download = false) {
    const params = new URLSearchParams();
    if (name !== undefined) {
        params.append("downloadAs", name);
    }
    if (download) {
        params.append("download", "true");
    }
    url.search = params.toString();
    return url;
}
