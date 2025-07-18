using DProjects.Factories;

namespace DProjects.Azure {

    public class Assembly : IAssembly {
        public static System.Reflection.Assembly Instance = typeof(Assembly).Assembly;
    }
     
}