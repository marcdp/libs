# Temporary Files

`TempMiddleware` and the `xshell.temp` service provide a small transport bridge between browser file selection and application APIs. They create a
temporary resource; they do not know about application entities or permanent storage.

## Status

Implemented transport behavior. Temporary-file cleanup and application persistence are outside the current middleware contract.

## Flow

```text
browser File
    -> xshell.temp.upload(...)
    -> POST /temp
    -> TempMiddleware
    -> server-local temporary file
    -> /temp/<guid>/<filename>
    -> GET or application JSON reference
    -> application layer decides final persistence
```

The returned temp URL is a normal browser resource reference. For example, an application can later send it in its own request:

```js
const uploaded = await xshell.temp.upload(file);

await fetch("/api/documents", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({
        name: "Document",
        attachment: uploaded.url
    })
});
```

`/api/documents` is application-specific. The application layer validates the request, recognizes a temporary resource if appropriate, and may
copy, move, or otherwise promote it to permanent storage before persisting a final reference. `TempMiddleware` does not implement a generic
promotion API and has no knowledge of documents, customers, invoices, avatars, database entities, or final storage locations.

## Browser service

XShell creates the service as `xshell.temp` and also registers it as the `temp` runtime service. It uses native `File`, `FormData`, and `fetch`;
there is no client-side upload framework.

```js
const result = await xshell.temp.upload(file);
console.log(result.url);
```

`upload()` accepts either one `File` or an iterable of `File` objects, such as an array or `FileList`. A single `File` returns one upload result;
an iterable returns an array of results. Iterable uploads occur sequentially, with one POST for each file.

```js
const results = await xshell.temp.upload(files);

for (const result of results) {
    console.log(result.url);
}
```

For each request, the service appends the file to `FormData` with the field name `file`, posts it to `/temp`, throws an `Error` for a non-successful
HTTP response, and returns the parsed JSON response. Supplying neither a `File` nor an iterable causes JavaScript iteration to fail; supplying a
non-`File` item throws `TypeError` before it is sent.

## Host middleware

`Extensions.UseXShell()` registers `TempMiddleware` with `Configuration.TempPath` and `Configuration.TempUrl`. By default these are a server local
temporary directory under the system temporary path and `/temp`, respectively. The middleware constructor receives these as distinct values:

- **Physical path** is the server-local directory where temporary files are stored.
- **Request path** is the public URL prefix, normalized to begin with one slash and not allowed to be the root path.

For example, a physical path such as `C:\app\data\temp` can hold `C:\app\data\temp\c0013353b5254a60a964487c9ee775dc\hello.txt`, while its
public temp URL is `/temp/c0013353b5254a60a964487c9ee775dc/hello.txt`. The physical path is not exposed in the URL.

### Upload

`POST /temp` accepts a form-content request containing exactly one uploaded file; the server does not require a particular form field name. It
normalizes the supplied filename to its last path segment, creates a server-generated 32-character GUID in `N` format, and stores the file below
that GUID directory.

On success, the response is `201 Created`, includes a `Location` header with the temp URL, and contains:

```json
{
    "url": "/temp/<guid>/<filename>"
}
```

The filename is URL-escaped in this response. A request without form content returns `415 Unsupported Media Type`; a request with any number of
files other than one, or an empty, `.` or `..` filename after normalization, returns `400 Bad Request`.

### Download

`GET /temp/<guid>/<filename>` reads the temporary file directly from its configured physical directory. The URL must contain exactly a GUID and a
filename segment; invalid paths, invalid filenames, traversal attempts, and missing files return `404 Not Found`. The middleware detects content
type from the filename extension and falls back to `application/octet-stream` when it has no mapping.

This is temporary upload/download transport, not a general-purpose file server. Methods other than `POST /temp` and `GET` under the configured
request path return `405 Method Not Allowed`. The middleware does not implement DELETE.

## Limits and boundaries

The current middleware creates its base directory when constructed. It has no built-in ownership checks, upload tokens, antivirus scanning,
MIME validation, file-size limit, expiration, or garbage collection. Authentication and authorization are not implemented by this middleware;
any access controls must be supplied by the surrounding ASP.NET Core pipeline.

Temporary cleanup is therefore pending or owned by hosting/application infrastructure. Applications must not assume a temporary file persists for
a particular lifetime.

## Current configuration limitation

Although the host can configure `TempUrl`, the shipped `xshell.temp` instance is constructed without options and therefore posts to `/temp`.
Applications using a different `TempUrl` need a matching client-side arrangement; XShell does not currently pass that host setting into `temp.js`.

## Related documentation

- [Subsystems](index.md)
- [Configuration](../architecture/configuration.md)
