using System.Text;

namespace DProjects.Types {


    public class I18nString {


        //value
        private string mValue;


        //constructor
        public I18nString(string value) {
            mValue = value;
        }


        //properties
        public string this[string lang] {
            get {
                var value = "";
                foreach (var part in mValue.Split('|')) {
                    if (part.StartsWith("i18n_") && part.IndexOf(':') != -1) {
                        var i = part.IndexOf(":");
                        var aux = part.Substring(i + 1);
                        if (aux.Length > 0) value = aux;
                        if (part.StartsWith("i18n_" + lang + ":")) break;
                    } else {
                        value = part;
                    }
                }
                return value;
            }
            set {
                var sb = new StringBuilder();
                var found = false;
                foreach (var part in mValue.Split('|')) {
                    if (part.StartsWith("i18n_") && part.IndexOf(':') != -1) {
                        var i = part.IndexOf(":");
                        var aux = part.Substring(i + 1);
                        if (part.StartsWith("i18n_" + lang + ":")) {
                            aux = value;
                            found = true;
                        }
                        if (aux.Length > 0) {
                            if (sb.Length > 0) sb.Append("|");
                            sb.Append(part.Substring(0, i)).Append(":").Append(aux);
                        }
                    }
                }
                if (!found) {
                    if (value.Length > 0) {
                        if (sb.Length > 0) sb.Append("|");
                        sb.Append("i18n_" + lang + ":").Append(value);
                    }
                }
                mValue = sb.ToString();
            }
        }


        //methods
        public override string ToString() {
            return mValue;
        }
    }

}
