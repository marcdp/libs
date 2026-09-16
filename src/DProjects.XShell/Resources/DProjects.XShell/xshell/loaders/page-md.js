import { createPageClassFromJsDefinition } from "./page-js.js"


// functions
function extractFrontmatter(source) {
    if (!source.startsWith('---')) return { meta: null, body: source };
    const end = source.indexOf('\n---', 3);
    if (end === -1) return { meta: null, body: source };
    const rawMeta = source.slice(3, end).trim();
    const body = source.slice(end + 4).trimStart();
    const meta = {};
    for (const line of rawMeta.split('\n')) {
        const trimmed = line.trim();
        if (!trimmed || trimmed.startsWith('#')) continue;
        const idx = trimmed.indexOf(':');
        if (idx === -1) continue;
        const key = trimmed.slice(0, idx).trim();
        let value = trimmed.slice(idx + 1).trim();
        // basic value coercion
        if (value === 'true') value = true;
        else if (value === 'false') value = false;
        else if (!isNaN(value) && value !== '') value = Number(value);
        meta[key] = value;
    }
    return { meta, body };
}

//export 
export default class LoaderPageMd {
    async load(src, context) {
        const request = await fetch(src);
        if (!request.ok) throw new Error(`Failed to load markdown page: ${src}`);
        const markdown = await request.text();
        const { meta, body } = extractFrontmatter(markdown);
        // definition
        let definition = {
            meta: meta || {},
            template: body
        }
        definition.meta.renderEngine = "markdown";
        // create page
        const pageClass = createPageClassFromJsDefinition(src, context, definition);
        // return
        return pageClass;
    }
};