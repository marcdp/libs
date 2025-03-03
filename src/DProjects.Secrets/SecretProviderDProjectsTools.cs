using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Threading;

using DProjects.Fs;
using DProjects.Fs.Extensions;
using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using DProjects.Crypto;
using System.Collections.Generic;

namespace DProjects.Secrets {

    public class SecretProviderDProjectsTools(string databaseName) : ISecretProvider {

        //consts
        private const string KEYRING_PASSWORD_NAME = "dprojectstools";
        private const string FILE_EXTENSION = ".json.aes";

        //methods
        public Task<Secret?> GetAsync(string name, CancellationToken cancellationToken) {
            var aux = LoadJson();
            if (!aux.TryGetPropertyValue(name, out var value) || value == null) return Task.FromResult<Secret?>(null); 
            var secret = new Secret(name, "", value.GetValue<string>());
            return Task.FromResult<Secret?>(secret);
        }
        public Task<string[]> GetNamesAsync(CancellationToken cancellationToken) {
            var aux = LoadJson();
            var keys = new List<string>();
            foreach (var key in aux) {
                keys.Add(key.Key);
            }
            return Task.FromResult(keys.ToArray());
        }


        //utils
        private JsonObject LoadJson() {
            //filename
            var filename = System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile), ".dprojectstools", "secrets", databaseName + FILE_EXTENSION);
            if (!System.IO.File.Exists(filename)) throw new Exception($"Unable to load secrets: file not found: {filename}");
            //load text
            var text = System.IO.File.ReadAllText(filename);
            var password = GetKeyRingPassword();
            if (password == null) throw new Exception($"Unable to load secrets: password not found in os keyring: {KEYRING_PASSWORD_NAME}");
            //decrypt
            var crypto = new DProjects.Crypto.CryptoSymmetricDecryptAES();
            var json = crypto.Decrypt(text, password);
            //return
            return JsonNode.Parse(json)!.AsObject()!;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct CREDENTIAL {
            public int Flags;
            public int Type;
            public IntPtr TargetName;
            public IntPtr Comment;
            public IntPtr LastWritten;
            public int CredentialBlobSize;
            public IntPtr CredentialBlob;
            public int Persist;
            public int AttributeCount;
            public IntPtr Attributes;
            public IntPtr TargetAlias;
            public IntPtr UserName;
        }

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool CredRead(string target, int type, int reservedFlag, out IntPtr credentialPtr);

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern void CredFree(IntPtr credentialPtr);

        private const int CRED_TYPE_GENERIC = 1;

        private string? GetKeyRingPassword() {
            string? password = null;
            if (CredRead(KEYRING_PASSWORD_NAME, CRED_TYPE_GENERIC, 0, out IntPtr credentialPtr)) {
                CREDENTIAL credential = Marshal.PtrToStructure<CREDENTIAL>(credentialPtr);
                //read credentials
                string username = Marshal.PtrToStringUni(credential.UserName);
                password = Marshal.PtrToStringUni(credential.CredentialBlob, credential.CredentialBlobSize / 2); // Convert bytes to string
                // Free allocated memory
                CredFree(credentialPtr);
            }
            return password;
        }
    }

}