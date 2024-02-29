namespace DProjects.Factories {
    public interface IFactoryByUrlAndArgument<TType,TArgument> {

        TType Create(string url, TArgument argument);

    }

}