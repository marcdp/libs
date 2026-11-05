"""Regenerate sanitized interoperability fixtures with canonical marcdp/xvault."""

import pathlib
import shutil
import sys
from unittest.mock import patch


PASSWORD = "correct horse battery staple"
REKEY_OLD_PASSWORD = "fixture-v2-old-password"
REKEY_NEW_PASSWORD = "fixture-v2-new-password"
FIXTURES = pathlib.Path(__file__).parent
XVAULT_ROOT = FIXTURES.parents[3] / "xvault"
XVAULT_SOURCE = XVAULT_ROOT / "src"

sys.path.insert(0, str(XVAULT_SOURCE))

from xvault.crypto import CryptoV1, CryptoV2  # noqa: E402
from xvault.handlers import HandlerEnv, HandlerJson, HandlerJsonc, HandlerMd, HandlerYaml  # noqa: E402
from xvault.meta import ENC_PREFIX, META_CHECK_VALUE, XVaultMeta  # noqa: E402


class DeterministicRandom:

    # vars
    def __init__(self):
        self.value = 0

    # methods
    def __call__(self, length):
        result = bytes((self.value + index) % 256 for index in range(length))
        self.value += length
        return result


RANDOM_BYTES = DeterministicRandom()


def replace_markers(handler, text, transform, preserve_wrapping=True):
    def replace(value):
        wrapped = value.startswith("${") and value.endswith("}")
        token = value[2:-1] if wrapped else value
        transformed = transform(token[len(ENC_PREFIX):])
        return "${" + transformed + "}" if wrapped and preserve_wrapping else transformed

    return handler.replace_enc_tokens(text, replace)


def generate_v1(name, handler, plaintext):
    meta = XVaultMeta(schema_version=1, crypto_version=1, salt="00112233445566778899aabbccddeeff", check=None)
    crypto = CryptoV1(meta, password=PASSWORD)
    with patch("xvault.crypto.v1.os.urandom", RANDOM_BYTES):
        encrypted = replace_markers(handler, plaintext, lambda value: ENC_PREFIX + crypto.encrypt(value))
        meta.check = crypto.encrypt(META_CHECK_VALUE)
    write_fixture(name, handler.stringify(meta, encrypted), replace_markers(handler, plaintext, lambda value: value, preserve_wrapping=False))


def generate_v2(name, handler, plaintext):
    meta = XVaultMeta(schema_version=1, crypto_version=2, salt="00112233445566778899aabbccddeeff", metadata_present=False)
    crypto = CryptoV2(meta, password=PASSWORD, random_bytes=RANDOM_BYTES)
    encrypted = replace_markers(handler, plaintext, lambda value: ENC_PREFIX + crypto.encrypt(value))
    write_fixture(name, handler.stringify(meta, encrypted), replace_markers(handler, plaintext, lambda value: value, preserve_wrapping=False))


def generate_rekey_v2():
    handler = HandlerEnv()
    plaintext = "# retained rekey fixture\nPLAIN=visible\nSECRET=enc:rekey-secret\n"
    meta = XVaultMeta(schema_version=1, crypto_version=2, salt="102132435465768798a9bacbdcedfe0f", metadata_present=False)
    crypto = CryptoV2(meta, password=REKEY_OLD_PASSWORD, random_bytes=RANDOM_BYTES)
    encrypted = replace_markers(handler, plaintext, lambda value: ENC_PREFIX + crypto.encrypt(value))
    expected = replace_markers(handler, plaintext, lambda value: value, preserve_wrapping=False)
    write_fixture("rekey-before-v2.env", handler.stringify(meta, encrypted), expected)
    rotation = crypto.prepare_password_rotation(REKEY_NEW_PASSWORD)
    write_fixture("rekey-after-v2.env", handler.stringify(rotation.meta, encrypted), expected)


def write_fixture(name, encrypted, expected):
    (FIXTURES / name).write_text(encrypted, encoding="utf-8", newline="\n")
    (FIXTURES / f"{name}.expected").write_text(expected, encoding="utf-8", newline="\n")


# preserve the existing deterministic v1 fixtures
generate_v1(
    "compatibility.json", HandlerJson(),
    '{\n  "username": "public-user",\n  "password": "enc:json-secret",\n  "nested": {\n    "api_key": "enc:json-api-key"\n  }\n}\n',
)
generate_v1(
    "compatibility.jsonc", HandlerJsonc(),
    '{\n  // retained application comment\n  "username": "public-user",\n  "password": "enc:jsonc-secret",\n'
    '  "nested": {\n    "api_key": "enc:jsonc-api-key"\n  }\n}\n',
)
generate_v1(
    "compatibility.yaml", HandlerYaml(),
    "username: public-user\npassword: enc:yaml-secret\nnested:\n  api_key: enc:yaml-api-key\n",
)
generate_v1(
    "compatibility.env", HandlerEnv(),
    "# retained application comment\nUSERNAME=public-user\nPASSWORD=enc:env-secret\nAPI_KEY=${enc:env-api-key}\n\n# retained footer\n",
)
generate_v1(
    "compatibility.md", HandlerMd(),
    "# Deployment notes\n\nPublic user: public-user\n\nPassword: ${enc:markdown-secret}\n\nAPI key: ${enc:markdown-api-key}\n",
)

# generate v2 fixtures through canonical CryptoV2 and each canonical format handler
generate_v2(
    "compatibility-v2.json", HandlerJson(),
    '{\n  "username": "public-user",\n  "password": "enc:json-v2-secret",\n  "nested": {\n'
    '    "api_key": "${enc:json-v2-api-key}",\n    "unicode": "enc:Héllo 密码 🔐"\n  }\n}\n',
)
generate_v2(
    "compatibility-v2.jsonc", HandlerJsonc(),
    '{\n  // retained v2 application comment\n  "username": "public-user",\n  "password": "enc:jsonc-v2-secret",\n'
    '  "nested": {\n    "api_key": "${enc:jsonc-v2-api-key}"\n  }\n}\n',
)
generate_v2(
    "compatibility-v2.yaml", HandlerYaml(),
    "# retained v2 application comment\nusername: public-user\npassword: enc:yaml-v2-secret\n"
    "nested:\n  api_key: ${enc:yaml-v2-api-key}\n",
)
generate_v2(
    "compatibility-v2.env", HandlerEnv(),
    "# retained v2 application comment\nUSERNAME=public-user\nPASSWORD=enc:env-v2-secret\nAPI_KEY=${enc:env-v2-api-key}\n",
)
generate_v2(
    "compatibility-v2.md", HandlerMd(),
    "# V2 deployment notes\n\nPublic user: public-user\n\nPassword: enc:markdown-v2-secret\n\n"
    "API key: ${enc:markdown-v2-api-key}\n",
)

# copy the canonical fixed Argon2 vector verbatim and create a plaintext companion
shutil.copyfile(XVAULT_ROOT / "tests" / "fixtures" / "v2.env", FIXTURES / "canonical-v2.env")
(FIXTURES / "canonical-v2.env.expected").write_text("PLAIN=visible\nSECRET=fixture-v2-secret\n", encoding="utf-8", newline="\n")
generate_rekey_v2()
