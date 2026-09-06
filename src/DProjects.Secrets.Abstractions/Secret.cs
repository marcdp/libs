using System;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace DProjects.Secrets {


    public class Secret {

        
        //const
        public const string DEFAULT_VALUE_NAME = "default";


        //ctor
        public Secret() {
            Name = "";
            Description = "";
            CreatedAt = DateTime.Now;
            Values = new();
            Values[DEFAULT_VALUE_NAME] = "";
            Tags = [];
        } 
        public Secret(string name, string description, string value) {
            Name = name;
            Description = description;
            Values = new();
            Values[DEFAULT_VALUE_NAME] = value;
            CreatedAt = DateTime.Now;
            Tags = [];
        }
        public Secret(string name, string description, Dictionary<string, string> values, string[] tags) {
            Name = name;
            Description = description;
            Values = values;
            CreatedAt = DateTime.Now;
            Tags = tags;
        }
        public Secret(string name, string description, DateTime createdAt, DateTime? modifiedAt, DateTime? expiresAt, Dictionary<string, string> values, string[] tags) {
            Name = name;
            Description = description;
            CreatedAt = createdAt;
            ModifiedAt = modifiedAt;
            ExpiresAt = expiresAt;
            Values = values;
            Tags = tags;
        }


        //props
        public string Name { get; set; }
        public string Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ModifiedAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public Dictionary<string,string> Values { get; set; }
        public string[] Tags{ get; set; }



        //methods
        public string GetValue() {
            if (!Values.ContainsKey(DEFAULT_VALUE_NAME)) return "";
            return Values[DEFAULT_VALUE_NAME];
        }
        public string GetValue(string key) {
            return Values[key];
        }
        public override bool Equals(object obj) {
            return obj is Secret secret && Name == secret.Name;
        }
        public override int GetHashCode() {
            return Name.GetHashCode() ;
        }

    }

}