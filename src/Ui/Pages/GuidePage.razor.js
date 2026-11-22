// Opens a demo video in a new tab. Returns false when the device is offline, so the page can say so; the rest of the app does not need the network.
export function openVideo(url) {
    if (navigator.onLine === false) {
        return false;
    }

    window.open(url, "_blank", "noopener");
    return true;
}
