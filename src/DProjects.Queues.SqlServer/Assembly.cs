using DProjects.Factories;

namespace DProjects.Queues.SqlServer {

    public class Assembly : IAssembly {
        public static System.Reflection.Assembly Instance = typeof(Assembly).Assembly;
    }

}