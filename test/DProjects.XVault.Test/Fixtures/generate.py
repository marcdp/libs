"""Regenerate sanitized interoperability fixtures with canonical marcdp/xvault."""

import pathlib
import sys


PASSWORD = "correct horse battery staple"
FIXTURES = pathlib.Path(__file__).parent
XVAULT_SOURCE = FIXTURES.parents[3] / "xvault" / "src"

sys.path.insert(0, str(XVAULT_SOURCE))

from xvault import xvault as canonical  # noqa: E402


class DeterministicRandom:
    def __init__(self):
        self.value = 0

    def __call__(self, length):
        result = bytes((self.value + index) % 256 for index in range(length))
        self.value += length
        return result


def generate(name, handler, plaintext):
    vault = canonical.XVault.__new__(canonical.XVault)
    vault._path = pathlib.Path(name)
    vault._password = PASSWORD
    vault._key = None
    vault._no_cache_key = True
    vault._cache = {}
    vault._meta = canonical.XVaultMeta(schema_version=1, crypto_version=1, salt="00112233445566778899aabbccddeeff", check=None)
    vault._handler = handler
    encrypted = vault._encrypt(plaintext)
    vault._meta.check = vault._encrypt_value(canonical.META_CHECK_VALUE)
    result = handler.stringify(vault._meta, encrypted)
    expected = vault._decrypt(encrypted, return_unprefixed_values=True)
    (FIXTURES / name).write_text(result, encoding="utf-8", newline="\n")
    (FIXTURES / f"{name}.expected").write_text(expected, encoding="utf-8", newline="\n")


canonical.os.urandom = DeterministicRandom()

generate("compatibility.json", canonical.HandlerJson(), '{\n  "username": "public-user",\n  "password": "enc:json-secret",\n  "nested": {\n    "api_key": "enc:json-api-key"\n  }\n}\n')
generate("compatibility.jsonc", canonical.HandlerJsonc(), '{\n  // retained application comment\n  "username": "public-user",\n  "password": "enc:jsonc-secret",\n  "nested": {\n    "api_key": "enc:jsonc-api-key"\n  }\n}\n')
generate("compatibility.yaml", canonical.HandlerYaml(), "username: public-user\npassword: enc:yaml-secret\nnested:\n  api_key: enc:yaml-api-key\n")
generate("compatibility.env", canonical.HandlerEnv(), "# retained application comment\nUSERNAME=public-user\nPASSWORD=enc:env-secret\nAPI_KEY=${enc:env-api-key}\n\n# retained footer\n")
generate("compatibility.md", canonical.HandlerMd(), "# Deployment notes\n\nPublic user: public-user\n\nPassword: ${enc:markdown-secret}\n\nAPI key: ${enc:markdown-api-key}\n")
