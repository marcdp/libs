# Dialogs

The `dialog` service opens Pages through `navigation.navigate({ open: "dialog", ... })` and awaits their closing result.

| Method | Parameters and selected resource |
| --- | --- |
| `confirm({ title, message, variant })` | `xshell.ui.dialog.confirm` |
| `message({ type, title, message })` | `xshell.ui.dialog.message` |
| `prompt({ title, message, value, inputType, placeholder, required })` | `xshell.ui.dialog.prompt` |
| `picker({ title, message, value, inputType, placeholder, domain, multiple, required })` | `xshell.ui.dialog.picker` |
| `language({ title, message, value, required, current } = {})` | Builds a language domain and invokes picker with radios. |
| `open({ href, params, title, breadcrumb, icon, context })` | Opens the supplied Page with params, navigation metadata, and context. |

The first four helpers pass fields as Page creation context. `language()` uses `i18n.config.langs`, excludes IDs in `current`, and defaults to
required selection. It returns a selection without switching language; the current implementation does not forward `value` to picker.

`xshell.ui.dialog.*` selects standard **Page resources**. `xshell.ui.layout.dialog` selects the **presentation layout**.
Changing the layout does not change the helper Page.

## Results

A Page calls `page.close(result)`, which delegates to its hosting `x-page`.
Navigation listens for the `x-page` close event, removes the host, and resolves its promise with the result.
The checked-in x-demo result dialogs use this pattern:

```js
controller({ page }) {
    return {
        accept() {
            page.close({ accepted: true });
        }
    };
}
```

A caller awaits an arbitrary Page exposed by the composed application:

```js
const result = await dialog.open({
    href: "/_assets/x-demo/pages/04-dialogs/custom-dialog.js",
    title: "Results"
});
```

Current `x-page` result handling uses `page.result || null`: `false`, `0`, and `""` become `null`.
The bundled confirm Page returns `"yes"`, `"no"`, `"cancel"`, or `"ok"` according to the chosen button/variant.
Message confirmation returns `"ok"`. Prompt/picker submit their current value, and cancellation returns `null`.
Closing without a result yields `null`. Use an object for custom dialogs that must preserve a falsy payload.

Dialog removal disconnects the host and unmounts its Page but does not call the explicit `removePage()` final-unload path.
Account for this cleanup limitation when registering instance-lifetime resources.

See [Pages](../architecture/pages.md), [Navigation](../architecture/navigation.md), and [Lifecycle](../components/lifecycle.md).
