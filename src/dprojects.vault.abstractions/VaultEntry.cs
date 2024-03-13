using DProjects.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net.Http;

namespace DProjects.Vault {

    public class VaultEntry(VaultEntryType type,
                            string path,
                            string description,
                            string version,
                            DateTime createdAt,
                            ReadOnlyDictionary<string, string> headers) {

        //props
        public VaultEntryType Type { get; private set; } = type;
        public string Path { get; private set; } = path;
        public string Description { get; private set; } = description;
        public string Version { get; private set; } = version;
        public DateTime CreatedAt { get; private set; } = createdAt;
        public ReadOnlyDictionary<string, string> Headers { get; private set; } = headers;

    }

}
