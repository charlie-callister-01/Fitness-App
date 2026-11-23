// Focus handling for Sheet.razor: focus starts inside the dialog, Tab stays inside it, and closing hands focus back to the opener.
// Escape and the outside tap are handled in Blazor. Several sheets may be open at once (a sheet opened from a menu); each keeps its own handle.
const FOCUSABLE = 'button:not([disabled]), a[href], input:not([disabled]), select:not([disabled]), textarea:not([disabled]), [tabindex]:not([tabindex="-1"])';

const open = new Map();
let nextHandle = 1;

function focusablesOf(dialog) {
    return Array.from(dialog.querySelectorAll(FOCUSABLE)).filter((el) => el.offsetParent !== null);
}

function trapTab(dialog, event) {
    if (event.key !== 'Tab') return;
    const items = focusablesOf(dialog);
    if (items.length === 0) {
        event.preventDefault();
        dialog.focus();
        return;
    }
    const first = items[0];
    const last = items[items.length - 1];
    const active = document.activeElement;
    const outside = !dialog.contains(active);
    if (outside || (event.shiftKey && (active === first || active === dialog))) {
        event.preventDefault();
        (event.shiftKey ? last : first).focus();
    } else if (!event.shiftKey && active === last) {
        event.preventDefault();
        first.focus();
    }
}

// headingSelector names the screen's heading (Ui.Accessibility.ScreenHeading), focused on close when there is no opener to return to.
export function attach(dialog, opener, headingSelector) {
    const handle = nextHandle++;
    const entry = {
        opener: opener || document.activeElement,
        headingSelector,
        onKeyDown: (event) => trapTab(dialog, event),
    };
    document.addEventListener('keydown', entry.onKeyDown, true);
    open.set(handle, entry);
    dialog.focus();
    return handle;
}

export function detach(handle) {
    const entry = open.get(handle);
    if (!entry) return;
    open.delete(handle);
    document.removeEventListener('keydown', entry.onKeyDown, true);
    const opener = entry.opener;
    if (opener && opener !== document.body && opener.isConnected && typeof opener.focus === 'function') {
        opener.focus();
    } else if (open.size === 0) {
        focusHeading(entry.headingSelector);
    }
}

// There is no opener to return to (it was the page body, or it has gone because the screen changed, as on Finish before the Pain
// check): the screen's heading takes focus, as it does after navigation, rather than leaving it on the page body.
function focusHeading(selector) {
    const heading = selector ? document.querySelector(selector) : null;
    if (!heading) return;
    if (!heading.hasAttribute('tabindex')) heading.tabIndex = -1;
    heading.focus({ preventScroll: true });
}
