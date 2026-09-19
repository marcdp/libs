# Module Specification

A `module.jsonc` file is declarative JSONC. Its `modules` object contains canonical module definitions; a root module may also provide `app` metadata.
A file may contribute nested `xshell` settings. Bootstrap records a resolved `url` on loaded definitions as source provenance.

```jsonc
{
    "modules": {
        "orders": {
            "label": "Orders",
            "version": "1.0.0",
            "styles": ["/css/orders.css"],
            "imports": {
                "customer": {
                    "url": "url:../customer/module.jsonc",
                    "params": { "region": "eu" }
                }
            },
            "contract": {
                "params": { "region": { "type": "string" } },
                "events": { "orderSaved": {} },
                "methods": { "refresh": {} },
                "intents": { "order.detail": { "params": { "orderId": { "type": "string" } } } }
            }
        }
    }
}
```

The `contract` block illustrates the intended communication categories; its exact JSONC field syntax is **provisional**, and the runtime does not
parse or enforce it. Do not treat the example as a supported schema.

## Definition and import

A definition holds metadata such as `label`, `version`, `icon`, `description`, and `tags`; resources and settings such as `styles`; imports; and
optionally a public contract. These are configuration data, not live runtime objects. The import key is a local instance name, not necessarily the
target definition identity. Two imports can point to one definition URL and supply different params. The definition should contribute once to
effective configuration.

Module-relative static paths use the checked-in `/_assets/<module>/...` namespace after normalization. The source `url` identifies the module
definition location. Both live instances share static resources.

## Public module contract (planned)

- **params** specify accepted inputs at live instance creation. They may describe names, simple types, defaults, and required status; values belong to
  the import/instance.
- **events** name public notifications emitted onto the XShell Bus. Local DOM events are a separate component concern.
- **methods** name operations requested on an instance through XShell mediation. Dispatch should support asynchronous results.
- **intents** name public navigation capabilities, such as `customer.detail` with `customerId`, leaving URLs and menus private to the owning module.

The exact descriptor syntax, type vocabulary, method dispatch API, intent mapping, validation, and failure behavior remain TODOs. Current
`Modules.init()` instead uses a `handler` class and `onCommand("load", ...)` from legacy dotted configuration; the checked-in sample demonstrates it.
The desired instance lifecycle is not yet settled.

See [Modules](../architecture/modules.md), [Configuration](../architecture/configuration.md), and [Root Module](application.md).
