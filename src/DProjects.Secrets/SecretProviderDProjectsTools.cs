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
using System.Collections.Specialized;

namespace DProjects.Secrets {

    public class SecretProviderDProjectsTools(string databaseName) : ISecretProvider {

        //consts
        private const string KEYRING_PASSWORD_NAME = "dprojectstools";
        private const string FILE_EXTENSION = ".json.aes";

        //methods
        public Secret? Get(string name) {
            var dictionary = LoadDictionary();
            if (!dictionary.ContainsKey(name)) return null;
            var value = dictionary[name];   
            var secret = new Secret(name, "", value);
            return secret;
        }
        public Task<Secret?> GetAsync(string name, CancellationToken cancellationToken) {
            return Task.FromResult<Secret?>(Get(name));
        }


        //utils
        private StringDictionary LoadDictionary() {
            var dictionary = new StringDictionary();
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
            var jsonObject = JsonNode.Parse(json)!.AsObject();
            foreach(var key in jsonObject) {
                if (key.Value is JsonObject) {
                    foreach (var subkey in key.Value.AsObject()) {
                        dictionary[key.Key + "." + subkey.Key] = subkey.Value!.AsValue().ToString();
                    }
                } else {
                    dictionary[key.Key] = key.Value!.AsValue().ToString();
                }
            }
            //return
            return dictionary;
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