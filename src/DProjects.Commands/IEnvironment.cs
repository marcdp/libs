namespace DProjects.Commands {
    public interface IEnvironment {

        public IInput In { get; }
        public IOutput Out {get;}
        public IOutput Err { get; }

        public void GetVariable(string name);
        public void SetVariable(string name, string value);

    }

}