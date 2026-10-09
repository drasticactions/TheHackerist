// Settings for the browser head, kept in this origin's localStorage.
export function read(key) {
    try {
        return globalThis.localStorage.getItem(key);
    } catch {
        return null;
    }
}

export function write(key, value) {
    try {
        globalThis.localStorage.setItem(key, value);
    } catch {
        // Private windows and blocked storage keep settings for this session only.
    }
}
