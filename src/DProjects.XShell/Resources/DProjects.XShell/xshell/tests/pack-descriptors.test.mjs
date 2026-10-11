import assert from "node:assert/strict";
import { spawn, spawnSync } from "node:child_process";
import { createHash } from "node:crypto";
import { existsSync, mkdtempSync, mkdirSync, readFileSync, readdirSync, rmSync, statSync, writeFileSync } from "node:fs";
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

function pack(source, output) {
    return spawnSync("dotnet", [assemblyPath, "pack", "--source", source, "--output", output], { encoding: "utf8" });
}

function successfulPack(source, output) {
    const result = pack(source, output);
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
        assert.equal(release.packages.length, 5);
        assert.equal(release.root, release.packages.find(path => path.startsWith("app/")));
        assert.equal(release.packages.filter(path => path.startsWith("x/")).length, 1);
        for (const path of release.packages) {
            const directory = join(output, dirname(path));
            const archive = entries(join(directory, "module.zip"));
            assert.deepEqual(readdirSync(directory).sort(), ["module.json", "module.zip"]);
            assert.equal(archive.has("module.json"), true);
            assert.equal(archive.has("module.jsonc"), false);
            assert.equal(archive.has("module.files.json"), true);
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
        const orders = descriptor(output, release.packages.find(path => path.startsWith("orders/")));
        assert.deepEqual(orders.modules.customers.params, { theme: "blue" });
        assert.equal(readFileSync(join(sources, "app", "module.jsonc"), "utf8"), "// authored configuration\n" + authored);

        const priorPackages = new Map(release.packages.map(path => [path, {
            descriptor: readFileSync(join(output, path)), zip: readFileSync(join(output, dirname(path), "module.zip"))
        }]));
        writeFileSync(join(output, "release.json"), JSON.stringify({ ...release, createdAt: "2000-01-01T00:00:00Z" }));
        module(join(sources, "customers"), "customers", {}, { builtAt: "2030-01-01T00:00:00Z" });
        const repeated = successfulPack(join(sources, "app"), output);
        assert.deepEqual(repeated.packages, release.packages);
        assert.equal(repeated.root, release.root);
        assertUtcTimestamp(repeated.createdAt);
        assert.notEqual(repeated.createdAt, "2000-01-01T00:00:00Z");
        for (const path of release.packages) {
            assert.deepEqual(readFileSync(join(output, path)), priorPackages.get(path).descriptor);
            assert.deepEqual(readFileSync(join(output, dirname(path), "module.zip")), priorPackages.get(path).zip);
        }
        const otherOutput = join(root, "another-dist");
        const otherRelease = successfulPack(join(sources, "app"), otherOutput);
        assert.deepEqual(otherRelease.packages, release.packages);
        assert.equal(otherRelease.root, release.root);
        for (const path of release.packages) {
            assert.deepEqual(readFileSync(join(output, dirname(path), "module.zip")), readFileSync(join(otherOutput, dirname(path), "module.zip")));
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
        writeFileSync(join(sources, "child", "resource.txt"), "changed");
        const after = successfulPack(join(sources, "app"), output);
        assertUtcTimestamp(after.createdAt);
        assert.notEqual(after.root, before.root);
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
        writeFileSync(join(source, "xshell.js"), "console.log('x');");
        const output = join(root, "dist");
        const release = successfulPack(source, output);
        assertUtcTimestamp(release.createdAt);
        assert.equal(release.root.startsWith("xshell/2.0.0."), true);
        const published = descriptor(output, release.root);
        assertUtcTimestamp(published.xshell.builtAt);
        assert.equal(published.xshell.assetsUrl, "url:./xshell.zip");
        const archive = entries(join(output, dirname(release.root), "xshell.zip"));
        assert.equal(archive.has("xshell.json"), true);
        assert.equal(contentHash(archive), published.xshell.hash);
        assert.equal(Object.hasOwn(JSON.parse(archive.get("xshell.json")).xshell, "builtAt"), false);
        const originalDescriptor = readFileSync(join(output, release.root));
        const originalZip = readFileSync(join(output, dirname(release.root), "xshell.zip"));
        writeFileSync(join(source, "xshell.jsonc"), '{"xshell":{"version":"2.0.0","builtAt":"2000-01-01T00:00:00Z"}}');
        const repeated = successfulPack(source, output);
        assert.equal(repeated.root, release.root);
        assert.deepEqual(readFileSync(join(output, release.root)), originalDescriptor);
        assert.deepEqual(readFileSync(join(output, dirname(release.root), "xshell.zip")), originalZip);
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
            const child = spawn("dotnet", [assemblyPath, "pack", "--source", source, "--output", output]);
            let stderr = "";
            child.stderr.on("data", chunk => { stderr += chunk; });
            child.on("close", status => resolve({ status, stderr }));
        });
        const results = await Promise.all([run(), run()]);
        assert.equal(results.some(result => result.status === 0), true);
        for (const result of results.filter(result => result.status !== 0)) assert.match(result.stderr, /publication lock/);
        const release = JSON.parse(readFileSync(join(output, "release.json"), "utf8"));
        assert.deepEqual(release.packages, [release.root]);
        assert.equal(existsSync(join(output, release.root)), true);
        assert.equal(existsSync(join(output, dirname(release.root), "module.zip")), true);
        assert.equal(existsSync(join(output, ".pack.lock")), false);
    } finally {
        rmSync(root, { recursive: true, force: true });
    }
});
