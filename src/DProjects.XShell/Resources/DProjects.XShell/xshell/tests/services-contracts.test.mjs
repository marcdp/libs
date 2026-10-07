import assert from "node:assert/strict";
import test from "node:test";

import ToastContract from "../../x/contracts/toast.json" with { type: "json" };
import Toast from "../../x/services/toast-default.js";
import Contracts from "../contracts.js";
import Services from "../services.js";
import validateContract from "../validation/contract.js";

function createContractItem(id, methods = {}, properties = {}) {
    return {
        id,
        url: `/_assets/contracts/${id}.json`,
        moduleId: "contracts",
        contract: { label: id, description: `${id} contract`, methods, properties },
        size: 1,
        status: "loaded",
        time: 0
    };
}

function createServices(definitions, implementations, contractItems) {
    const loads = [];
    const emitted = [];
    const config = {
        app: { basePath: "/app" },
        modules: {
            test: {
                files: Object.values(definitions).map((service, index) => ({ path: service.implementation, size: index + 1 }))
            }
        },
        xshell: { services: definitions }
    };
    const services = new Services({
        config,
        loader: {
            async load(resource) {
                loads.push(resource);
                return implementations[resource];
            }
        },
        contracts: {
            getContractItemById(id) {
                return contractItems[id];
            }
        },
        areas: {
            getModuleId() {
                return "test";
            }
        },
        bus: { emit(type, detail) { emitted.push({ type, detail, state: services.getServiceInfo(detail.id).state }); } }
    });
    return { loads, services, emitted };
}

test("configured services load eagerly but instantiate lazily as singletons", async () => {
    let constructions = 0;
    class ExampleService {

        constructor() {
            constructions++;
        }

        execute() {}
    }
    const definitions = { example: { contract: "example", implementation: "/_assets/test/services/example.js" } };
    const implementations = { "module:/_assets/test/services/example.js": ExampleService };
    const contractItems = { example: createContractItem("example", { execute: {} }) };
    const { loads, services } = createServices(definitions, implementations, contractItems);

    await services.init();

    assert.deepEqual(loads, ["module:/_assets/test/services/example.js"]);
    assert.equal(constructions, 0);
    assert.equal(services.getServiceInfo("example").state, "registered");

    const first = services.resolve("example");
    const second = services.resolve("example");

    assert.equal(constructions, 1);
    assert.equal(first, second);
    assert.equal(services.getServiceInfo("example").state, "created");
});

test("successful lazy creation emits once after validation with only the service ID", async () => {
    class ExampleService { execute() {} }
    const definitions = { example: { contract: "example", implementation: "/_assets/test/services/example.js" } };
    const implementations = { "module:/_assets/test/services/example.js": ExampleService };
    const contractItems = { example: createContractItem("example", { execute: {} }) };
    const { services, emitted } = createServices(definitions, implementations, contractItems);

    await services.init();
    assert.deepEqual(emitted, []);
    services.resolve("example");
    services.resolve("example");
    assert.deepEqual(emitted, [{ type: "xshell:service:created", detail: { id: "example" }, state: "created" }]);
});

test("failed lazy construction emits no created event and can be retried", async () => {
    let attempts = 0;
    class ExampleService {
        constructor() {
            if (++attempts === 1) throw new Error("construction failed");
        }
    }
    const definitions = { example: { contract: "example", implementation: "/_assets/test/services/example.js" } };
    const implementations = { "module:/_assets/test/services/example.js": ExampleService };
    const contractItems = { example: createContractItem("example") };
    const { services, emitted } = createServices(definitions, implementations, contractItems);

    await services.init();
    assert.throws(() => services.resolve("example"), /construction failed/);
    assert.equal(services.getServiceInfo("example").state, "registered");
    assert.deepEqual(emitted, []);
    services.resolve("example");
    assert.deepEqual(emitted, [{ type: "xshell:service:created", detail: { id: "example" }, state: "created" }]);
});

test("service inspection returns independent read-only metadata snapshots", async () => {
    class ExampleService {

        execute() {
            return 42;
        }
    }
    const definitions = { example: { contract: "example", implementation: "/_assets/test/services/example.js" } };
    const implementations = { "module:/_assets/test/services/example.js": ExampleService };
    const contractItems = { example: createContractItem("example", { execute: {} }) };
    const runtime = { active: true };
    const { services } = createServices(definitions, implementations, contractItems);
    services.register("runtime", runtime);
    await services.init();

    const snapshot = services.registry;
    const example = snapshot.find(item => item.id === "example");
    assert.ok(Array.isArray(snapshot));
    assert.ok(Object.isFrozen(snapshot));
    assert.ok(Object.isFrozen(example));
    assert.deepEqual(example, {
        id: "example",
        state: "registered",
        contractId: "example",
        contractUrl: "/_assets/contracts/example.json",
        description: "example contract",
        icon: null,
        moduleId: "test",
        implementationName: "ExampleService",
        url: "/app/_assets/test/services/example.js",
        size: 1,
        time: example.time
    });
    assert.equal(typeof example.time, "number");
    assert.equal("instance" in example, false);
    assert.equal("implementationItem" in example, false);
    assert.equal("contractItem" in example, false);
    assert.equal(Object.values(example).includes(ExampleService), false);
    assert.deepEqual(services.getServiceInfo("example"), example);
    assert.notStrictEqual(services.getServiceInfo("example"), example);
    assert.equal(services.getServiceInfo("missing"), undefined);
    assert.deepEqual(snapshot.find(item => item.id === "runtime"), {
        id: "runtime", state: "created", contractId: null, contractUrl: null, description: null, icon: null,
        moduleId: null, implementationName: null, url: null, size: null, time: null
    });

    assert.throws(() => { example.state = "broken"; }, TypeError);
    assert.throws(() => { snapshot[0] = { id: "broken" }; }, TypeError);
    assert.throws(() => { snapshot.push({ id: "broken" }); }, TypeError);
    assert.equal(services.getServiceInfo("example").state, "registered");
    assert.equal(services.has("broken"), false);
    assert.strictEqual(services.resolve("runtime"), runtime);
    assert.equal(services.resolve("example").execute(), 42);
    assert.equal(example.state, "registered");
    assert.equal(services.registry.find(item => item.id === "example").state, "created");
});

test("has reports runtime and configured services without constructing lazy implementations", async () => {
    let constructions = 0;
    class ExampleService {

        constructor() {
            constructions++;
        }
    }
    const definitions = { example: { contract: "example", implementation: "/_assets/test/services/example.js" } };
    const implementations = { "module:/_assets/test/services/example.js": ExampleService };
    const contractItems = { example: createContractItem("example") };
    const { services } = createServices(definitions, implementations, contractItems);
    services.register("runtime", { active: true });

    await services.init();

    assert.equal(services.has("runtime"), true);
    assert.equal(services.has("example"), true);
    assert.equal(services.has("missing"), false);
    assert.equal(constructions, 0);
});

test("service registration is rejected after initialization finalizes the registry", async () => {
    const { services } = createServices({}, {}, {});
    services.register("runtime", { active: true });

    await services.init();

    assert.equal(services.has("runtime"), true);
    assert.throws(() => services.register("late", {}), /Service registry is immutable after initialization\./);
    assert.equal(services.has("late"), false);
});

test("unknown contract references fail before implementation loading", async () => {
    const definitions = { toast: { contract: "toast", implementation: "/_assets/test/services/toast.js" } };
    const { loads, services } = createServices(definitions, {}, {});

    await assert.rejects(() => services.init(), /Service 'toast' references unknown contract 'toast'\./);
    assert.deepEqual(loads, []);
});

test("service creation rejects an implementation missing a required method", async () => {
    class InvalidService {}
    const definitions = { toast: { contract: "toast", implementation: "/_assets/test/services/toast.js" } };
    const implementations = { "module:/_assets/test/services/toast.js": InvalidService };
    const contractItems = { toast: createContractItem("toast", { show: {} }) };
    const { services, emitted } = createServices(definitions, implementations, contractItems);
    await services.init();

    assert.throws(() => services.resolve("toast"), /Service 'toast' does not implement required method 'show'\./);
    assert.equal(services.getServiceInfo("toast").state, "registered");
    assert.deepEqual(emitted, []);
});

test("service creation accepts a declared own property", async () => {
    class IdentityService {

        constructor() {
            this.currentUser = null;
        }
    }
    const definitions = { identity: { contract: "identity", implementation: "/_assets/test/services/identity.js" } };
    const implementations = { "module:/_assets/test/services/identity.js": IdentityService };
    const contractItems = { identity: createContractItem("identity", {}, { currentUser: { type: "object", readonly: true } }) };
    const { services } = createServices(definitions, implementations, contractItems);
    await services.init();

    assert.equal(services.resolve("identity").currentUser, null);
});

test("service creation accepts a prototype getter without executing it during validation", async () => {
    let getterCalls = 0;
    class IdentityService {

        get currentUser() {
            getterCalls++;
            throw new Error("getter must not execute during validation");
        }
    }
    const definitions = { identity: { contract: "identity", implementation: "/_assets/test/services/identity.js" } };
    const implementations = { "module:/_assets/test/services/identity.js": IdentityService };
    const contractItems = { identity: createContractItem("identity", {}, { currentUser: { type: "object", readonly: true } }) };
    const { services } = createServices(definitions, implementations, contractItems);
    await services.init();

    const identity = services.resolve("identity");

    assert.ok(identity instanceof IdentityService);
    assert.equal(getterCalls, 0);
});

test("service creation rejects an implementation missing a required property", async () => {
    class InvalidIdentityService {}
    const definitions = { identity: { contract: "identity", implementation: "/_assets/test/services/identity.js" } };
    const implementations = { "module:/_assets/test/services/identity.js": InvalidIdentityService };
    const contractItems = { identity: createContractItem("identity", {}, { currentUser: { type: "object", readonly: true } }) };
    const { services } = createServices(definitions, implementations, contractItems);
    await services.init();

    assert.throws(() => services.resolve("identity"), /Service 'identity' does not implement required property 'currentUser'\./);
    assert.equal(services.getServiceInfo("identity").state, "registered");
});

test("circular constructor dependencies fail with the service path", async () => {
    class ServiceA {

        constructor(services) {
            this.b = services.b;
        }
    }
    class ServiceB {

        constructor(services) {
            this.a = services.a;
        }
    }
    const definitions = {
        a: { contract: "a", implementation: "/_assets/test/services/a.js" },
        b: { contract: "b", implementation: "/_assets/test/services/b.js" }
    };
    const implementations = {
        "module:/_assets/test/services/a.js": ServiceA,
        "module:/_assets/test/services/b.js": ServiceB
    };
    const contractItems = { a: createContractItem("a"), b: createContractItem("b") };
    const { services } = createServices(definitions, implementations, contractItems);
    await services.init();

    assert.throws(() => services.resolve("a"), /Circular service dependency detected: a -> b -> a/);
});

test("different service names may use the same contract", async () => {
    class FirstService {

        execute() {
            return "first";
        }
    }
    class SecondService {

        execute() {
            return "second";
        }
    }
    const definitions = {
        first: { contract: "shared", implementation: "/_assets/test/services/first.js" },
        second: { contract: "shared", implementation: "/_assets/test/services/second.js" }
    };
    const implementations = {
        "module:/_assets/test/services/first.js": FirstService,
        "module:/_assets/test/services/second.js": SecondService
    };
    const contractItems = { shared: createContractItem("shared", { execute: {} }) };
    const { services } = createServices(definitions, implementations, contractItems);
    await services.init();

    assert.equal(services.resolve("first").execute(), "first");
    assert.equal(services.resolve("second").execute(), "second");
});

test("services receive other lazily-created services through constructor injection", async () => {
    class DependencyService {

        getValue() {
            return 42;
        }
    }
    class ConsumerService {

        constructor({ dependency }) {
            this.dependency = dependency;
        }

        execute() {
            return this.dependency.getValue();
        }
    }
    const definitions = {
        dependency: { contract: "dependency", implementation: "/_assets/test/services/dependency.js" },
        consumer: { contract: "consumer", implementation: "/_assets/test/services/consumer.js" }
    };
    const implementations = {
        "module:/_assets/test/services/dependency.js": DependencyService,
        "module:/_assets/test/services/consumer.js": ConsumerService
    };
    const contractItems = {
        dependency: createContractItem("dependency", { getValue: {} }),
        consumer: createContractItem("consumer", { execute: {} })
    };
    const { services } = createServices(definitions, implementations, contractItems);
    await services.init();

    assert.equal(services.resolve("consumer").execute(), 42);
    assert.equal(services.getServiceInfo("dependency").state, "created");
});

test("Contracts.init rejects duplicate global IDs defensively", async () => {
    const contract = createContractItem("identity").contract;
    const contracts = new Contracts({
        config: {
            app: { basePath: "/app" },
            modules: {
                x: { assetsPath: "/_assets/x", files: [{ path: "/_assets/x/contracts/identity.json", size: 1 }] },
                auth: { assetsPath: "/_assets/auth", files: [{ path: "/_assets/auth/contracts/identity.json", size: 2 }] }
            }
        },
        loader: { async load() { return contract; } }
    });

    await assert.rejects(
        () => contracts.init(),
        error => error.message.includes("Duplicate contract 'identity'") && error.message.includes("module 'x'") && error.message.includes("module 'auth'")
    );
});

test("Contracts.init ignores lookalike directories and non-JSON files", async () => {
    const loads = [];
    const contracts = new Contracts({
        config: {
            app: { basePath: "/app" },
            modules: {
                x: {
                    assetsPath: "/_assets/x",
                    files: [
                        { path: "/_assets/x/contracts/toast.json", size: 1 },
                        { path: "/_assets/x/contracts/readme.txt", size: 2 },
                        { path: "/_assets/x/contracts-old/legacy.json", size: 3 }
                    ]
                }
            }
        },
        loader: {
            async load(resource) {
                loads.push(resource);
                return createContractItem("toast").contract;
            }
        }
    });

    await contracts.init();

    assert.deepEqual(loads, ["contract:toast"]);
    assert.deepEqual(contracts.registry.map(item => item.id), ["toast"]);
    const registry = contracts.registry;
    assert.equal(Object.isFrozen(registry), true);
    assert.equal(Object.isFrozen(registry[0]), true);
    assert.equal(registry[0].label, "toast");
    assert.equal(Object.hasOwn(registry[0], "contract"), false);
    assert.throws(() => { registry[0].label = "changed"; }, TypeError);
    assert.equal(contracts.getContractById("toast").label, "toast");
});

test("toast emits shown and closed events with the toast ID", () => {
    const previousDocument = globalThis.document;
    const bodyChildren = [];
    const containerChildren = [];
    globalThis.document = {
        createElement() {
            return {
                className: "",
                dataset: {},
                textContent: "",
                append(element) {
                    containerChildren.push(element);
                },
                remove() {
                    const index = containerChildren.indexOf(this);
                    if (index !== -1) containerChildren.splice(index, 1);
                },
                setAttribute() {}
            };
        },
        body: {
            append(element) {
                bodyChildren.push(element);
            }
        }
    };
    try {
        const toast = new Toast();
        const events = [];
        toast.addEventListener("shown", event => events.push({ type: event.type, detail: event.detail, visible: containerChildren.length === 1 }));
        toast.addEventListener("closed", event => events.push({ type: event.type, detail: event.detail }));

        const id = toast.show("Saved", "success", 0);
        toast.close(id);

        assert.equal(bodyChildren.length, 1);
        assert.deepEqual(events, [
            { type: "shown", detail: { id }, visible: true },
            { type: "closed", detail: { id } }
        ]);
    } finally {
        globalThis.document = previousDocument;
    }
});

test("toast contract is structurally valid and its implementation exposes every declared method", async () => {
    await validateContract("toast.json", ToastContract);
    const toast = new Toast();

    for (const methodName of Object.keys(ToastContract.methods)) {
        assert.equal(typeof toast[methodName], "function", methodName);
    }
});
