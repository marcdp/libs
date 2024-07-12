namespace DProjects.Repositories {

    public interface IGenericRepositoryElement<TKey> {
        TKey Id { get; set; }
    }

}