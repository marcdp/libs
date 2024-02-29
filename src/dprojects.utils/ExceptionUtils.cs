
using System;
using System.Reflection;
using System.Text;

namespace DProjects.Utils {


    public static class ExceptionUtils {

        public static string GetMessageDetailed(this Exception? exception, bool stackTrace = true) {
            if (exception == null) throw new ArgumentNullException("exception");
            var message = new StringBuilder();
            message.AppendLine(exception.Message);
            if (stackTrace) {
                message.AppendLine(exception.GetType().FullName);
                message.AppendLine(exception.StackTrace);
            }
            var aggregate = exception as AggregateException;
            if (aggregate != null) {
                var flat = aggregate.Flatten();
                if (flat.InnerExceptions.Count == 1) {
                    message.AppendLine(GetMessageDetailed(flat.InnerException, stackTrace));
                } else if (flat.InnerExceptions.Count > 0) {
                    foreach (var innerEx in flat.InnerExceptions) {
                        message.AppendLine(GetMessageDetailed(innerEx, stackTrace));
                    }
                }
            } else {
                if (exception is TargetInvocationException targetInvocationEx && targetInvocationEx.InnerException != null) {
                    message.AppendLine(GetMessageDetailed(targetInvocationEx.InnerException, stackTrace));
                } else if (exception.InnerException != null) {
                    message.AppendLine(GetMessageDetailed(exception.InnerException, stackTrace));
                }
            }
            return message.ToString();
        }

    }

}


