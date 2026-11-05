using DProjects.Factories;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("DProjects.XVault.Test")]

namespace DProjects.XVault {

    public class Assembly : IAssembly {
        public static System.Reflection.Assembly Instance = typeof(Assembly).Assembly;
    }

}
