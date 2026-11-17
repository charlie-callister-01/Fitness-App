// Browser storage facts (A3 ticket 07): persistence, usage estimate, the writer lock and the cleared-data marker.
// Thin by design: no rules, and no policy about what a missing feature means (Core decides that). Every function returns a
// plain value and never throws; a browser that lacks a feature answers "unsupported", null (estimate), "unsupported" (lock)
// or false (marker absent).

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

/** { usage, quota } in bytes, or null when the browser cannot say. */
export async function estimate() {
    try {
        const storage = navigator.storage;
        if (!storage || typeof storage.estimate !== 'function') return null;
        const { usage, quota } = await storage.estimate();
        if (!Number.isFinite(usage) || !Number.isFinite(quota)) return null;
        return { usage: Math.round(usage), quota: Math.round(quota) };
    } catch {
        return null;
    }
}

let lockHeld = false;

/**
 * Tries to take the exclusive writer lock, kept until the page is closed. Answers "obtained", "heldElsewhere" (another window has it),
 * "unsupported" (no Web Locks in this browser) or "unknown" (asking threw). What each means for writing is Core's decision.
 */
export async function tryTakeWriterLock() {
    if (lockHeld) return 'obtained';
    try {
        if (!navigator.locks || typeof navigator.locks.request !== 'function') return 'unsupported';
        return await new Promise((resolve, reject) => {
            navigator.locks.request(LOCK_NAME, { ifAvailable: true }, (lock) => {
                if (!lock) { resolve('heldElsewhere'); return undefined; }
                lockHeld = true;
                resolve('obtained');
                return new Promise(() => { }); // never settles: the lock is held until the page goes away
            }).catch(reject);
        });
    } catch {
        return 'unknown';
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
