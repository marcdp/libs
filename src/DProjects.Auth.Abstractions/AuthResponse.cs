
namespace DProjects.Auth {


    public enum AuthFieldType {
        String,
        Password
    }
    public class AuthField(string name, string label, AuthFieldType type) { 
        public string Name { get; } = name;
        public string Label { get; } = label;
        public AuthFieldType Type { get; } = type;
        public string Description { get; set; } = "";
        public string PlaceHolder { get; set; } = "";
        public bool Required { get; set; } = false;
        public string Value { get; set; } = "";
    }


    public class AuthResponse(AuthStatus status, string statusDescription, AuthField[] fields, AuthUser? user) {

        
        //props
        public AuthStatus Status { get; } = status;
        public string StatusDescription { get; } = statusDescription;
        public AuthField[] Fields { get; } = fields;
        public AuthUser? User { get; } = user;

        //methods
        public static AuthResponse DataRequired(AuthField[] fields) {
            return new AuthResponse(AuthStatus.DataRequired, "", fields, null);
        }
        public static AuthResponse Failure(AuthField[]? fields = null) {
            return new AuthResponse(AuthStatus.Failure, "Wrong credentials", fields ?? [], null);
        }
        public static AuthResponse Success(AuthUser user, AuthField[]? fields = null) {
            return new AuthResponse(AuthStatus.Success, "", fields ?? [], user);
        }
    }

}