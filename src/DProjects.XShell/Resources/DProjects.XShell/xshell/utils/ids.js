// generate id
const counters = new Map();

// export default function
export function generateId(prefix) {
    const current = counters.get(prefix) ?? 0;
    const nextValue = current + 1;
    counters.set(prefix, nextValue);
    return prefix + nextValue;
};