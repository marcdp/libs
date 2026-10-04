# Temporary Files

The `temp` service uploads files to ASP.NET Temp middleware and returns logical temporary identifiers.

## Browser API

```js
const results = await temp.upload(file, progress => {
    console.log(progress.loaded, progress.total, progress.percent);
});
const resource = results[0];
if (!resource.startsWith("error: ")) {
    const downloadUrl = temp.getAbsoluteUrl(resource);
}
```

`upload(files, onProgress)` accepts a File or an iterable file collection such as an array/FileList. It always resolves to an array in input order,
including single
uploads. Each file uses its own XMLHttpRequest POST with a FormData field `file`; requests run concurrently through `Promise.all`.
Success entries contain unchanged response text. Each rejected upload becomes `"error: " + message` rather than rejecting the whole batch.

Progress receives `{ loaded, total, percent }` only for length-computable events. It has no file identity and reports each concurrent request
independently. There is no cancellation argument.

`getAbsoluteUrl(resource)` replaces leading `temp:/` with `xshell.temp.url`, preserving path/query. It performs string conversion rather than
validation or Resolver/Loader processing; call it only on a successful identifier. The host supplies this URL from `TempUrl`.

## HTTP contract

POST is accepted only at the configured prefix, with form content and exactly one file. No particular field name is required by the server.
Success creates a GUID directory and returns **201**, `text/plain`, without a JSON wrapper or Location header:

```text
temp:/<guid-N>/<escaped-file-name>?size=<bytes>&type=<extension-mime-type>&hash=<lowercase-sha256>&expiration=<unix-seconds>
```

`size` is stored length, `type` is inferred from the filename extension, `hash` covers stored bytes, and `expiration` is a UTC Unix timestamp.

GET uses `TempUrl/<guid>/<escaped-file-name>`; metadata query parameters are not checked.
The middleware validates the GUID, normalizes filenames to their basename, rejects empty/`.`/`..` names, and checks containment inside the root.
It streams the stored file with content length and extension-based content type, falling back to `application/octet-stream`.

Non-form uploads return **415**; invalid file count/name returns **400**; invalid/missing GET paths/files return **404**.
Unsupported methods, including HEAD/DELETE, and POST to child paths return **405**. There is no delete API.

## Storage and expiration

| Host setting | Default |
| --- | --- |
| `TempPath` | System temporary directory / `DProjects.XShell` / `temp` |
| `TempUrl` | `/temp` |
| `TempExpirationTime` | One hour |

The storage root is created on middleware construction. Cleanup first runs after five minutes and then every five minutes.
It deletes GUID directories whose creation time plus retention has passed; errors are swallowed.
Expiration is eventual cleanup, not a hard GET deadline: GET does not check expiration. Response expiration and directory creation-based
cleanup timestamps can differ slightly.

The middleware provides no authentication, ownership authorization, scanning, or application-specific quotas.
ASP.NET limits and access policy belong to the surrounding host pipeline. MIME and hash metadata are not content trust checks.

See [Hosting](../architecture/40-hosting.md) and [Components](../components/index.md).
