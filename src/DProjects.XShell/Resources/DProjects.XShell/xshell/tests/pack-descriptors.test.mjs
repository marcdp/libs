import assert from "node:assert/strict";
import { spawnSync } from "node:child_process";
import { createHash } from "node:crypto";
import { existsSync, mkdtempSync, mkdirSync, readFileSync, readdirSync, renameSync, rmSync, statSync, writeFileSync } from "node:fs";
import { dirname, join, relative, sep } from "node:path";
import { fileURLToPath } from "node:url";
import test from "node:test";
import { inflateRawSync } from "node:zlib";

const projectPath = fileURLToPath(new URL("../../../../DProjects.XShell.csproj", import.meta.url));
const projectDirectory = dirname(projectPath);
const assemblyPath = join(projectDirectory, "bin", "Release", "net10.0", "DProjects.XShell.dll");
let assemblyReady = false;

function ensureAssembly() {
    if (assemblyReady) return;
    const packSourcePath = join(projectDirectory, "Commands", "Pack.cs");
    if (existsSync(assemblyPath) && statSync(assemblyPath).mtimeMs >= statSync(packSourcePath).mtimeMs) {
        assemblyReady = true;
        return;
    }
    const args = ["build", projectPath, "--configuration", "Release", "--no-restore", "-m:1", "-p:UseSharedCompilation=false"];
    const result = spawnSync("dotnet", args, {
        encoding: "utf8"
    });
    assert.equal(result.status, 0, `Unable to build pack test target.\n${result.stdout}\n${result.stderr}`);
    assemblyReady = true;
}

function writeDescriptor(directory, name, content) {
    mkdirSync(directory, { recursive: true });
    writeFileSync(join(directory, name), content);
}

function pack(source, output, zip = false) {
    const args = [assemblyPath, "pack", "--source", source, "--output", output];
    if (zip) args.push("--zip");
    const result = spawnSync("dotnet", args, { encoding: "utf8" });
    assert.equal(result.status, 0, `Pack failed.\n${result.stdout}\n${result.stderr}`);
    return result.stdout.trim().split(/\r?\n/).at(-1);
}

function packFailure(source, output, zip = false) {
    const args = [assemblyPath, "pack", "--source", source, "--output", output];
    if (zip) args.push("--zip");
    return spawnSync("dotnet", args, { encoding: "utf8" });
}

function zipEntries(path) {
    const archive = readFileSync(path);
    const entries = new Map();
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
        const method = archive.readUInt16LE(offset + 10);
        entries.set(name, method === 8 ? inflateRawSync(compressed) : compressed);
        offset += 46 + nameLength + extraLength + commentLength;
    }
    return entries;
}

function packageHash(entries) {
    const digest = createHash("sha256");
    for (const [name, content] of [...entries].sort(([a], [b]) => a < b ? -1 : a > b ? 1 : 0)) {
        const path = Buffer.from(name);
        const pathLength = Buffer.alloc(4);
        pathLength.writeInt32LE(path.length);
        const fileLength = Buffer.alloc(8);
        fileLength.writeBigInt64LE(BigInt(content.length));
        digest.update(pathLength).update(path).update(fileLength).update(content);
    }
    return digest.digest("hex").slice(0, 16);
}

function expandedEntries(root) {
    const entries = new Map();
    function visit(directory) {
        for (const entry of readdirSync(directory, { withFileTypes: true })) {
            const path = join(directory, entry.name);
            if (entry.isDirectory()) visit(path);
            else entries.set(relative(root, path).split(sep).join("/"), readFileSync(path));
        }
    }
    visit(root);
    return entries;
}

test("pack normalizes JSON and JSONC descriptors to canonical production filenames", () => {
    ensureAssembly();
    mkdirSync(join(projectDirectory, "obj"), { recursive: true });
    const root = mkdtempSync(join(projectDirectory, "obj", "pack-descriptors-"));
    try {
        const moduleJsonc = join(root, "module-jsonc");
        const moduleJson = join(root, "module-json");
        const moduleOutput = join(root, "module-output");
        writeDescriptor(moduleJsonc, "module.jsonc", `{
            // authored comment
            "modules": { "sample": { "version": "1.0.0", }, },
        }`);
        writeDescriptor(moduleJson, "module.json", `{
            "modules": { "sample": { "version": "1.0.0", }, },
        }`);

        const moduleJsoncPackage = pack(moduleJsonc, moduleOutput);
        const moduleJsonPackage = pack(moduleJson, moduleOutput);
        assert.equal(moduleJsonPackage, moduleJsoncPackage);
        assert.doesNotThrow(() => JSON.parse(readFileSync(join(moduleJsoncPackage, "module.json"), "utf8")));
        assert.equal(readFileSync(join(moduleJsoncPackage, "module.json"), "utf8").includes("authored comment"), false);
        assert.equal(existsSync(join(moduleJsoncPackage, "module.jsonc")), false);
        assert.equal(readFileSync(join(moduleJsoncPackage, "module.files.json"), "utf8").includes("module.jsonc"), false);

        const xshellJsonc = join(root, "xshell-jsonc");
        const xshellJson = join(root, "xshell-json");
        const xshellOutput = join(root, "xshell-output");
        writeDescriptor(xshellJsonc, "xshell.jsonc", `{
            // authored comment
            "xshell": { "version": "1.0.0", },
        }`);
        writeDescriptor(xshellJson, "xshell.json", `{
            "xshell": { "version": "1.0.0", },
        }`);

        const xshellJsoncPackage = pack(xshellJsonc, xshellOutput);
        const xshellJsonPackage = pack(xshellJson, xshellOutput);
        assert.equal(xshellJsonPackage, xshellJsoncPackage);
        assert.doesNotThrow(() => JSON.parse(readFileSync(join(xshellJsoncPackage, "xshell.json"), "utf8")));
        assert.equal(existsSync(join(xshellJsoncPackage, "xshell.jsonc")), false);
        assert.equal(readFileSync(join(xshellJsoncPackage, "module.files.json"), "utf8").includes("xshell.jsonc"), false);

        const xshellZipOutput = join(root, "xshell-zip-output");
        const xshellZipPackage = pack(xshellJsonc, xshellZipOutput, true);
        assert.doesNotThrow(() => JSON.parse(readFileSync(join(xshellZipPackage, "xshell.json"), "utf8")));
        assert.equal(readFileSync(join(xshellZipPackage, "xshell.json"), "utf8").includes('"assetsUrl": "url:./xshell.zip"'), true);
        assert.equal(readFileSync(join(xshellZipPackage, "xshell.json"), "utf8").includes('"files"'), true);
        assert.equal(existsSync(join(xshellZipPackage, "xshell.jsonc")), false);
        assert.equal(existsSync(join(xshellZipPackage, "xshell.zip")), true);
        const archiveEntries = zipEntries(join(xshellZipPackage, "xshell.zip"));
        assert.equal(archiveEntries.has("xshell.json"), true);
        assert.equal(archiveEntries.has("xshell.jsonc"), false);
        assert.equal(archiveEntries.has("module.files.json"), true);
        assert.equal(pack(xshellJson, xshellZipOutput, true), xshellZipPackage);

        // published package reuse recognizes only the canonical production descriptor
        renameSync(join(xshellZipPackage, "xshell.json"), join(xshellZipPackage, "xshell.jsonc"));
        const legacyReuse = packFailure(xshellJson, xshellZipOutput, true);
        assert.notEqual(legacyReuse.status, 0);
        assert.match(legacyReuse.stderr, /incomplete or unrecognized representation/);
    } finally {
        rmSync(root, { recursive: true, force: true });
    }
});

test("package hash includes the canonical descriptor and inventory for expanded and ZIP output", () => {
    ensureAssembly();
    mkdirSync(join(projectDirectory, "obj"), { recursive: true });
    const root = mkdtempSync(join(projectDirectory, "obj", "pack-hash-"));
    try {
        for (const kind of ["module", "xshell"]) {
            const source = join(root, kind);
            const descriptorName = `${kind}.json`;
            const own = kind === "module" ? config => config.modules.orders : config => config.xshell;
            const descriptor = kind === "module" ? { modules: { orders: { version: "1.4.0", hash: "authored" } } } :
                { xshell: { version: "0.9.0", hash: "authored" } };
            writeDescriptor(source, descriptorName, JSON.stringify(descriptor));
            writeDescriptor(join(source, "contracts"), "customer.json", '{"name":"customer"}');
            writeDescriptor(source, "module.files.json", '[{"path":"/stale"}]');

            const expanded = pack(source, join(root, `${kind}-expanded`));
            const zipped = pack(source, join(root, `${kind}-zip`), true);
            const suffix = expanded.split(/[\\/]/).at(-1).split(".").at(-1);
            assert.match(suffix, /^[0-9a-f]{16}$/);
            assert.equal(zipped.split(/[\\/]/).at(-1), expanded.split(/[\\/]/).at(-1));

            const publishedText = readFileSync(join(expanded, descriptorName), "utf8");
            const published = JSON.parse(publishedText);
            assert.equal(own(published).hash, suffix);
            const inventory = JSON.parse(readFileSync(join(expanded, "module.files.json"), "utf8"));
            assert.deepEqual(inventory.map(file => file.path), ["/contracts/customer.json"]);

            const canonical = expandedEntries(expanded);
            canonical.set(descriptorName, Buffer.from(publishedText.replace(`"hash": "${suffix}"`, '"hash": ""')));
            assert.equal(packageHash(canonical), suffix);

            const external = JSON.parse(readFileSync(join(zipped, descriptorName), "utf8"));
            assert.equal(own(external).hash, suffix);
            const archive = zipEntries(join(zipped, `${kind}.zip`));
            assert.equal(own(JSON.parse(archive.get(descriptorName))).hash, "");
            assert.equal(packageHash(archive), suffix);
            assert.equal(archive.has("module.files.json"), true);

            // an authored hash is ignored, while other descriptor content changes package identity
            own(descriptor).hash = "another-authored-value";
            writeDescriptor(source, descriptorName, JSON.stringify(descriptor));
            assert.equal(pack(source, join(root, `${kind}-expanded`)), expanded);
            own(descriptor).label = "Changed";
            writeDescriptor(source, descriptorName, JSON.stringify(descriptor));
            assert.notEqual(pack(source, join(root, `${kind}-expanded`)), expanded);
        }
    } finally {
        rmSync(root, { recursive: true, force: true });
    }
});
