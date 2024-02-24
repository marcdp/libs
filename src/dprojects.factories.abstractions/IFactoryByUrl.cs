using System;

namespace DProjects.Factories {

    public interface IFactoryByUrl<TType> {

        TType Create(string url); 

    }

}