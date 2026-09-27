import files from "../module.files.json" with { type: "json" };

// module class
export default class {

    // ctor
    constructor({ config, areas, moduleAssetsPath }) {
        const menu = this._createMenuFromModuleFiles(files, "/pages", "Demo", [".js", ".html"], moduleAssetsPath, true);
        areas.registerSource("x-demo-dynamic-navigation-menu-source", {
            resolve: () => {
                return menu;
            }
        });
    }

    // methods
    async start() {
    }

    async stop() {
    }

    // private methods
    _createMenuFromModuleFiles(files, root, rootItemLabel, extensions, assetsPrefix, createPaths) {
        const paths = new Set(files.map(file => file.path));

        const getExtension = (path) =>
            extensions.find(extension => path.endsWith(extension));

        const toRuntimePath = (path) => {
            const extension = getExtension(path);
            return extension
                ? path.substring(0, path.length - extension.length) + ".js"
                : path;
        };

        const toTitle = (name) => name
            .replace(/^\d+-/, "")
            .replace(/\.[^.]+$/, "")
            .replace(/[-_]+/g, " ")
            .replace(/\b\w/g, c => c.toUpperCase());

        const toPathPart = (name) => name
            .replace(/^\d+-/, "")
            .replace(/\.[^.]+$/, "");

        const getOrder = (name) => {
            const match = name.match(/^(\d+)-/);
            return match ? Number(match[1]) : Number.MAX_SAFE_INTEGER;
        };

        const createPath = (parts) =>
            "/" + parts.map(toPathPart).filter(Boolean).join("/");

        const ensureNode = (items, name, href = null, pathParts = []) => {
            let node = items.find(item => item._name === toPathPart(name));

            if (!node) {
                node = {
                    _name: toPathPart(name),
                    _order: getOrder(name),
                    label: toTitle(name),
                    href: href ? assetsPrefix + toRuntimePath(href) : null,
                    ...(createPaths ? { path: createPath(pathParts) } : {}),
                    children: []
                };

                items.push(node);
            } else if (href) {
                node.href = assetsPrefix + toRuntimePath(href);
            }

            return node;
        };

        // Root menu item may physically be index.js or index.html,
        // but its runtime URL is always index.js.
        const rootIndex = files.find(file =>
            extensions.some(extension =>
                file.path === `${root}/index${extension}`
            )
        );

        const rootItem = {
            label: rootItemLabel,
            href: assetsPrefix + `${root}/index.js`,
            ...(createPaths ? { path: "/" } : {}),
            default: true,
            children: []
        };

        const menu = rootItem.children;

        for (const file of files) {
            if (!file.path.startsWith(root + "/")) continue;

            const extension = getExtension(file.path);
            if (!extension) continue;

            if (rootIndex && file.path === rootIndex.path) continue;

            const relative = file.path.substring(root.length + 1);
            const parts = relative.split("/");
            let items = menu;

            for (let i = 0; i < parts.length; i++) {
                const part = parts[i];
                const isFile = i === parts.length - 1;

                if (isFile) {
                    if (extensions.some(ext => part === `index${ext}`)) continue;

                    ensureNode(
                        items,
                        part,
                        file.path,
                        parts.slice(0, i + 1)
                    );

                    continue;
                }

                const node = ensureNode(
                    items,
                    part,
                    null,
                    parts.slice(0, i + 1)
                );

                const directory = parts.slice(0, i + 1).join("/");

                const indexFile = files.find(file =>
                    extensions.some(ext =>
                        file.path === `${root}/${directory}/index${ext}`
                    )
                );

                if (indexFile) {
                    node.href = assetsPrefix + toRuntimePath(indexFile.path);
                }

                items = node.children;
            }
        }

        const clean = (items) => items
            .sort((a, b) =>
                a._order - b._order ||
                a.label.localeCompare(b.label)
            )
            .map(({ _name, _order, children, ...item }) => ({
                ...item,
                ...(children.length ? { children: clean(children) } : {})
            }));

        rootItem.children = clean(rootItem.children);

        return [rootItem];
    }

}