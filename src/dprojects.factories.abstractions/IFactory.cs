using System;

namespace DProjects.Factories {


    public interface IFactory<TType> {

        TType Create();

    }

}