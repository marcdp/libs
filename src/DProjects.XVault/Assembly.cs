using DProjects.Factories;

namespace DProjects.XVault {

    public class Assembly : IAssembly {
        public static System.Reflection.Assembly Instance = typeof(Assembly).Assembly;
    }

}