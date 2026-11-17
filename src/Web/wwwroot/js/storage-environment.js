// Browser storage facts (A3 ticket 07): persistence, usage estimate, the writer lock and the cleared-data marker.
// Thin by design: no rules. Every function returns a plain string or boolean and never throws; a browser that lacks
// a feature answers "unsupported", "unknown" (empty string), "not obtained" (false) or "marker absent" (false).

export const MARKER_KEY = 'fitness-app.cleared-data-marker';
export const LOCK_NAME = 'fitness-app.writer';

/** "granted", "notGranted" or "unsupported". Checks persisted() first so an already-granted answer costs no prompt. */
export async function requestPersistence() {
    try {
        const storage = navigator.storage;
        if (!storage || typeof storage.persist !== 'function') return 'unsupported';
        if (typeof storage.persisted === 'function' && await storage.persisted()) return 'granted';
        return (await storage.persist()) ? 'granted' : 'notGranted';
    } catch {
        return 'unsupported';
    }
}

/** JSON {"usage":n,"quota":n}, or "" when the browser cannot say. */
export async function estimate() {
    try {
        const storage = navigator.storage;
        if (!storage || typeof storage.estimate !== 'function') return '';
        const { usage, quota } = await storage.estimate();
        if (!Number.isFinite(usage) || !Number.isFinite(quota)) return '';
        return JSON.stringify({ usage, quota });
    } catch {
        return '';
    }
}

let lockHeld = false;

/**
 * True if this page now holds the exclusive writer lock (kept until the page is closed). False if another window holds it.
 * A browser without Web Locks cannot tell, so it is treated as the only window: true.
 */
export async function tryTakeWriterLock() {
    if (lockHeld) return true;
    try {
        if (!navigator.locks || typeof navigator.locks.request !== 'function') return true;
        return await new Promise((resolve, reject) => {
            navigator.locks.request(LOCK_NAME, { ifAvailable: true }, (lock) => {
                if (!lock) { resolve(false); return undefined; }
                lockHeld = true;
                resolve(true);
                return new Promise(() => { }); // never settles: the lock is held until the page goes away
            }).catch(reject);
        });
    } catch {
        return false;
    }
}

/** True if the marker is present. localStorage can throw (blocked); that reads as absent. */
export function readClearedDataMarker() {
    try {
        return localStorage.getItem(MARKER_KEY) === '1';
    } catch {
        return false;
    }
}

export function writeClearedDataMarker() {
    try {
        localStorage.setItem(MARKER_KEY, '1');
    } catch {
        // Nothing to do: without the marker a wipe is still caught by the initialised metadata.
    }
}
