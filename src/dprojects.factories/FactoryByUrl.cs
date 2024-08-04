using DProjects.Utils;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DProjects.Factories {


    
    public class FactoryByUrl<TType> : IDisposable, IFactoryByUrl<TType> where TType : class {


        //variables
        private IServiceProvider mServiceProvider;
        private FactoryByUrlConfiguration<TType> mConfiguration; 
        private List<IDisposable> mDisposables;

        //constructor
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

        //methods
        public TType Create(string url) {
            //validations
            //if (url.StartsWith("/")) url = "fs://" + url;
            //aliases
            url = mConfiguration.Aliases.Where(x => x.Name.Equals(url)).Select(x => x.Value).DefaultIfEmpty(url).FirstOrDefault();
            //if (url.StartsWith("/")) url = "fs://" + url;
            //add dots if required
            if (url.Length > 0 && url.IndexOf(":") == -1) url += ":";
            //fill userInfo password
            var scheme = (url.Length > 0 ? url.Substring(0, url.IndexOf(':')) : "");
            if (url.Length > 0) {
                var aUrl = new Uri(url);
                if ((!string.IsNullOrEmpty(aUrl.UserInfo) && aUrl.UserInfo.IndexOf(":") == -1) || StringUtils.SeemsConnectionString(url)) {
                    var passwordFiller = mServiceProvider.GetService<IFactoryPasswordFiller>();
                    if (passwordFiller != null) {
                        passwordFiller.FillPassword(ref url);
                    }
                }
            }
            //try return default instance
            if (url.Length == 0) {
                var defaultInstance = mServiceProvider.GetService<TType>();
                if (defaultInstance != null) return defaultInstance;
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
            //fill userInfo password
            var scheme = (url.Length > 0 ? url.Substring(0, url.IndexOf(':')) : "");
            if (url.Length > 0) {
                var aUrl = new Uri(url);
                if ((!string.IsNullOrEmpty(aUrl.UserInfo) && aUrl.UserInfo.IndexOf(":") == -1) || StringUtils.SeemsConnectionString(url)) {
                    var passwordFiller = mServiceProvider.GetService<IFactoryPasswordFiller>();
                    if (passwordFiller != null) {
                        passwordFiller.FillPassword(ref url);
                    }
                }
            }
            //try return default instance
            if (url.Length == 0) {
                var defaultInstance = mServiceProvider.GetService<TType>();
                if (defaultInstance != null) return defaultInstance;
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