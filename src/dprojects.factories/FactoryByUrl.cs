using DProjects.Utils;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DProjects.Factories {


    public class FactoryByUrl<TType> : IFactoryByUrl<TType> where TType : class {


        //variables
        private IServiceProvider mServiceProvider;
        private FactoryByUrlConfiguration<TType> mConfiguration; 

        //constructor
        public FactoryByUrl(FactoryByUrlConfiguration<TType> configuration, IServiceProvider serviceProvider) {
            mConfiguration = configuration;
            mServiceProvider = serviceProvider; 
        }

        //props
        public IReadOnlyList<FactoryByUrlProtocol<TType>> Protocols => mConfiguration.Protocols;
        public IReadOnlyList<FactoryByUrlAlias> Aliases => mConfiguration.Aliases;

        //methods
        public TType Create(string url) {
            //validations
            if (url.StartsWith("/")) url = "dir://" + url;
            //aliases
            url = mConfiguration.Aliases.Where(x => x.Name.Equals(url)).Select(x => x.Value).DefaultIfEmpty(url).FirstOrDefault();
            //add dots if required
            if (url.Length > 0 && url.IndexOf(":") == -1) url += ":";
            //fill userinfo password
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
            //create
            var protocol = mConfiguration.Protocols.Where(x => x.Name.Equals(scheme)).FirstOrDefault();
            if (protocol == null) throw new  ArgumentException("Unable to create instance of type, protocol not found: schema: " + scheme + ", protocol: " + typeof(TType).FullName);
            var subFactory = mServiceProvider.GetRequiredKeyedService<IFactoryByUrl<TType>>(protocol.Name);
            return subFactory.Create(url);
        }


    }


}