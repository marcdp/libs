
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DProjects.Commands {

    public interface ICommand {

        //methods
        Task<int> ExecuteAsync(CancellationToken cancellationToken);

    }

    public interface IEnvironment {

        public IInput In { get; }
        public IOutput Out {get;}
        public IOutput Err { get; }

        public void GetVariable(string name);
        public void SetVariable(string name, string value);

    }

    public interface IInput {

    }
    public interface IOutput {
        
    }


    public class Environment : IEnvironment {

        public IInput In => throw new System.NotImplementedException();

        public IOutput Out => throw new System.NotImplementedException();

        public IOutput Err => throw new System.NotImplementedException();

        public void GetVariable(string name) {
            throw new System.NotImplementedException();
        }

        public void SetVariable(string name, string value) {
            throw new System.NotImplementedException();
        }
    }

}