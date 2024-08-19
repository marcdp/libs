
namespace DProjects.Identity.SignIn {

    public class SignInField(string name, string label, SignInFieldType type) { 

        public string Name { get;  } = name;
        public string Label { get; } = label;
        public SignInFieldType Type { get; } = type;
        public string Description { get; set; } = "";
        public string PlaceHolder { get; set; } = "";
        public bool Required { get; set; } = false;
        public string Value { get; set; } = "";

    }

}