import assert from "node:assert/strict";
import { spawn, spawnSync } from "node:child_process";
import { cpSync, mkdirSync, mkdtempSync, rmSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";
import test from "node:test";

const projectDirectory = dirname(fileURLToPath(new URL("../../../../DProjects.XShell.csproj", import.meta.url)));
const assemblyPath = join(projectDirectory, "bin", "Release", "net10.0", "DProjects.XShell.dll");

function ensureAssembly() {
    const result = spawnSync("dotnet", ["build", join(projectDirectory, "DProjects.XShell.csproj"), "--configuration", "Release", "--no-restore", "-m:1",
        "-p:UseSharedCompilation=false"], { encoding: "utf8" });
    assert.equal(result.status, 0, result.stdout + result.stderr);
}

function metaContent(index, name) {
    const match = index.match(new RegExp(`<meta\\s+name="${name}"\\s+content="([^"]*)"`));
    assert.notEqual(match, null, `Missing ${name} meta value`);
    return match[1];
}

function startServer(toolAssembly, args) {
    return new Promise((resolve, reject) => {
        const child = spawn("dotnet", [toolAssembly, "server", ...args], {
            env: { ...process.env, ASPNETCORE_URLS: "http://127.0.0.1:0", DOTNET_ENVIRONMENT: "Production" },
            windowsHide: true
        });
        let output = "";
        const timeout = setTimeout(() => {
            child.kill();
            reject(new Error("Timed out waiting for the XShell server to listen.\n" + output));
        }, 15000);
        const inspect = chunk => {
            output += chunk;
            const match = output.match(/Now listening on:\s+(http:\/\/[^\s]+)/);
            if (match) {
                clearTimeout(timeout);
                resolve({ child, url: match[1] });
            }
        };
        child.stdout.on("data", chunk => inspect(chunk.toString()));
        child.stderr.on("data", chunk => inspect(chunk.toString()));
        child.once("error", error => {
            clearTimeout(timeout);
            reject(error);
        });
        child.once("exit", code => {
            if (code !== null && !output.includes("Now listening on:")) {
                clearTimeout(timeout);
                reject(new Error(`XShell server exited with ${code}.\n${output}`));
            }
        });
    });
}

async function stopServer(child) {
    if (child.exitCode !== null) return;
    await new Promise(resolve => {
        child.once("exit", resolve);
        child.kill();
        setTimeout(() => child.kill("SIGKILL"), 2000).unref();
    });
}

test("server derives every default resource path from ResourcesName and preserves an explicit config path", async () => {
    ensureAssembly();
    mkdirSync(join(projectDirectory, "obj"), { recursive: true });
    const root = mkdtempSync(join(projectDirectory, "obj", "server-resources-"));
    try {
        const tool = join(root, "tool");
        cpSync(dirname(assemblyPath), tool, { recursive: true });
        cpSync(join(tool, "Resources", "DProjects.XShell"), join(tool, "Resources", "Custom"), { recursive: true });
        const defaults = await startServer(join(tool, "DProjects.XShell.dll"), ["--app-base-path", "/default", "--environment", "Production"]);
        try {
            const response = await fetch(defaults.url + "/default/");
            assert.equal(response.status, 200);
            const index = await response.text();
            assert.equal(metaContent(index, "xshell:app.configPath"), "/default/_resources/DProjects.XShell/x-demo/module.jsonc");
            assert.match(index, /<script src="\/default\/_resources\/DProjects\.XShell\/xshell\/bootstrap\.js"/);
        } finally {
            await stopServer(defaults.child);
        }
        const first = await startServer(join(tool, "DProjects.XShell.dll"),
            ["--resources-name", "Custom", "--app-base-path", "/demo", "--environment", "Production"]);
        try {
            const response = await fetch(first.url + "/demo/");
            assert.equal(response.status, 200);
            const index = await response.text();
            assert.equal(metaContent(index, "xshell:app.basePath"), "/demo");
            assert.equal(metaContent(index, "xshell:app.configPath"), "/demo/_resources/Custom/x-demo/module.jsonc");
            assert.match(index, /<script src="\/demo\/_resources\/Custom\/xshell\/bootstrap\.js"/);
        } finally {
            await stopServer(first.child);
        }
        const second = await startServer(join(tool, "DProjects.XShell.dll"),
            ["--resources-name", "Custom", "--app-base-path", "/demo", "--app-config-path", "/explicit/module.json", "--environment", "Production"]);
        try {
            const response = await fetch(second.url + "/demo/");
            assert.equal(response.status, 200);
            assert.equal(metaContent(await response.text(), "xshell:app.configPath"), "/explicit/module.json");
        } finally {
            await stopServer(second.child);
        }
    } finally {
        rmSync(root, { recursive: true, force: true });
    }
});
