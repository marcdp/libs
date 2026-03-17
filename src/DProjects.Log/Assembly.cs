using DProjects.Factories;

namespace DProjects.Log {

    public class Assembly : IAssembly { 
        public static System.Reflection.Assembly Instance = typeof(Assembly).Assembly;
    }

}