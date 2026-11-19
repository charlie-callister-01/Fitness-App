// Screen Wake Lock (B2 ticket 07): keeps the phone awake while the Workout log is open.
// Thin by design: no rules, never throws. A browser without the feature answers false from isSupported() and acquire() does nothing.
// The browser drops the lock whenever the tab is hidden, so while the log wants it this module takes it again when the tab is shown.

let sentinel = null;
let wanted = false;
let listening = false;

export function isSupported() {
    return typeof navigator !== 'undefined' && !!navigator.wakeLock && typeof navigator.wakeLock.request === 'function';
}

async function take() {
    if (!isSupported() || sentinel) return;
    try {
        const taken = await navigator.wakeLock.request('screen');
        // Released while we waited, or no longer wanted: let go again.
        if (!wanted) { await taken.release(); return; }
        sentinel = taken;
        taken.addEventListener('release', () => { if (sentinel === taken) sentinel = null; });
    } catch {
        // Refused (battery saver, hidden tab): the screen simply may sleep. Not an error.
    }
}

function onVisibilityChange() {
    if (wanted && document.visibilityState === 'visible') take();
}

export async function acquire() {
    wanted = true;
    if (!listening) {
        document.addEventListener('visibilitychange', onVisibilityChange);
        listening = true;
    }
    await take();
}

export async function release() {
    wanted = false;
    const held = sentinel;
    sentinel = null;
    try {
        if (held) await held.release();
    } catch {
        // Already released by the browser.
    }
}
