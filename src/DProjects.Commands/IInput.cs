using System;
using System.IO;

namespace DProjects.Commands {

    public interface IInput : IDisposable {

        TextReader CreateTextReader();
    }

}