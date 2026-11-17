// IndexedDB storage module (A3 ticket 06). Thin by design: no rules, no JSON parsing of records.
// Every exported function takes strings and returns a JSON string, and never throws.
//
// Result shapes (all JSON text):
//   success: { "ok": true, ... }
//   failure: { "ok": false, "kind": "blocked"|"quota"|"unavailable"|"notOpen"|"unexpected", "detail": "..." }
//
// Layout version 1: one object store per record kind (out-of-line key = record key, value = the
// record as JSON text) plus "meta" (key -> string value). No secondary indexes.

export const LAYOUT_VERSION = 1;
export const KINDS = ['exercises', 'settings', 'plans', 'weighIns', 'breaks', 'badges', 'guides', 'workouts', 'weekSchedules'];
const META = 'meta';

// The one open connection. Module-level on purpose: an ES module is a singleton per page, which is exactly the lifetime of the
// connection. It is set by open(), cleared when the browser closes or versions it away, and null means "not open".
let db = null;

const ok = (extra) => JSON.stringify({ ok: true, ...extra });
const fail = (kind, detail) => JSON.stringify({ ok: false, kind, detail: detail ?? '' });

// Errors we raise ourselves carry a ready-made kind; browser errors are mapped by name.
class StoreError extends Error {
    constructor(kind, detail) { super(detail); this.kind = kind; }
}

function failFrom(e) {
    if (e instanceof StoreError) return fail(e.kind, e.message);
    const name = e && e.name;
    const detail = `${name ?? 'Error'}: ${(e && e.message) ?? e}`;
    if (name === 'QuotaExceededError') return fail('quota', detail);
    if (['SecurityError', 'InvalidStateError', 'UnknownError', 'NotSupportedError'].includes(name)) return fail('unavailable', detail);
    return fail('unexpected', detail);
}

// One upgrade step per layout version; a later version appends a step and bumps LAYOUT_VERSION.
const upgradeSteps = [
    // layout version 1
    (d) => {
        for (const kind of KINDS) d.createObjectStore(kind);
        d.createObjectStore(META);
    },
];

function onUpgrade(event) {
    const d = event.target.result;
    for (let v = event.oldVersion; v < event.newVersion; v++) upgradeSteps[v](d);
    // Record the layout version in the same (upgrade) transaction, so meta always says which layout the database has.
    event.target.transaction.objectStore(META).put(String(event.newVersion), 'layoutVersion');
}

/** Opens (creating or upgrading) the database. Idempotent while open. */
export async function open(name) {
    if (db) return ok({ layoutVersion: LAYOUT_VERSION });
    try {
        if (typeof indexedDB === 'undefined' || !indexedDB) throw new StoreError('unavailable', 'IndexedDB is not supported');
        db = await new Promise((resolve, reject) => {
            const req = indexedDB.open(name, LAYOUT_VERSION);
            req.onupgradeneeded = onUpgrade;
            req.onblocked = () => reject(new StoreError('blocked', 'Upgrade blocked by another open window'));
            req.onsuccess = () => resolve(req.result);
            req.onerror = () => reject(req.error);
        });
        // Let a newer build in another tab upgrade: close as soon as asked.
        db.onversionchange = () => { db.close(); db = null; };
        db.onclose = () => { db = null; };
        return ok({ layoutVersion: LAYOUT_VERSION });
    } catch (e) {
        db = null;
        return failFrom(e);
    }
}

export function close() {
    if (db) { db.close(); db = null; }
    return ok({});
}

/** Deletes the whole database (test cleanup only). */
export async function destroy(name) {
    close();
    try {
        await new Promise((resolve, reject) => {
            const req = indexedDB.deleteDatabase(name);
            req.onsuccess = () => resolve();
            req.onerror = () => reject(req.error);
            req.onblocked = () => reject(new StoreError('blocked', 'Delete blocked by another open window'));
        });
        return ok({});
    } catch (e) { return failFrom(e); }
}

/** Runs fn(tx) in one transaction over the given stores; resolves when the transaction commits, rejects if it aborts. */
function transact(stores, mode, fn) {
    return new Promise((resolve, reject) => {
        if (!db) { reject(new StoreError('notOpen', 'Database is not open')); return; }
        let tx;
        try { tx = db.transaction(stores, mode); } catch (e) {
            // A connection the browser closed behind our back throws InvalidStateError: treat it as not open so the caller reopens.
            if (e && e.name === 'InvalidStateError') { db = null; reject(new StoreError('notOpen', 'Database connection was closed')); } else reject(e);
            return;
        }
        tx.oncomplete = () => resolve();
        tx.onerror = () => reject(tx.error);
        tx.onabort = () => reject(tx.error ?? new DOMException('Transaction aborted', 'AbortError'));
        try { fn(tx); } catch (e) { try { tx.abort(); } catch { /* already finished */ } reject(e); }
    });
}

const requestResult = (req) => new Promise((resolve, reject) => { req.onsuccess = () => resolve(req.result); req.onerror = () => reject(req.error); });

function checkKind(kind) {
    if (!KINDS.includes(kind)) throw new StoreError('unexpected', `Unknown record kind '${kind}'`);
}

/**
 * A read-write transaction over `stores` that also writes `meta` ({ key: value }) in the same transaction, so a batch and its
 * metadata commit or abort together. A null meta value deletes the key (so "never written" can be restored).
 */
function transactWrite(stores, meta, fn) {
    const entries = Object.entries(meta);
    const all = entries.length && !stores.includes(META) ? [...stores, META] : stores;
    return transact(all, 'readwrite', (tx) => {
        fn(tx);
        const metaStore = entries.length ? tx.objectStore(META) : null;
        for (const [key, value] of entries) {
            if (value === null || value === undefined) metaStore.delete(key); else metaStore.put(value, key);
        }
    });
}

/** Requests every key and value of a store; resolves to [{ key, value }] in key order. */
async function readAll(store) {
    const [keys, values] = await Promise.all([requestResult(store.getAllKeys()), requestResult(store.getAll())]);
    return keys.map((key, i) => ({ key: String(key), value: values[i] }));
}

/** Returns { records: { kind: [{key, json}] }, meta: { key: value } }, read in one transaction. */
export async function loadAll() {
    try {
        const out = { records: {}, meta: {} };
        const pending = [];
        await transact([...KINDS, META], 'readonly', (tx) => {
            for (const kind of KINDS) {
                pending.push(readAll(tx.objectStore(kind)).then((rows) => {
                    out.records[kind] = rows.map(({ key, value }) => ({ key, json: value }));
                }));
            }
            pending.push(readAll(tx.objectStore(META)).then((rows) => {
                for (const { key, value } of rows) out.meta[key] = value;
            }));
        });
        await Promise.all(pending);
        return ok(out);
    } catch (e) { return failFrom(e); }
}

/**
 * Applies a batch atomically. Input: { "ops": [ {"op":"put","kind","key","json"}, {"op":"delete","kind","key"} ],
 * "meta": { "key": "value" }? } (meta writes join the same transaction). Any failure aborts everything.
 */
export async function apply(batchJson) {
    try {
        const batch = JSON.parse(batchJson);
        const ops = batch.ops ?? [];
        const meta = batch.meta ?? {};
        for (const op of ops) checkKind(op.kind);
        const stores = [...new Set(ops.map((o) => o.kind))];
        if (!stores.length && !Object.keys(meta).length) return ok({});
        await transactWrite(stores, meta, (tx) => {
            for (const op of ops) {
                const store = tx.objectStore(op.kind);
                if (op.op === 'put') store.put(op.json, op.key);
                else if (op.op === 'delete') store.delete(op.key);
                else throw new StoreError('unexpected', `Unknown op '${op.op}'`);
            }
        });
        return ok({});
    } catch (e) { return failFrom(e); }
}

/**
 * Atomically clears every record store and writes the document. Input: { "records": { kind: [{key, json}] },
 * "meta": { key: value }? }. Meta keys given are written; other meta keys are left alone.
 */
export async function replaceAll(documentJson) {
    try {
        const doc = JSON.parse(documentJson);
        const records = doc.records ?? {};
        for (const kind of Object.keys(records)) checkKind(kind);
        await transactWrite(KINDS, doc.meta ?? {}, (tx) => {
            for (const kind of KINDS) {
                const store = tx.objectStore(kind);
                store.clear();
                for (const r of records[kind] ?? []) store.put(r.json, r.key);
            }
        });
        return ok({});
    } catch (e) { return failFrom(e); }
}
