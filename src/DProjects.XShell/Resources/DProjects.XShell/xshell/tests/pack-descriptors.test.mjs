import assert from "node:assert/strict";
import { spawnSync } from "node:child_process";
import { existsSync, mkdtempSync, mkdirSync, readFileSync, renameSync, rmSync, statSync, writeFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";
import test from "node:test";

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

function zipEntryNames(path) {
    const archive = readFileSync(path);
    const names = [];
    for (let offset = 0; offset <= archive.length - 46;) {
        if (archive.readUInt32LE(offset) !== 0x02014b50) {
            offset++;
            continue;
        }
        const nameLength = archive.readUInt16LE(offset + 28);
        const extraLength = archive.readUInt16LE(offset + 30);
        const commentLength = archive.readUInt16LE(offset + 32);
        names.push(archive.toString("utf8", offset + 46, offset + 46 + nameLength));
        offset += 46 + nameLength + extraLength + commentLength;
    }
    return names;
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
        const archiveEntries = zipEntryNames(join(xshellZipPackage, "xshell.zip"));
        assert.equal(archiveEntries.includes("xshell.json"), true);
        assert.equal(archiveEntries.includes("xshell.jsonc"), false);
        assert.equal(archiveEntries.includes("module.files.json"), true);
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
