
namespace DProjects.Auth {

    public class AuthField(string name, string label, AuthFieldType type) { 

        public string Name { get;  } = name;
        public string Label { get; } = label;
        public AuthFieldType Type { get; } = type;
        public string Description { get; set; } = "";
        public string PlaceHolder { get; set; } = "";
        public bool Required { get; set; } = false;
        public string Value { get; set; } = "";

    }

}