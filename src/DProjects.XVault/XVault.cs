using System;

namespace DProjects.XVault {
    public class XVault {

        // enum
        public enum Format {
            Json,
            Yaml,
            Xml,
            Env,
            Markdown,
        }

        //vars
        private readonly string mPath;
        private readonly string mText;
        private readonly Format mFormat;

        //ctor
        public XVault(string path) {
            mPath = path;
            mText = System.IO.File.ReadAllText(path);
            if (mPath.EndsWith(".json", StringComparison.OrdinalIgnoreCase) || mPath.EndsWith(".jsonc", StringComparison.OrdinalIgnoreCase)) {
                mFormat = Format.Json;
            } else if (mPath.EndsWith(".yaml", StringComparison.OrdinalIgnoreCase) || mPath.EndsWith(".yml", StringComparison.OrdinalIgnoreCase)) {
                mFormat = Format.Yaml;
            } else if (mPath.EndsWith(".xml", StringComparison.OrdinalIgnoreCase)) {
                mFormat = Format.Xml;
            } else if (mPath.EndsWith(".env", StringComparison.OrdinalIgnoreCase)) {
                mFormat = Format.Env;
            } else if (mPath.EndsWith(".md", StringComparison.OrdinalIgnoreCase) || mPath.EndsWith(".markdown", StringComparison.OrdinalIgnoreCase)) {
                mFormat = Format.Markdown;
            } else {
                throw new Exception("Unable to determine vault format from file extension. Supported extensions are .json, .yaml/.yml, .xml, .env, .md/.markdown");
            }
        }
        public XVault(string text, Format format, string? path = null) {
            mText = text;
            mFormat = format;
            mPath = path ?? "";
        }


        // methods
        public string Decrypt(string? password = null) {
            if (mFormat == Format.Json) {
                // json
                return new Handlers.JsonHandler().Decrypt(mText, password, mPath);
            } else if (mFormat == Format.Xml) {
                // xml
                return new Handlers.XmlHandler().Decrypt(mText, password, mPath);
            } else if (mFormat == Format.Yaml) {
                // yaml
                return new Handlers.YamlHandler().Decrypt(mText, password, mPath);
            } else if (mFormat == Format.Env) {
                // env  
                return new Handlers.EnvHandler().Decrypt(mText, password, mPath);
            } else if (mFormat == Format.Markdown) {
                // markdown
                return new Handlers.MarkdownHandler().Decrypt(mText, password, mPath);
            }
            throw new NotImplementedException();
        }

    }
}