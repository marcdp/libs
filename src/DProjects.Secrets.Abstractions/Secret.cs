
namespace DProjects.Secrets {

    public class Secret {

        //ctor
        public Secret(string name, string value) {
            Name = name;
            Value = value;
        }

        //props
        public string Name { get; private set; }
        public string Value { get; private set; }
        
    }

}