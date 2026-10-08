function reportUnloadError(error) {
    console.error("Component unload failed:", error);
}

function initiateComponentUnload(element) {
    if (element?.constructor?.isXShellComponent !== true || typeof(element.unload) !== "function") return;
    try {
        Promise.resolve(element.unload()).catch(reportUnloadError);
    } catch (error) {
        reportUnloadError(error);
    }
}

// Finalize definition-based XShell Components in a renderer-owned DOM subtree.
// Shadow roots are intentionally not traversed here: each Component unloads the
// render tree it owns through its own render engine.
export function unloadComponents(root) {
    if (!root) return;
    const children = Array.from(root.childNodes ?? []);
    initiateComponentUnload(root);
    for (const child of children) {
        unloadComponents(child);
    }
}

export function unloadChildComponents(root, retainedChildren = []) {
    if (!root) return;
    const retained = new Set(retainedChildren);
    for (const child of Array.from(root.childNodes ?? [])) {
        if (!retained.has(child)) unloadComponents(child);
    }
}
