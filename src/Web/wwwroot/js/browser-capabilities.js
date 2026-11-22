// Browser-owned capabilities: clipboard, file download, Web Share, standalone detection, and PWA install prompt.
// Thin by design: no rules; every failure returns false rather than throwing.

let deferredPrompt = null;
let dotNetListener = null;
let listenersAttached = false;

function notifyInstallChanged() {
    if (dotNetListener) {
        dotNetListener.invokeMethodAsync('OnInstallAvailabilityChanged').catch(() => { });
    }
}

function ensureInstallListeners() {
    if (listenersAttached) return;
    listenersAttached = true;

    window.addEventListener('beforeinstallprompt', (event) => {
        event.preventDefault();
        deferredPrompt = event;
        notifyInstallChanged();
    });

    window.addEventListener('appinstalled', () => {
        deferredPrompt = null;
        notifyInstallChanged();
    });
}

ensureInstallListeners();

export function registerInstallListener(dotNetRef) {
    dotNetListener = dotNetRef;
    ensureInstallListeners();
    return Boolean(deferredPrompt);
}

export async function copyText(text) {
    try {
        if (!navigator.clipboard || typeof navigator.clipboard.writeText !== 'function') return false;
        await navigator.clipboard.writeText(text);
        return true;
    } catch {
        return false;
    }
}

export function downloadFile(fileName, content, contentType) {
    try {
        const blob = new Blob([content], { type: contentType || 'application/octet-stream' });
        const url = URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = fileName;
        link.style.display = 'none';
        document.body.appendChild(link);
        link.click();
        link.remove();
        setTimeout(() => URL.revokeObjectURL(url), 1000);
        return true;
    } catch {
        return false;
    }
}

export function canShare() {
    return typeof navigator.share === 'function';
}

export async function share(title, text, fileName, contentType) {
    try {
        if (typeof navigator.share !== 'function') return false;
        if (fileName && typeof navigator.canShare === 'function' && typeof File === 'function') {
            const file = new File([text], fileName, { type: contentType || 'text/plain' });
            if (navigator.canShare({ files: [file] })) {
                await navigator.share({ title, files: [file] });
                return true;
            }
        }

        await navigator.share({ title, text });
        return true;
    } catch {
        return false;
    }
}

export function isStandalone() {
    try {
        const matchMediaStandalone = window.matchMedia && window.matchMedia('(display-mode: standalone)').matches;
        return Boolean(matchMediaStandalone || navigator.standalone === true);
    } catch {
        return false;
    }
}

export function canInstall() {
    return Boolean(deferredPrompt);
}

export async function promptInstall() {
    try {
        if (!deferredPrompt) return false;
        const promptEvent = deferredPrompt;
        deferredPrompt = null;
        await promptEvent.prompt();
        const choice = await promptEvent.userChoice;
        notifyInstallChanged();
        return Boolean(choice && choice.outcome === 'accepted');
    } catch {
        return false;
    }
}
