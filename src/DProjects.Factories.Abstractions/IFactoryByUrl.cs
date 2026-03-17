using System;

namespace DProjects.Factories {

    public interface IFactoryByUrl<TType> where TType : class{

        TType Create(string url);
        
    }
    public interface IFactoryByUrl<TType, TArgument> where TType : class where TArgument : class {

        TType Create(string url, TArgument argument);
        

    }

}