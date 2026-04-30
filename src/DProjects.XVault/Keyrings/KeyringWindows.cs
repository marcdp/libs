using System;
using System.Runtime.InteropServices;
using System.Text;

namespace DProjects.XVault.Keyrings {
    public sealed class KeyringWindows : Keyring {


        private const uint CredTypeGeneric = 1;

        public override bool TryReadBytes(string key, out byte[] value) {
            value = Array.Empty<byte>();

            if (!DProjects.Utils.EnvironmentUtils.IsWindows()) {
                return false;
            }

            if (!CredReadW(key, CredTypeGeneric, 0, out var credentialPtr)) {
                return false;
            }

            try {
                var credential = Marshal.PtrToStructure<CREDENTIAL>(credentialPtr);
                if (credential.CredentialBlob == IntPtr.Zero || credential.CredentialBlobSize == 0) {
                    return false;
                }

                var blobSize = checked((int)credential.CredentialBlobSize);
                value = new byte[blobSize];
                Marshal.Copy(credential.CredentialBlob, value, 0, blobSize);
                return true;
            } finally {
                CredFree(credentialPtr);
            }
        }

        public override bool TryReadText(string key, out string value) {
            value = string.Empty;
            if (!TryReadBytes(key, out var bytes) || bytes.Length == 0) {
                return false;
            }

            value = Encoding.Unicode.GetString(bytes).TrimEnd('\0');
            return true;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct CREDENTIAL {
            public uint Flags;
            public uint Type;
            public string TargetName;
            public string Comment;
            public System.Runtime.InteropServices.ComTypes.FILETIME LastWritten;
            public uint CredentialBlobSize;
            public IntPtr CredentialBlob;
            public uint Persist;
            public uint AttributeCount;
            public IntPtr Attributes;
            public string TargetAlias;
            public string UserName;
        }

        [DllImport("advapi32", CharSet = CharSet.Unicode, SetLastError = true, EntryPoint = "CredReadW")]
        private static extern bool CredReadW(string target, uint type, uint reservedFlag, out IntPtr credentialPtr);

        [DllImport("advapi32", SetLastError = true)]
        private static extern void CredFree([In] IntPtr cred);
    }
}