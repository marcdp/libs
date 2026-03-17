using DProjects.Factories;

namespace DProjects.Log.Storage {

    public class Assembly : IAssembly { 
        public static System.Reflection.Assembly Instance = typeof(Assembly).Assembly;
    }

}