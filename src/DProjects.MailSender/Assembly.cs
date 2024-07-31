using DProjects.Factories;

namespace DProjects.MailSender {

    public class Assembly : IAssembly {
        public static System.Reflection.Assembly Instance = typeof(Assembly).Assembly;
    }

}