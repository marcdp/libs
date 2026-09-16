
// export defaultfunction
export function base64UrlEncode(input) {
    if (typeof input !== "string") {
        throw new Error("base64UrlEncode: input must be a string");
    }

    // Convert string to UTF-8 bytes
    const utf8Bytes = new TextEncoder().encode(input);

    // Convert bytes to binary string
    let binary = "";
    for (let i = 0; i < utf8Bytes.length; i++) {
        binary += String.fromCharCode(utf8Bytes[i]);
    }

    // Standard base64
    let base64 = btoa(binary);

    // Convert to URL-safe
    return base64
        .replace(/\+/g, "-")
        .replace(/\//g, "_")
        .replace(/=+$/, "");
}

export function base64UrlDecode(input) {
    if (typeof input !== "string") {
        throw new Error("base64UrlDecode: input must be a string");
    }

    // Convert URL-safe base64 to standard base64
    let base64 = input
        .replace(/-/g, "+")
        .replace(/_/g, "/");

    // Restore padding if missing
    while (base64.length % 4 !== 0) {
        base64 += "=";
    }

    // Decode base64 to binary string
    const binary = atob(base64);

    // Convert binary string to Uint8Array
    const bytes = new Uint8Array(binary.length);
    for (let i = 0; i < binary.length; i++) {
        bytes[i] = binary.charCodeAt(i);
    }

    // Decode UTF-8 bytes back to string
    return new TextDecoder().decode(bytes);
}
