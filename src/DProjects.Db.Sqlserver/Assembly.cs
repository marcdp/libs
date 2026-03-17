using System;
using System.Collections.Generic;
using System.IO;
using DProjects.Factories;

namespace DProjects.Db.SqlServer {

    public class Assembly : IAssembly { 
        public static System.Reflection.Assembly Instance = typeof(Assembly).Assembly;
    }

}