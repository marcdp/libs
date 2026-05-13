using System;
using DProjects.XVault.Handlers;
using Microsoft.Extensions.Configuration;

namespace DProjects.XVault {
    
    public class XVault {
         
        //vars
        private readonly Handler mHandler;

        //ctor
        public XVault(string path, string? password = null) {
            var text = System.IO.File.ReadAllText(path);
            if (path.EndsWith(".json", StringComparison.OrdinalIgnoreCase) || path.EndsWith(".jsonc", StringComparison.OrdinalIgnoreCase)) {
                mHandler = new JsonHandler(text, path, password);
            } else if (path.EndsWith(".yaml", StringComparison.OrdinalIgnoreCase) || path.EndsWith(".yml", StringComparison.OrdinalIgnoreCase)) {
                mHandler = new YamlHandler(text, path, password);
            } else if (path.EndsWith(".xml", StringComparison.OrdinalIgnoreCase)) {
                mHandler = new XmlHandler(text, path, password);
            } else if (path.EndsWith(".env", StringComparison.OrdinalIgnoreCase)) {
                mHandler = new EnvHandler(text, path, password);
            } else if (path.EndsWith(".md", StringComparison.OrdinalIgnoreCase) || path.EndsWith(".markdown", StringComparison.OrdinalIgnoreCase)) {
                mHandler = new MarkdownHandler(text, path, password);
            } else {
                throw new Exception("Unable to determine vault format from file extension. Supported extensions are .json, .yaml/.yml, .xml, .env, .md/.markdown");
            }
        } 


        // methods
        public string Decrypt() {
            return mHandler.Decrypt();
        }
        public void Register(ConfigurationManager configurationManager) {
            mHandler.Register(configurationManager);
        }

    }
}