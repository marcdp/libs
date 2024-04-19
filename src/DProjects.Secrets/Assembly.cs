using DProjects.Factories;

namespace DProjects.Secrets {

    public class Assembly : IAssembly {
        public static System.Reflection.Assembly Instance = typeof(Assembly).Assembly;
    }

}