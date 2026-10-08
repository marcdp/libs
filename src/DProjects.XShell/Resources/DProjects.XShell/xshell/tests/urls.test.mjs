import assert from "node:assert/strict";
import test from "node:test";

import {combineUrls} from "../utils/urls.js";

test("combineUrls preserves URI scheme-qualified targets", () => {
    for (const target of ["https://example.test/foo", "mailto:test@example.com", "data:text/plain,test", "custom:resource"]) {
        assert.equal(combineUrls("/pages/current.js", target), target);
    }
});

test("combineUrls resolves colons in query and fragment references relatively", () => {
    assert.equal(combineUrls("/pages/current.js", "details?time=10:30"), "/pages/details?time=10:30");
    assert.equal(combineUrls("/pages/current.js", "details#section:advanced"), "/pages/details#section:advanced");
});
