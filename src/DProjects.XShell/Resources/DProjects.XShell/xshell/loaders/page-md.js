import Page from "../page.js";


function escapeHtml(value) {
    return String(value)
        .replaceAll("&", "&amp;")
        .replaceAll("<", "&lt;")
        .replaceAll(">", "&gt;");
}

function renderInline(text) {
    return text
        .replace(/`([^`]+)`/g, "<code>$1</code>")
        .replace(/\*\*([^*]+)\*\*/g, "<strong>$1</strong>")
        .replace(/\*([^*]+)\*/g, "<em>$1</em>")
        .replace(/\[([^\]]+)\]\(([^)]+)\)/g, '<a href="$2">$1</a>');
}

function renderMarkdown(markdown) {
    const lines = markdown.replace(/\r\n?/g, "\n").split("\n");

    const html = [];
    let paragraph = [];
    let list = null;
    let code = null;
    let codeLanguage = "";

    function flushParagraph() {
        if (!paragraph.length) {
            return;
        }

        html.push(
            `<p>${renderInline(paragraph.join(" "))}</p>`
        );

        paragraph = [];
    }

    function flushList() {
        if (!list) {
            return;
        }

        html.push(
            `<${list.type}>${list.items
                .map(item => `<li>${renderInline(item)}</li>`)
                .join("")}</${list.type}>`
        );

        list = null;
    }

    function flushCode() {
        if (code === null) {
            return;
        }

        const languageClass = codeLanguage
            ? ` class="language-${escapeHtml(codeLanguage)}"`
            : "";

        html.push(
            `<pre><code${languageClass}>${escapeHtml(code.join("\n"))}</code></pre>`
        );

        code = null;
        codeLanguage = "";
    }

    for (const line of lines) {

        if (code !== null) {
            if (line.startsWith("```")) {
                flushCode();
            } else {
                code.push(line);
            }

            continue;
        }

        if (line.startsWith("```")) {
            flushParagraph();
            flushList();

            code = [];
            codeLanguage = line.substring(3).trim();

            continue;
        }

        if (!line.trim()) {
            flushParagraph();
            flushList();
            continue;
        }

        const heading = line.match(/^(#{1,6})\s+(.*)$/);

        if (heading) {
            flushParagraph();
            flushList();

            const level = heading[1].length;
            const content = renderInline(heading[2]);

            html.push(`<h${level}>${content}</h${level}>`);
            continue;
        }

        const unordered = line.match(/^\s*[-*+]\s+(.*)$/);

        if (unordered) {
            flushParagraph();

            if (!list || list.type !== "ul") {
                flushList();
                list = {
                    type: "ul",
                    items: []
                };
            }

            list.items.push(unordered[1]);
            continue;
        }

        const ordered = line.match(/^\s*\d+\.\s+(.*)$/);

        if (ordered) {
            flushParagraph();

            if (!list || list.type !== "ol") {
                flushList();
                list = {
                    type: "ol",
                    items: []
                };
            }

            list.items.push(ordered[1]);
            continue;
        }

        paragraph.push(line.trim());
    }

    flushParagraph();
    flushList();
    flushCode();

    return html.join("\n");
}

function getTitle(markdown, src) {
    const match = markdown.match(/^#\s+(.+)$/m);

    if (match) {
        return match[1].trim();
    }

    let name = src.split("?")[0];
    name = name.substring(name.lastIndexOf("/") + 1);
    name = name.replace(/\.md$/i, "");
    name = name.replace(/^\d+-/, "");
    name = name.replace(/[-_]+/g, " ");

    return name.replace(/\b\w/g, value => value.toUpperCase());
}


export default class LoaderPageMd {

    async load(src, context) {
        
        const response = await fetch(src);

        if (!response.ok) {
            throw new Error(
                `Failed to load Markdown page '${src}' (${response.status})`
            );
        }

        const markdown = await response.text();
        const html = renderMarkdown(markdown);
        const title = getTitle(markdown, src);


        return class extends Page {

            constructor({src, context}) {
                super({src, context});

                this._label = title;
                this._description = "";
                this._icon = "";

                this._controller = {};
            }


            async mount({host}) {
                if (this._unloaded) {
                    return;
                }

                this._host = host;

                const article = document.createElement("article");
                article.className = "xshell-markdown";
                article.innerHTML = html;

                this._rewriteLinks(article);

                host.replaceChildren(article);

                await super.mount({host});
            }


            async unmount() {
                if (this._unloaded) {
                    return;
                }

                if (this._host) {
                    this._host.replaceChildren();
                }

                await super.unmount();
            }


            _rewriteLinks(root) {
                const base = new URL(src, document.baseURI);

                root.querySelectorAll("a[href]").forEach(anchor => {
                    const href = anchor.getAttribute("href");

                    if (!href) {
                        return;
                    }

                    if (
                        href.startsWith("#") ||
                        href.startsWith("/") ||
                        /^[a-zA-Z][a-zA-Z0-9+.-]*:/.test(href)
                    ) {
                        return;
                    }

                    anchor.href = new URL(href, base).pathname;
                });
            }
        };
    }
};