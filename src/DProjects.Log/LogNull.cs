namespace DProjects.Log {

    public class LogNull : Log {


        //constructor
        public LogNull() : base(false, false) {
        }

        //methods
        protected override void ProcessEntry(LogEntry logEntry) {            
        }

    }

}

