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
required selection. It forwards `value` to the radios picker and returns the selection without switching language.

`xshell.ui.dialog.*` selects standard **Page resources**. `xshell.ui.layout.dialog` selects the **presentation layout**.
Changing the layout does not change the helper Page.

## Results

A Page calls `page.close(result)`, which delegates to its hosting `x-page`.
Navigation listens for the `x-page` close event, preserves the result, and awaits `removePage()` before resolving its promise. Dialog close is a final
Page destruction path: the Page unmounts and unloads, then the host is removed. A cleanup failure rejects the dialog promise.
Navigation stack updates close open dialogs through this same path.
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

`x-page` preserves `false`, `0`, and `""` results; an absent or null result is returned as `null`.
The bundled confirm Page returns `"yes"`, `"no"`, `"cancel"`, or `"ok"` according to the chosen button/variant.
Message confirmation returns `"ok"`. Prompt/picker submit their current value, and cancellation returns `null`.
Closing without a result yields `null`. The picker Page contract accepts `select` and `radios` input modes.

See [Pages](../architecture/70-pages.md), [Navigation](../architecture/120-navigation.md), and [Lifecycle](../components/60-lifecycle.md).
