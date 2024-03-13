using DProjects.Factories;

namespace DProjects.Vault {

    public class Assembly : IAssembly {
        public static System.Reflection.Assembly Instance = typeof(Assembly).Assembly;
    }

}