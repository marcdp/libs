// finalize definition-based Components in renderer-owned light DOM before discarding it
export function unloadComponents(...roots) {
    const components = new Set();
    for (const root of roots) {
        if (root?.constructor?.isXShellComponent === true) components.add(root);
        for (const element of root?.querySelectorAll?.("*") ?? []) {
            if (element.constructor.isXShellComponent === true) components.add(element);
        }
    }
    for (const component of components) {
        try {
            Promise.resolve(component.unload()).catch(error => console.error("Component unload failed:", error));
        } catch (error) {
            console.error("Component unload failed:", error);
        }
    }
}
