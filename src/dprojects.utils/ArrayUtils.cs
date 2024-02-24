using System;


namespace DProjects.Utils {


    public static class ArrayUtils {

        //base64
        public static bool IsArray(object value) {
            return typeof(Array).IsAssignableFrom(value.GetType());
        }

    }


}


