using DProjects.Secrets;
using DProjects.Utils;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace DProjects.Factories {


    
    public class FactoryByUrl<TType> : IDisposable, IFactoryByUrl<TType> where TType : class {

        // const
        //private static readonly Regex mSecretRegex = new(@"\$\{secret:(?<name>[A-Za-z0-9_\-]+)\}", RegexOptions.Compiled);
        private static readonly Regex mSecretRegex = new(@"\$\{secret:(?<name>[^}]+)\}", RegexOptions.Compiled);

        // variables
        private IServiceProvider mServiceProvider;
        private FactoryByUrlConfiguration<TType> mConfiguration; 
        private List<IDisposable> mDisposables;

        // ctor
        public FactoryByUrl(FactoryByUrlConfiguration<TType> configuration, IServiceProvider serviceProvider) {
            mConfiguration = configuration;
            mServiceProvider = serviceProvider;
            mDisposables = new();
        }
        public void Dispose() {
            foreach (var disposable in mDisposables) { 
                disposable.Dispose(); 
            }
        }


        //props
        public FactoryByUrlConfiguration<TType> Configuration => mConfiguration;  
        public IReadOnlyList<FactoryByUrlProtocol<TType>> Protocols => mConfiguration.Protocols;
        public IReadOnlyList<FactoryByUrlAlias> Aliases => mConfiguration.Aliases;
        public IReadOnlyList<FactoryByUrlHandler<TType>> Handlers => mConfiguration.Handlers;

        //methods
        public TType Create(string url) {
            //aliases
            if (url.IndexOf(":") == -1 && url.IndexOf("?") != -1) {
                // aliases with query (ex: my_alias?var1=a&var2=b)
                var name = url.Split('?')[0];
                var query = url.Substring(name.Length + 1);
                var aux = mConfiguration.Aliases.Where(x => x.Name.Equals(name)).Select(x => x.Value).FirstOrDefault();
                if (aux != null) {
                    var queryValues = UrlUtils.ParseQueryString(query);
                    foreach(var key in queryValues.AllKeys) {
                        aux = UrlUtils.ReplaceQueryValue(aux, key, queryValues[key]);
                    }
                    url = aux;
                }
            } else {
                // aliases without query (ex: my_alias)
                url = mConfiguration.Aliases.Where(x => x.Name.Equals(url)).Select(x => x.Value).DefaultIfEmpty(url).FirstOrDefault();
            }
            //windows path
            if (url.Length > 2 && url[1] == ':' && url[2] == '\\') {
                url = "file:///" + url.Replace("\\", "/").Replace(":", "");
            }
            //add dots if required
            if (url.Length > 0 && url.IndexOf(":") == -1) url += ":";
            //fill secrets
            var scheme = (url.Length > 0 ? url.Substring(0, url.IndexOf(':')) : "");
            if (url.Length > 0 && url.IndexOf("${secret:")!=-1) {
                var secretProvider = mServiceProvider.GetRequiredService<ISecretProvider>();
                url = mSecretRegex.Replace(url, (match) => {
                    var name = match.Groups["name"].Value;
                    var secret = secretProvider.Get(name) ?? throw new KeyNotFoundException($"Secret not found: {name}"); ;
                    var value = secret.GetValue();
                    return value;
                });
            }
            //try return default instance
            if (url.Length == 0) {
                var defaultInstance = mServiceProvider.GetService<TType>();
                if (defaultInstance != null) return defaultInstance;
            }
            //try get handler
            var handler = mConfiguration.Handlers.Where(x => x.Name.Equals(scheme)).Select(x => x.Handler).FirstOrDefault();
            if (handler != null) {
                //create from handler
                var instanceFromHandler = handler(mServiceProvider, url);
                //register IDisposable instance
                if (instanceFromHandler is IDisposable instanceFromHandlerDisposable) mDisposables.Add(instanceFromHandlerDisposable);
                //return
                return instanceFromHandler;
            }
            //create instance
            var protocol = mConfiguration.Protocols.Where(x => x.Name.Equals(scheme)).FirstOrDefault();
            if (protocol == null) {
                throw new ArgumentException("Unable to create instance of type, protocol not found: schema: " + scheme + ", protocol: " + typeof(TType).FullName);
            }
            var factory = mServiceProvider.GetRequiredKeyedService<IFactoryByUrl<TType>>(protocol.Name);
            var instance = factory.Create(url);
            //register IDisposable instance
            if (instance is IDisposable instanceDisposable) mDisposables.Add(instanceDisposable);
            //return instance
            return instance;
        }
    }

    public class FactoryByUrl<TType, TArgument> : IDisposable, IFactoryByUrl<TType, TArgument> where TType : class where TArgument: class {

        //const
        private static readonly Regex mSecretRegex = new(@"\$\{secret:(?<name>[^}]+)\}", RegexOptions.Compiled);

        //variables
        private IServiceProvider mServiceProvider;
        private FactoryByUrlConfiguration<TType, TArgument> mConfiguration;
        private List<IDisposable> mDisposables;

        //constructor
        public FactoryByUrl(FactoryByUrlConfiguration<TType, TArgument> configuration, IServiceProvider serviceProvider) {
            mConfiguration = configuration;
            mServiceProvider = serviceProvider;
            mDisposables = new();
        }
        public void Dispose() {
            foreach (var disposable in mDisposables) {
                disposable.Dispose();
            }
        }


        //props
        public FactoryByUrlConfiguration<TType, TArgument> Configuration => mConfiguration;
        public IReadOnlyList<FactoryByUrlProtocol<TType, TArgument>> Protocols => mConfiguration.Protocols;
        public IReadOnlyList<FactoryByUrlAlias> Aliases => mConfiguration.Aliases;

        //methods
        public TType Create(string url, TArgument argument) {
            //validations
            //if (url.StartsWith("/")) url = "fs://" + url;
            //aliases
            url = mConfiguration.Aliases.Where(x => x.Name.Equals(url)).Select(x => x.Value).DefaultIfEmpty(url).FirstOrDefault();
            //if (url.StartsWith("/")) url = "fs://" + url;
            //add dots if required
            if (url.Length > 0 && url.IndexOf(":") == -1) url += ":";
            //add dots if required
            if (url.Length > 0 && url.IndexOf(":") == -1) url += ":";
            //fill secrets
            var scheme = (url.Length > 0 ? url.Substring(0, url.IndexOf(':')) : "");
            if (url.Length > 0 && url.IndexOf("${secret:") != -1) {
                var secretProvider = mServiceProvider.GetRequiredService<ISecretProvider>();
                url = mSecretRegex.Replace(url, (match) => {
                    var name = match.Groups["name"].Value;
                    var secret = secretProvider.Get(name) ?? throw new KeyNotFoundException($"Secret not found: {name}"); ;
                    var value = secret.GetValue();
                    return value;
                });
            }
            //try return default instance
            if (url.Length == 0) {
                var defaultInstance = mServiceProvider.GetService<TType>();
                if (defaultInstance != null) return defaultInstance;
            }
            //try get handler
            var handler = mConfiguration.Handlers.Where(x => x.Name.Equals(scheme)).Select(x => x.Handler).FirstOrDefault();
            if (handler != null) {  
                //create from handler
                var instanceFromHandler = handler(mServiceProvider, argument);
                //register IDisposable instance
                if (instanceFromHandler is IDisposable instanceFromHandlerDisposable) mDisposables.Add(instanceFromHandlerDisposable);
                //return
                return instanceFromHandler;
            }
            //create instance
            var protocol = mConfiguration.Protocols.Where(x => x.Name.Equals(scheme)).FirstOrDefault();
            if (protocol == null) {
                throw new ArgumentException("Unable to create instance of type, protocol not found: schema: " + scheme + ", protocol: " + typeof(TType).FullName);
            }
            var subFactory = mServiceProvider.GetRequiredKeyedService<IFactoryByUrl<TType, TArgument>>(protocol.Name);
            var instance = subFactory.Create(url, argument);
            //register IDisposable instance
            if (instance is IDisposable instanceDisposable) mDisposables.Add(instanceDisposable);
            //return instance
            return instance;
        }
    }





}