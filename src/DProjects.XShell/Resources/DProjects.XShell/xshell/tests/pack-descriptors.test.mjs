import assert from "node:assert/strict";
import { spawn, spawnSync } from "node:child_process";
import { createHash } from "node:crypto";
import { cpSync, existsSync, mkdtempSync, mkdirSync, readFileSync, readdirSync, rmSync, statSync, writeFileSync } from "node:fs";
import { dirname, join, relative, resolve } from "node:path";
import { fileURLToPath, pathToFileURL } from "node:url";
import test from "node:test";
import { inflateRawSync } from "node:zlib";

const projectDirectory = dirname(fileURLToPath(new URL("../../../../DProjects.XShell.csproj", import.meta.url)));
const assemblyPath = join(projectDirectory, "bin", "Release", "net10.0", "DProjects.XShell.dll");
let ready = false;

function ensureAssembly() {
    if (ready) return;
    const result = spawnSync("dotnet", ["build", join(projectDirectory, "DProjects.XShell.csproj"), "--configuration", "Release", "--no-restore", "-m:1",
        "-p:UseSharedCompilation=false"], { encoding: "utf8" });
    assert.equal(result.status, 0, result.stdout + result.stderr);
    ready = true;
}

function fixture() {
    ensureAssembly();
    mkdirSync(join(projectDirectory, "obj"), { recursive: true });
    return mkdtempSync(join(projectDirectory, "obj", "pack-distribution-"));
}

function module(directory, id, references = {}, extra = {}) {
    mkdirSync(directory, { recursive: true });
    const modules = { [id]: { version: "1.0.0", ...extra }, ...references };
    const content = JSON.stringify({ modules });
    writeFileSync(join(directory, "module.jsonc"), "// authored configuration\n" + content);
    writeFileSync(join(directory, "resource.txt"), id);
    return content;
}

function pack(source, output, args = []) {
    return packWithAssembly(assemblyPath, source, output, args);
}

function packWithAssembly(toolAssembly, source, output, args = []) {
    return spawnSync("dotnet", [toolAssembly, "pack", source, output, ...args], { encoding: "utf8", cwd: source });
}

function successfulPack(source, output, args = []) {
    const result = pack(source, output, args);
    assert.equal(result.status, 0, result.stdout + result.stderr);
    assert.equal(result.stdout.trim().split(/\r?\n/).at(-1), join(output, "release.json"));
    assert.equal(existsSync(join(output, ".pack.lock")), false);
    return JSON.parse(readFileSync(join(output, "release.json"), "utf8"));
}

function entries(archivePath) {
    const archive = readFileSync(archivePath);
    const result = new Map();
    for (let offset = 0; offset <= archive.length - 46;) {
        if (archive.readUInt32LE(offset) !== 0x02014b50) {
            offset++;
            continue;
        }
        const nameLength = archive.readUInt16LE(offset + 28);
        const extraLength = archive.readUInt16LE(offset + 30);
        const commentLength = archive.readUInt16LE(offset + 32);
        const name = archive.toString("utf8", offset + 46, offset + 46 + nameLength);
        const localOffset = archive.readUInt32LE(offset + 42);
        const contentOffset = localOffset + 30 + archive.readUInt16LE(localOffset + 26) + archive.readUInt16LE(localOffset + 28);
        const compressed = archive.subarray(contentOffset, contentOffset + archive.readUInt32LE(offset + 20));
        result.set(name, archive.readUInt16LE(offset + 10) === 8 ? inflateRawSync(compressed) : compressed);
        offset += 46 + nameLength + extraLength + commentLength;
    }
    return result;
}

function contentHash(archive) {
    const hash = createHash("sha256");
    for (const [name, content] of [...archive].filter(([name]) => !name.endsWith("/")).sort(([a], [b]) => a < b ? -1 : a > b ? 1 : 0)) {
        const path = Buffer.from(name);
        const pathLength = Buffer.alloc(4);
        pathLength.writeInt32LE(path.length);
        const fileLength = Buffer.alloc(8);
        fileLength.writeBigInt64LE(BigInt(content.length));
        hash.update(pathLength).update(path).update(fileLength).update(content);
    }
    return hash.digest("hex").slice(0, 16);
}

function descriptor(output, packagePath) {
    return JSON.parse(readFileSync(join(output, packagePath), "utf8"));
}

function metaContent(index, name) {
    const match = index.match(new RegExp(`<meta\\s+name="${name}"\\s+content="([^"]*)"`));
    assert.notEqual(match, null, `Missing ${name} meta value`);
    return match[1];
}

function archivePath(output, packagePath) {
    return join(output, dirname(packagePath), packagePath.endsWith("/xshell.json") ? "xshell.zip" : "module.zip");
}

function assertUtcTimestamp(value) {
    assert.match(value, /^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}Z$/);
    assert.equal(Number.isNaN(Date.parse(value)), false);
}

test("pack discovers a dependency graph, rewrites references, and commits one complete release", () => {
    const root = fixture();
    try {
        const sources = join(root, "sources");
        const output = join(root, "dist");
        module(join(sources, "customers"), "customers", {}, { builtAt: "2000-01-01T00:00:00Z" });
        module(join(sources, "x"), "x");
        module(join(sources, "orders"), "orders", {
            customers: { configUrl: "url:../customers/module.jsonc", params: { theme: "blue" } },
            x: { configUrl: "url:../x/module.jsonc" }
        });
        module(join(sources, "reports"), "reports", { x: { configUrl: "url:../x/module.jsonc" } });
        const authored = module(join(sources, "app"), "app", {
            orders: { configUrl: "url:../orders/module.jsonc" },
            reports: { configUrl: "url:../reports/module.jsonc" }
        });

        const release = successfulPack(join(sources, "app"), output);
        assertUtcTimestamp(release.createdAt);
        assert.equal(release.packages.length, 6);
        assert.equal(release.root, release.packages.find(path => path.startsWith("app/")));
        assert.match(release.xshell, /^xshell\/[^/]+\.[0-9a-f]{16}\/xshell\.json$/);
        assert.equal(release.packages.filter(path => path === release.xshell).length, 1);
        assert.deepEqual(release.packages, [...release.packages].sort((a, b) => a < b ? -1 : a > b ? 1 : 0));
        assert.equal(release.packages.filter(path => path.startsWith("x/")).length, 1);
        assert.equal(existsSync(join(output, "index.html")), false);
        assert.equal(existsSync(join(output, "sw.js")), false);
        const rootDirectory = join(output, dirname(release.root));
        const index = readFileSync(join(rootDirectory, "index.html"), "utf8");
        const worker = readFileSync(join(rootDirectory, "sw.js"), "utf8");
        assert.match(index, new RegExp(`content="/${release.root}"`));
        assert.match(index, new RegExp(`src="/${dirname(release.xshell)}/bootstrap\\.js"`));
        assert.match(index, /xshell:xshell.environment"\s+content="Production"/);
        assert.match(index, /xshell:xshell.temp.url"\s+content="\/temp"/);
        assert.match(worker, new RegExp(`importScripts\\("/${dirname(release.xshell)}/sw\\.js"\\)`));
        assert.equal(release.packages.some(path => path === "index.html" || path === "sw.js"), false);
        for (const name of ["main", "isRoot", "moduleType"]) assert.equal(Object.hasOwn(descriptor(output, release.root).modules.app, name), false);
        for (const path of release.packages.filter(path => path !== release.xshell)) {
            const directory = join(output, dirname(path));
            const archive = entries(join(directory, "module.zip"));
            assert.deepEqual(readdirSync(directory).sort(), path === release.root ? ["index.html", "module.json", "module.zip", "sw.js"] : ["module.json", "module.zip"]);
            assert.equal(archive.has("module.json"), true);
            assert.equal(archive.has("module.jsonc"), false);
            assert.equal(archive.has("module.files.json"), true);
            if (path === release.root) {
                const canonicalIndex = archive.get("index.html").toString("utf8");
                const hash = dirname(path).split(".").at(-1);
                assert.equal(metaContent(canonicalIndex, "xshell:app.configPath"), "/app/1.0.0.__XSHELL_ROOT_HASH__/module.json");
                assert.equal(metaContent(index, "xshell:app.configPath"), `/app/1.0.0.${hash}/module.json`);
                assert.deepEqual(readFileSync(join(directory, "sw.js")), archive.get("sw.js"));
                assert.equal(JSON.parse(archive.get("module.files.json")).some(file => ["/index.html", "/sw.js"].includes(file.path)), false);
            } else {
                assert.equal(archive.has("index.html"), false);
                assert.equal(archive.has("sw.js"), false);
            }
            assert.equal(contentHash(archive), dirname(path).split(".").at(-1));
            const canonical = JSON.parse(archive.get("module.json")).modules[path.split("/")[0]];
            assert.equal(canonical.hash, "");
            assert.equal(Object.hasOwn(canonical, "builtAt"), false);
            const published = descriptor(output, path);
            assertUtcTimestamp(published.modules[path.split("/")[0]].builtAt);
            assert.equal(published.modules[path.split("/")[0]].hash, dirname(path).split(".").at(-1));
            assert.equal(published.modules[path.split("/")[0]].assetsUrl, "url:./module.zip");
            assert.deepEqual(published.modules[path.split("/")[0]].files.map(file => file.path), ["/resource.txt"]);
            assert.equal(published.modules[path.split("/")[0]].files[0].hash, createHash("sha256").update(path.split("/")[0]).digest("hex"));
            assert.equal(published.modules[path.split("/")[0]].files[0].size, Buffer.byteLength(path.split("/")[0]));
            const archived = JSON.parse(archive.get("module.json"));
            for (const [id, reference] of Object.entries(published.modules)) {
                if (id === path.split("/")[0]) continue;
                assert.deepEqual(archived.modules[id], reference);
                assert.equal(reference.configUrl.startsWith("url:../../"), true);
                const target = resolve(directory, reference.configUrl.slice(4));
                assert.equal(existsSync(target), true);
                assert.equal(target, join(output, release.packages.find(candidate => candidate.startsWith(id + "/"))));
            }
        }
        const frameworkDirectory = join(output, dirname(release.xshell));
        assert.deepEqual(readdirSync(frameworkDirectory).sort(), ["bootstrap.js", "sw.js", "xshell.json", "xshell.zip"]);
        const frameworkArchive = entries(archivePath(output, release.xshell));
        assert.equal(frameworkArchive.has("xshell.json"), true);
        assert.equal(frameworkArchive.has("module.files.json"), true);
        assert.equal(frameworkArchive.has("bootstrap.js"), true);
        assert.equal(frameworkArchive.has("sw.js"), true);
        assert.deepEqual(frameworkArchive.get("sw.js"), readFileSync(join(dirname(assemblyPath), "Resources", "DProjects.XShell", "xshell", "sw.js")));
        assert.equal(JSON.parse(frameworkArchive.get("module.files.json")).some(file => file.path === "/sw.js"), true);
        assert.equal(frameworkArchive.has("xshell.js"), true);
        assert.equal(frameworkArchive.has("xshell.jsonc"), false);
        for (const name of ["bootstrap.js", "sw.js"]) {
            assert.deepEqual(readFileSync(join(frameworkDirectory, name)), frameworkArchive.get(name));
        }
        assert.notDeepEqual(readFileSync(join(frameworkDirectory, "xshell.json")), frameworkArchive.get("xshell.json"));
        const frameworkCanonical = JSON.parse(frameworkArchive.get("xshell.json")).xshell;
        const frameworkPublished = descriptor(output, release.xshell).xshell;
        const frameworkHash = dirname(release.xshell).split(".").at(-1);
        assert.equal(frameworkCanonical.hash, "");
        assert.equal(Object.hasOwn(frameworkCanonical, "builtAt"), false);
        assert.equal(frameworkPublished.hash, frameworkHash);
        assert.equal(frameworkPublished.assetsUrl, "url:./xshell.zip");
        assert.deepEqual(frameworkPublished.files, JSON.parse(frameworkArchive.get("module.files.json")));
        assertUtcTimestamp(frameworkPublished.builtAt);
        assert.equal(contentHash(frameworkArchive), frameworkHash);
        assert.equal(Object.hasOwn(descriptor(output, release.root).modules, "xshell"), false);
        const orders = descriptor(output, release.packages.find(path => path.startsWith("orders/")));
        assert.deepEqual(orders.modules.customers.params, { theme: "blue" });
        assert.equal(readFileSync(join(sources, "app", "module.jsonc"), "utf8"), "// authored configuration\n" + authored);

        const priorPackages = new Map(release.packages.map(path => [path, {
            descriptor: readFileSync(join(output, path)), zip: readFileSync(archivePath(output, path))
        }]));
        const priorRootStartup = ["index.html", "sw.js"].map(name => [name, readFileSync(join(rootDirectory, name))]);
        const originalFrameworkFiles = ["bootstrap.js", "sw.js"].map(name => [name, readFileSync(join(frameworkDirectory, name))]);
        writeFileSync(join(output, "release.json"), JSON.stringify({ ...release, createdAt: "2000-01-01T00:00:00Z" }));
        module(join(sources, "customers"), "customers", {}, { builtAt: "2030-01-01T00:00:00Z" });
        const repeated = successfulPack(join(sources, "app"), output);
        assert.deepEqual(repeated.packages, release.packages);
        assert.equal(repeated.root, release.root);
        assert.equal(repeated.xshell, release.xshell);
        assertUtcTimestamp(repeated.createdAt);
        assert.notEqual(repeated.createdAt, "2000-01-01T00:00:00Z");
        for (const path of release.packages) {
            assert.deepEqual(readFileSync(join(output, path)), priorPackages.get(path).descriptor);
            assert.deepEqual(readFileSync(archivePath(output, path)), priorPackages.get(path).zip);
        }
        for (const [name, bytes] of originalFrameworkFiles) assert.deepEqual(readFileSync(join(frameworkDirectory, name)), bytes);
        for (const [name, bytes] of priorRootStartup) assert.deepEqual(readFileSync(join(rootDirectory, name)), bytes);
        const otherOutput = join(root, "another-dist");
        const otherRelease = successfulPack(join(sources, "app"), otherOutput);
        assert.deepEqual(otherRelease.packages, release.packages);
        assert.equal(otherRelease.root, release.root);
        assert.equal(otherRelease.xshell, release.xshell);
        for (const path of release.packages) {
            assert.deepEqual(readFileSync(archivePath(output, path)), readFileSync(archivePath(otherOutput, path)));
        }
        for (const [name, bytes] of priorRootStartup) assert.deepEqual(readFileSync(join(otherOutput, dirname(release.root), name)), bytes);
        for (const [name, bytes] of originalFrameworkFiles) {
            assert.deepEqual(readFileSync(join(otherOutput, dirname(release.xshell), name)), bytes);
        }
        assert.equal(readdirSync(output).some(name => name.endsWith(".tmp")), false);
    } finally {
        rmSync(root, { recursive: true, force: true });
    }
});

test("dependency identity changes propagate to consumers while unrelated packages are reused", () => {
    const root = fixture();
    try {
        const sources = join(root, "sources");
        const output = join(root, "dist");
        module(join(sources, "child"), "child");
        module(join(sources, "other"), "other");
        module(join(sources, "app"), "app", {
            child: { configUrl: "url:../child/module.jsonc" },
            other: { configUrl: "url:../other/module.jsonc" }
        });
        const before = successfulPack(join(sources, "app"), output);
        const originalOther = descriptor(output, before.packages.find(path => path.startsWith("other/"))).modules.other.builtAt;
        const originalFrameworkDescriptor = readFileSync(join(output, before.xshell));
        const originalFrameworkZip = readFileSync(archivePath(output, before.xshell));
        const originalFrameworkFiles = ["bootstrap.js", "sw.js"].map(name => [name, readFileSync(join(output, dirname(before.xshell), name))]);
        writeFileSync(join(sources, "child", "resource.txt"), "changed");
        const after = successfulPack(join(sources, "app"), output);
        assertUtcTimestamp(after.createdAt);
        assert.notEqual(after.root, before.root);
        assert.equal(after.xshell, before.xshell);
        assert.deepEqual(readFileSync(join(output, after.xshell)), originalFrameworkDescriptor);
        assert.deepEqual(readFileSync(archivePath(output, after.xshell)), originalFrameworkZip);
        for (const [name, bytes] of originalFrameworkFiles) assert.deepEqual(readFileSync(join(output, dirname(after.xshell), name)), bytes);
        assert.notEqual(after.packages.find(path => path.startsWith("child/")), before.packages.find(path => path.startsWith("child/")));
        assert.equal(after.packages.find(path => path.startsWith("other/")), before.packages.find(path => path.startsWith("other/")));
        assert.equal(descriptor(output, after.packages.find(path => path.startsWith("other/"))).modules.other.builtAt, originalOther);
        assertUtcTimestamp(descriptor(output, after.packages.find(path => path.startsWith("child/"))).modules.child.builtAt);
    } finally {
        rmSync(root, { recursive: true, force: true });
    }
});

test("invalid graphs and build failures leave the committed release unchanged", () => {
    const root = fixture();
    try {
        const sources = join(root, "sources");
        const output = join(root, "dist");
        module(join(sources, "app"), "app");
        successfulPack(join(sources, "app"), output);
        const committed = readFileSync(join(output, "release.json"), "utf8");
        module(join(sources, "app"), "app", { missing: { configUrl: "url:../missing/module.jsonc" } });
        assert.match(pack(join(sources, "app"), output).stderr, /Missing module configuration/);
        module(join(sources, "child"), "child", { app: { configUrl: "url:../app/module.jsonc" } });
        module(join(sources, "app"), "app", { child: { configUrl: "url:../child/module.jsonc" } });
        assert.match(pack(join(sources, "app"), output).stderr, /cycle/i);
        module(join(sources, "child"), "wrong");
        assert.match(pack(join(sources, "app"), output).stderr, /defines local module 'wrong'/);
        module(join(sources, "child"), "child");
        module(join(sources, "app"), "app", { child: { configUrl: "url:../child/module.jsonc" } });
        writeFileSync(join(sources, "child", "pages.html"), "<div></div>");
        writeFileSync(join(sources, "child", "pages.js"), "export default {};");
        assert.match(pack(join(sources, "app"), output).stderr, /Conflicting module resources/);
        assert.equal(readFileSync(join(output, "release.json"), "utf8"), committed);
        assert.equal(readdirSync(root).some(name => name.includes("-pack-")), false);
    } finally {
        rmSync(root, { recursive: true, force: true });
    }
});

test("framework packaging retains its distinct descriptor and ZIP format", () => {
    const root = fixture();
    try {
        const source = join(root, "xshell");
        mkdirSync(source);
        writeFileSync(join(source, "xshell.jsonc"), '// comment\n{"xshell":{"version":"2.0.0"}}');
        writeFileSync(join(source, "bootstrap.js"), "console.log('bootstrap');");
        writeFileSync(join(source, "sw.js"), "console.log('worker');");
        writeFileSync(join(source, "xshell.js"), "console.log('x');");
        const output = join(root, "dist");
        const release = successfulPack(source, output);
        assertUtcTimestamp(release.createdAt);
        assert.equal(release.root.startsWith("xshell/2.0.0."), true);
        assert.equal(release.xshell, release.root);
        assert.deepEqual(release.packages, [release.xshell]);
        assert.equal(existsSync(join(output, "index.html")), false);
        assert.equal(existsSync(join(output, "sw.js")), false);
        const published = descriptor(output, release.root);
        assertUtcTimestamp(published.xshell.builtAt);
        assert.equal(published.xshell.assetsUrl, "url:./xshell.zip");
        const archive = entries(join(output, dirname(release.root), "xshell.zip"));
        assert.equal(archive.has("xshell.json"), true);
        assert.deepEqual(readdirSync(join(output, dirname(release.root))).sort(), ["bootstrap.js", "sw.js", "xshell.json", "xshell.zip"]);
        for (const name of ["bootstrap.js", "sw.js"]) assert.deepEqual(readFileSync(join(output, dirname(release.root), name)), archive.get(name));
        assert.notDeepEqual(readFileSync(join(output, release.root)), archive.get("xshell.json"));
        assert.equal(contentHash(archive), published.xshell.hash);
        assert.equal(JSON.parse(archive.get("xshell.json")).xshell.hash, "");
        assert.equal(Object.hasOwn(JSON.parse(archive.get("xshell.json")).xshell, "builtAt"), false);
        const originalDescriptor = readFileSync(join(output, release.root));
        const originalZip = readFileSync(join(output, dirname(release.root), "xshell.zip"));
        const originalFiles = ["bootstrap.js", "sw.js"].map(name => [name, readFileSync(join(output, dirname(release.root), name))]);
        writeFileSync(join(source, "xshell.jsonc"), '{"xshell":{"version":"2.0.0","builtAt":"2000-01-01T00:00:00Z"}}');
        const repeated = successfulPack(source, output);
        assert.equal(repeated.root, release.root);
        assert.equal(repeated.xshell, release.xshell);
        assert.deepEqual(readFileSync(join(output, release.root)), originalDescriptor);
        assert.deepEqual(readFileSync(join(output, dirname(release.root), "xshell.zip")), originalZip);
        for (const [name, bytes] of originalFiles) assert.deepEqual(readFileSync(join(output, dirname(release.root), name)), bytes);
        writeFileSync(join(source, "xshell.jsonc"), '{"app":{},"xshell":{"version":"2.0.0"}}');
        assert.match(pack(source, output).stderr, /must not define top-level app/);
        assert.equal(existsSync(join(output, "index.html")), false);
    } finally {
        rmSync(root, { recursive: true, force: true });
    }
});

test("framework packaging requires both canonical bootstrap files", () => {
    const root = fixture();
    try {
        const source = join(root, "xshell");
        mkdirSync(source);
        writeFileSync(join(source, "xshell.json"), '{"xshell":{"version":"2.0.0"}}');
        const output = join(root, "dist");
        assert.match(pack(source, output).stderr, /missing required bootstrap\.js/);
        writeFileSync(join(source, "bootstrap.js"), "bootstrap");
        assert.match(pack(source, output).stderr, /missing required sw\.js/);
        assert.equal(existsSync(join(output, "release.json")), false);
    } finally {
        rmSync(root, { recursive: true, force: true });
    }
});

test("equivalent JSON and JSONC property orders produce the same package identity", () => {
    const root = fixture();
    try {
        const first = join(root, "first");
        const second = join(root, "second");
        module(first, "app", {}, { label: "Application" });
        mkdirSync(second);
        writeFileSync(join(second, "module.json"), '{"modules":{"app":{"label":"Application","version":"1.0.0"}}}');
        writeFileSync(join(second, "resource.txt"), "app");
        const a = successfulPack(first, join(root, "output-a"));
        const b = successfulPack(second, join(root, "output-b"));
        assert.equal(a.root, b.root);
        assert.deepEqual(a.packages, b.packages);
    } finally {
        rmSync(root, { recursive: true, force: true });
    }
});

test("conflicting identities and unsupported dependency URLs fail before publication", () => {
    const root = fixture();
    try {
        const sources = join(root, "sources");
        const output = join(root, "dist");
        module(join(sources, "one"), "shared");
        module(join(sources, "two"), "shared");
        module(join(sources, "app"), "app", {
            shared: { configUrl: "url:../one/module.jsonc" },
            branch: { configUrl: "url:../branch/module.jsonc" }
        });
        module(join(sources, "branch"), "branch", { shared: { configUrl: "url:../two/module.jsonc" } });
        assert.match(pack(join(sources, "app"), output).stderr, /conflicting configUrl/);
        assert.equal(existsSync(join(output, "release.json")), false);
        module(join(sources, "app"), "app", { shared: { configUrl: "url:https://example.invalid/shared/module.json" } });
        assert.match(pack(join(sources, "app"), output).stderr, /Remote module dependency/);
        module(join(sources, "app"), "app", { shared: { params: { mode: "compact" } } });
        assert.match(pack(join(sources, "app"), output).stderr, /exactly one local module definition/);
        assert.equal(existsSync(join(output, "release.json")), false);
    } finally {
        rmSync(root, { recursive: true, force: true });
    }
});

test("already packaged immutable dependencies are rejected instead of recompiled", () => {
    const root = fixture();
    try {
        const childSource = join(root, "child-source");
        const appSource = join(root, "app-source");
        module(childSource, "child");
        const childOutput = join(root, "child-dist");
        const childRelease = successfulPack(childSource, childOutput);
        module(appSource, "app", { child: { configUrl: "url:" + pathToFileURL(join(childOutput, childRelease.root)).href } });
        const output = join(root, "app-dist");
        assert.match(pack(appSource, output).stderr, /Immutable package dependency/);
        assert.equal(existsSync(join(output, "release.json")), false);
    } finally {
        rmSync(root, { recursive: true, force: true });
    }
});

test("an immutable package collision cannot replace the committed release", () => {
    const root = fixture();
    try {
        const sources = join(root, "sources");
        const output = join(root, "dist");
        module(join(sources, "child"), "child");
        module(join(sources, "app"), "app", { child: { configUrl: "url:../child/module.jsonc" } });
        const release = successfulPack(join(sources, "app"), output);
        const committed = readFileSync(join(output, "release.json"), "utf8");
        const child = release.packages.find(path => path.startsWith("child/"));
        writeFileSync(join(output, child), '{"modules":{"child":{"hash":"corrupt"}}}');
        assert.match(pack(join(sources, "app"), output).stderr, /collides with different or damaged content/);
        assert.equal(readFileSync(join(output, "release.json"), "utf8"), committed);
    } finally {
        rmSync(root, { recursive: true, force: true });
    }
});

test("a damaged XShell package leaves the committed application release selected", () => {
    const root = fixture();
    try {
        const source = join(root, "app");
        const output = join(root, "dist");
        module(source, "app");
        const release = successfulPack(source, output);
        const committed = readFileSync(join(output, "release.json"), "utf8");
        const framework = descriptor(output, release.xshell);
        framework.xshell.assetsUrl = "url:./other.zip";
        writeFileSync(join(output, release.xshell), JSON.stringify(framework));
        assert.match(pack(source, output).stderr, /collides with different or damaged content/);
        assert.equal(readFileSync(join(output, "release.json"), "utf8"), committed);
    } finally {
        rmSync(root, { recursive: true, force: true });
    }
});

test("XShell bootstrap copies must remain complete and unchanged on reuse", () => {
    const root = fixture();
    try {
        const source = join(root, "app");
        const output = join(root, "dist");
        module(source, "app");
        const release = successfulPack(source, output);
        const committed = readFileSync(join(output, "release.json"), "utf8");
        const frameworkDirectory = join(output, dirname(release.xshell));
        for (const name of ["bootstrap.js", "sw.js"]) {
            const path = join(frameworkDirectory, name);
            const original = readFileSync(path);
            writeFileSync(path, "modified");
            assert.match(pack(source, output).stderr, /collides with different or damaged content/);
            assert.equal(readFileSync(join(output, "release.json"), "utf8"), committed);
            writeFileSync(path, original);
            rmSync(path);
            assert.match(pack(source, output).stderr, /incomplete or unrecognized representation/);
            assert.equal(readFileSync(join(output, "release.json"), "utf8"), committed);
            writeFileSync(path, original);
        }
        writeFileSync(join(frameworkDirectory, "extra.txt"), "unexpected");
        assert.match(pack(source, output).stderr, /incomplete or unrecognized representation/);
        assert.equal(readFileSync(join(output, "release.json"), "utf8"), committed);
    } finally {
        rmSync(root, { recursive: true, force: true });
    }
});

test("root startup copies and exact package representation are required for immutable reuse", () => {
    const root = fixture();
    try {
        const source = join(root, "app");
        const output = join(root, "dist");
        module(source, "app");
        const release = successfulPack(source, output);
        const committed = readFileSync(join(output, "release.json"), "utf8");
        const directory = join(output, dirname(release.root));
        for (const name of ["index.html", "sw.js"]) {
            const path = join(directory, name);
            const original = readFileSync(path);
            writeFileSync(path, "modified");
            assert.match(pack(source, output).stderr, /collides with different or damaged content/);
            assert.equal(readFileSync(join(output, "release.json"), "utf8"), committed);
            writeFileSync(path, original);
            rmSync(path);
            assert.match(pack(source, output).stderr, /incomplete or unrecognized representation/);
            assert.equal(readFileSync(join(output, "release.json"), "utf8"), committed);
            writeFileSync(path, original);
        }
        writeFileSync(join(directory, "extra.txt"), "unexpected");
        assert.match(pack(source, output).stderr, /incomplete or unrecognized representation/);
        assert.equal(readFileSync(join(output, "release.json"), "utf8"), committed);
    } finally {
        rmSync(root, { recursive: true, force: true });
    }
});

test("reuse rejects invalid build times and changed external metadata without changing the release", () => {
    const root = fixture();
    try {
        const source = join(root, "app");
        const output = join(root, "dist");
        module(source, "app");
        const release = successfulPack(source, output);
        const committed = readFileSync(join(output, "release.json"), "utf8");
        const original = readFileSync(join(output, release.root), "utf8");
        const published = JSON.parse(original);

        published.modules.app.builtAt = "not-utc";
        writeFileSync(join(output, release.root), JSON.stringify(published));
        assert.match(pack(source, output).stderr, /invalid builtAt timestamp/);
        assert.equal(readFileSync(join(output, "release.json"), "utf8"), committed);

        published.modules.app.builtAt = JSON.parse(original).modules.app.builtAt;
        published.modules.app.assetsUrl = "url:./other.zip";
        writeFileSync(join(output, release.root), JSON.stringify(published));
        assert.match(pack(source, output).stderr, /collides with different or damaged content/);
        assert.equal(readFileSync(join(output, "release.json"), "utf8"), committed);
    } finally {
        rmSync(root, { recursive: true, force: true });
    }
});

test("legacy immutable packages without builtAt remain reusable and unchanged", () => {
    const root = fixture();
    try {
        const source = join(root, "app");
        const output = join(root, "dist");
        module(source, "app");
        const release = successfulPack(source, output);
        const published = descriptor(output, release.root);
        delete published.modules.app.builtAt;
        writeFileSync(join(output, release.root), JSON.stringify(published));
        const legacyDescriptor = readFileSync(join(output, release.root));
        const repeated = successfulPack(source, output);
        assert.equal(repeated.root, release.root);
        assert.deepEqual(readFileSync(join(output, release.root)), legacyDescriptor);
    } finally {
        rmSync(root, { recursive: true, force: true });
    }
});

test("reuse rejects a damaged ZIP even when the external descriptor is unchanged", () => {
    const root = fixture();
    try {
        const source = join(root, "app");
        const output = join(root, "dist");
        module(source, "app");
        const release = successfulPack(source, output);
        const committed = readFileSync(join(output, "release.json"), "utf8");
        const archivePath = join(output, dirname(release.root), "module.zip");
        const archive = readFileSync(archivePath);
        writeFileSync(archivePath, archive.subarray(0, archive.length - 1));
        assert.notEqual(pack(source, output).status, 0);
        assert.equal(readFileSync(join(output, "release.json"), "utf8"), committed);
    } finally {
        rmSync(root, { recursive: true, force: true });
    }
});

test("a collision after publishing one new package leaves the old release selected", () => {
    const root = fixture();
    try {
        const sources = join(root, "sources");
        const output = join(root, "dist");
        module(join(sources, "child"), "child");
        module(join(sources, "app"), "app", { child: { configUrl: "url:../child/module.jsonc" } });
        successfulPack(join(sources, "app"), output);
        const committed = readFileSync(join(output, "release.json"), "utf8");
        writeFileSync(join(sources, "child", "resource.txt"), "changed");
        const candidate = successfulPack(join(sources, "app"), join(root, "candidate"));
        const child = candidate.packages.find(path => path.startsWith("child/"));
        mkdirSync(join(output, dirname(child)), { recursive: true });
        writeFileSync(join(output, child), "invalid");
        writeFileSync(join(output, dirname(child), "module.zip"), "invalid");
        assert.match(pack(join(sources, "app"), output).stderr, /collides with different or damaged content/);
        assert.equal(readFileSync(join(output, "release.json"), "utf8"), committed);
        assert.equal(existsSync(join(output, candidate.root)), true);
    } finally {
        rmSync(root, { recursive: true, force: true });
    }
});

test("concurrent publication attempts preserve a complete release", async () => {
    const root = fixture();
    try {
        const source = join(root, "app");
        const output = join(root, "dist");
        module(source, "app");
        const run = () => new Promise(resolve => {
            const child = spawn("dotnet", [assemblyPath, "pack", source, output]);
            let stderr = "";
            child.stderr.on("data", chunk => { stderr += chunk; });
            child.on("close", status => resolve({ status, stderr }));
        });
        const results = await Promise.all([run(), run()]);
        assert.equal(results.some(result => result.status === 0), true);
        for (const result of results.filter(result => result.status !== 0)) assert.match(result.stderr, /publication lock/);
        const release = JSON.parse(readFileSync(join(output, "release.json"), "utf8"));
        assert.deepEqual(release.packages, [release.root, release.xshell]);
        assert.equal(existsSync(join(output, release.root)), true);
        assert.equal(existsSync(join(output, dirname(release.root), "module.zip")), true);
        assert.equal(existsSync(join(output, release.xshell)), true);
        assert.equal(existsSync(archivePath(output, release.xshell)), true);
        assert.equal(existsSync(join(output, ".pack.lock")), false);
    } finally {
        rmSync(root, { recursive: true, force: true });
    }
});

test("deployment bootstrap options create distinct root generations without changing dependency or XShell identities", () => {
    const root = fixture();
    try {
        const source = join(root, "app");
        const output = join(root, "dist");
        const child = join(root, "child");
        module(child, "child");
        module(source, "app", { child: { configUrl: "url:../child/module.jsonc" } });
        const original = successfulPack(source, output);
        const originalDependency = original.packages.find(path => path.startsWith("child/"));
        const check = args => {
            const release = successfulPack(source, output, args);
            assert.notEqual(release.root, original.root);
            assert.equal(release.xshell, original.xshell);
            assert.equal(release.packages.find(path => path.startsWith("child/")), originalDependency);
            return release;
        };
        const packed = check(["--app-base-path", "/demo", "--environment", "Staging",
            "--param", "tenant=acme & co", "--param", "mode=a=b", "--temp-base-path", "/uploads"]);
        const rootDirectory = join(output, dirname(packed.root));
        const index = readFileSync(join(rootDirectory, "index.html"), "utf8");
        const worker = readFileSync(join(rootDirectory, "sw.js"), "utf8");
        assert.ok(index.includes(`content="/demo/${packed.root}"`));
        assert.ok(index.includes(`src="/demo/${dirname(packed.xshell)}/bootstrap.js"`));
        assert.match(index, /xshell:app.basePath"\s+content="\/demo"/);
        assert.match(index, /xshell:xshell.environment"\s+content="Staging"/);
        assert.match(index, /xshell:xshell.temp.url"\s+content="\/uploads"/);
        assert.match(index, /tenant=acme%20%26%20co&amp;mode=a%3Db/);
        assert.ok(worker.includes(`importScripts("/demo/${dirname(packed.xshell)}/sw.js")`));
        const defaultTemp = check(["--app-base-path", "/demo"]);
        assert.match(readFileSync(join(output, dirname(defaultTemp.root), "index.html"), "utf8"), /xshell:xshell.temp.url"\s+content="\/demo\/temp"/);
        for (const args of [["--environment", "Staging"], ["--param", "tenant=other"], ["--temp-base-path", "/uploads"]]) check(args);
        const canonical = entries(archivePath(output, packed.root));
        const canonicalIndex = canonical.get("index.html").toString("utf8");
        assert.equal(canonicalIndex.includes("/demo/app/1.0.0.__XSHELL_ROOT_HASH__/module.json"), true);
        assert.equal(index.includes(`/demo/app/1.0.0.${dirname(packed.root).split(".").at(-1)}/module.json`), true);
        assert.equal(contentHash(canonical), dirname(packed.root).split(".").at(-1));
        const before = {
            descriptor: readFileSync(join(output, packed.root)), index: readFileSync(join(rootDirectory, "index.html")),
            worker: readFileSync(join(rootDirectory, "sw.js")), zip: readFileSync(archivePath(output, packed.root))
        };
        const repeated = successfulPack(source, output, ["--app-base-path", "/demo", "--environment", "Staging",
            "--param", "tenant=acme & co", "--param", "mode=a=b", "--temp-base-path", "/uploads"]);
        assert.equal(repeated.root, packed.root);
        assert.deepEqual(readFileSync(join(output, packed.root)), before.descriptor);
        assert.deepEqual(readFileSync(join(rootDirectory, "index.html")), before.index);
        assert.deepEqual(readFileSync(join(rootDirectory, "sw.js")), before.worker);
        assert.deepEqual(readFileSync(archivePath(output, packed.root)), before.zip);
        const otherOutput = join(root, "another-dist");
        const elsewhere = successfulPack(source, otherOutput, ["--app-base-path", "/demo", "--environment", "Staging",
            "--param", "tenant=acme & co", "--param", "mode=a=b", "--temp-base-path", "/uploads"]);
        assert.equal(elsewhere.root, packed.root);
        assert.equal(elsewhere.xshell, packed.xshell);
        assert.deepEqual(readFileSync(archivePath(otherOutput, elsewhere.root)), before.zip);
        for (const name of ["index.html", "sw.js"]) assert.deepEqual(readFileSync(join(otherOutput, dirname(elsewhere.root), name)), before[name === "index.html" ? "index" : "worker"]);
        assert.equal(existsSync(join(output, "index.html")), false);
        assert.equal(existsSync(join(output, "sw.js")), false);
    } finally {
        rmSync(root, { recursive: true, force: true });
    }
});

test("root self-hash publication preserves placeholder text in user bootstrap values", () => {
    const root = fixture();
    try {
        const source = join(root, "app");
        const output = join(root, "dist");
        module(source, "app");
        const args = ["--app-base-path", "/demo", "--environment", "__XSHELL_ROOT_HASH__", "--param", "token=__XSHELL_ROOT_HASH__",
            "--temp-base-path", "/__XSHELL_ROOT_HASH__"];
        const release = successfulPack(source, output, args);
        const hash = dirname(release.root).split(".").at(-1);
        const directory = join(output, dirname(release.root));
        const archive = entries(archivePath(output, release.root));
        const canonicalIndex = archive.get("index.html").toString("utf8");
        const publishedIndex = readFileSync(join(directory, "index.html"), "utf8");
        assert.equal(metaContent(canonicalIndex, "xshell:app.configPath"), "/demo/app/1.0.0.__XSHELL_ROOT_HASH__/module.json");
        assert.equal(metaContent(publishedIndex, "xshell:app.configPath"), `/demo/app/1.0.0.${hash}/module.json`);
        for (const index of [canonicalIndex, publishedIndex]) {
            assert.equal(metaContent(index, "xshell:app.params"), "token=__XSHELL_ROOT_HASH__");
            assert.equal(metaContent(index, "xshell:xshell.environment"), "__XSHELL_ROOT_HASH__");
            assert.equal(metaContent(index, "xshell:xshell.temp.url"), "/__XSHELL_ROOT_HASH__");
        }
        assert.equal(contentHash(archive), hash);
        assert.deepEqual(readFileSync(join(directory, "sw.js")), archive.get("sw.js"));
        const before = {
            descriptor: readFileSync(join(output, release.root)), index: readFileSync(join(directory, "index.html")),
            worker: readFileSync(join(directory, "sw.js")), zip: readFileSync(archivePath(output, release.root))
        };
        const repeated = successfulPack(source, output, args);
        assert.equal(repeated.root, release.root);
        assert.deepEqual(readFileSync(join(output, release.root)), before.descriptor);
        assert.deepEqual(readFileSync(join(directory, "index.html")), before.index);
        assert.deepEqual(readFileSync(join(directory, "sw.js")), before.worker);
        assert.deepEqual(readFileSync(archivePath(output, release.root)), before.zip);
    } finally {
        rmSync(root, { recursive: true, force: true });
    }
});

test("pack selects bundled XShell resource sets without changing package or release paths", () => {
    const root = fixture();
    try {
        const source = join(root, "app");
        const child = join(root, "child");
        module(child, "child");
        module(source, "app", { child: { configUrl: "url:../child/module.jsonc" } });
        const copy = join(root, "tool-copy");
        cpSync(dirname(assemblyPath), copy, { recursive: true });
        const defaultResources = join(copy, "Resources", "DProjects.XShell");
        for (const name of ["A", "B"]) {
            const selected = join(copy, "Resources", name);
            cpSync(defaultResources, selected, { recursive: true });
            const worker = join(selected, "xshell", "sw.js");
            writeFileSync(worker, readFileSync(worker, "utf8") + `\n// resource set ${name}\n`);
        }
        const selected = [];
        for (const name of ["A", "B"]) {
            const output = join(root, `dist-${name}`);
            const result = packWithAssembly(join(copy, "DProjects.XShell.dll"), source, output, ["--resources-name", name]);
            assert.equal(result.status, 0, result.stdout + result.stderr);
            const release = JSON.parse(readFileSync(join(output, "release.json"), "utf8"));
            assert.match(release.xshell, /^xshell\/[^/]+\.[0-9a-f]{16}\/xshell\.json$/);
            assert.equal(release.packages.some(path => path.startsWith(name + "/")), false);
            assert.equal(existsSync(join(output, name)), false);
            assert.match(readFileSync(join(output, dirname(release.root), "sw.js"), "utf8"),
                new RegExp(dirname(release.xshell).replaceAll(".", "\\.") + "/sw\\.js"));
            selected.push(release);
        }
        assert.notEqual(selected[0].xshell, selected[1].xshell);
        assert.notEqual(selected[0].root, selected[1].root);
        assert.equal(selected[0].packages.find(path => path.startsWith("child/")), selected[1].packages.find(path => path.startsWith("child/")));
        mkdirSync(join(copy, "Resources", "NoXShell"));
        const noXShell = packWithAssembly(join(copy, "DProjects.XShell.dll"), source, join(root, "no-xshell"), ["--resources-name", "NoXShell"]);
        assert.notEqual(noXShell.status, 0);
        assert.match(noXShell.stderr, /Bundled XShell resources directory not found/);
        mkdirSync(join(copy, "Resources", "NoDescriptor", "xshell"), { recursive: true });
        const noDescriptor = packWithAssembly(join(copy, "DProjects.XShell.dll"), source, join(root, "no-descriptor"), ["--resources-name", "NoDescriptor"]);
        assert.notEqual(noDescriptor.status, 0);
        assert.match(noDescriptor.stderr, /Bundled XShell descriptor.*not found/);
    } finally {
        rmSync(root, { recursive: true, force: true });
    }
});

test("pack rejects invalid and missing resource-set names before changing the selected release", () => {
    const root = fixture();
    try {
        const source = join(root, "app");
        const output = join(root, "dist");
        module(source, "app");
        successfulPack(source, output);
        const committed = readFileSync(join(output, "release.json"), "utf8");
        for (const name of ["", ".", "..", "../other", "a/b", "a\\b"]) {
            const result = pack(source, output, ["--resources-name", name]);
            assert.notEqual(result.status, 0);
            assert.match(result.stderr, /Resources name.*single valid directory name/);
            assert.equal(readFileSync(join(output, "release.json"), "utf8"), committed);
        }
        const missing = pack(source, output, ["--resources-name", "Missing"]);
        assert.notEqual(missing.status, 0);
        assert.match(missing.stderr, /Bundled resource set directory not found/);
        assert.equal(readFileSync(join(output, "release.json"), "utf8"), committed);
    } finally {
        rmSync(root, { recursive: true, force: true });
    }
});

test("authored app metadata and invalid options do not commit a release", () => {
    const root = fixture();
    try {
        const source = join(root, "app");
        const output = join(root, "dist");
        module(source, "app");
        const original = successfulPack(source, output);
        const committed = readFileSync(join(output, "release.json"), "utf8");
        const index = readFileSync(join(output, dirname(original.root), "index.html"), "utf8");
        const worker = readFileSync(join(output, dirname(original.root), "sw.js"), "utf8");
        writeFileSync(join(source, "module.jsonc"), JSON.stringify({ app: { label: "obsolete" }, modules: { app: { version: "1.0.0" } } }));
        assert.match(pack(source, output).stderr, /must not define top-level app/);
        assert.equal(readFileSync(join(output, "release.json"), "utf8"), committed);
        assert.equal(readFileSync(join(output, dirname(original.root), "index.html"), "utf8"), index);
        assert.equal(readFileSync(join(output, dirname(original.root), "sw.js"), "utf8"), worker);
        for (const args of [["--app-base-path", "//wrong"], ["--temp-base-path", "relative"], ["--param", "missing-equals"]]) {
            assert.notEqual(pack(source, output, args).status, 0);
            assert.equal(readFileSync(join(output, "release.json"), "utf8"), committed);
        }

        assert.equal(original.root.startsWith("app/"), true);
    } finally {
        rmSync(root, { recursive: true, force: true });
    }
});
