namespace DProjects.Functional {

    public sealed class Error(string code, string description) {

        // props
        public string Code { get; } = code;
        public string Description { get; } = description;

    }

}