
namespace DProjects.Utils {

    public static class EnumUtils {

        public static T TryParse<T>(string value, T defaultValue = default) where T : struct {
            if (System.Enum.TryParse<T>(value, true, out T result)) return result;
            return defaultValue;
        }
    }

}


