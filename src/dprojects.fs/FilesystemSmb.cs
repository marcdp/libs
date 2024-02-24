using DProjects.Utils;
using System;
using System.Threading.Tasks;


namespace DProjects.Fs {


    public class FilesystemSmb : FilesystemLocal, IDisposable {


        //variables
        protected string mHost;
        protected string mShare;
        protected string mUsername;
        protected string mPassword;
        protected Uri mUrl;


        //constructor
        public FilesystemSmb(Uri url, bool isReadonly) : base("/", isReadonly, false, false) {
            mUrl = url;
            if (mUrl.AbsolutePath.Length > 1 && mUrl.AbsolutePath.EndsWith("/")) throw new Exception("Url should not end with /");
            mUsername = "";
            mPassword = "";
            if (!String.IsNullOrEmpty(url.UserInfo)) {
                var aux = url.UserInfo + ":";
                mUsername = UrlUtils.UrlDecode(aux.Split(':')[0]);
                mPassword = UrlUtils.UrlDecode(aux.Split(':')[1]);
            }
            if (EnvironmentUtils.IsWindows()) {
                mHost = url.Host;
                mShare = url.AbsolutePath.Split('/')[1];
                mPath = "\\\\" + mHost + url.AbsolutePath.Replace("/", "\\");
            } else {
                mHost = url.Host;
                mShare = url.PathAndQuery.Substring(1);
                mPath = "/mnt/" + mHost + "-" + mShare;
            }
            //mount remote filesystem
            AsyncUtils.RunSync(async () => {
                if (EnvironmentUtils.IsWindows()) {
                    //autenticate
                    if (!String.IsNullOrEmpty(mUsername)) {
                        //net use \\172.1.1.12\Share /user:USER PASSWORD"
                        var arguments = "use \\\\" + mHost + "\\" + mShare + " /user:" + mUsername + " " + mPassword;
                        var processResult = await ProcessUtils.ExecuteNativeProcessAsync("net", arguments, default);
                        if (processResult.ExitCode != 0) {
                            throw new Exception("Error mounting FilesystemSmb //" + mHost + "/" + mShare + " on " + mPath + ": " + processResult.Output + processResult.Error + " (code " + processResult.ExitCode + ", username: " + mUsername + ", password: " + (mPassword + "     ").Substring(0, 5) + "..." + ")");
                        }
                    }
                } else {
                    //make dir
                    var processResult = await ProcessUtils.ExecuteNativeProcessAsync("mkdir", "-p " + mPath, default);
                    if (processResult.ExitCode != 0) {
                        throw new NotImplementedException("FilesystemSmb is not implemented in this platform , return code is " + processResult.ExitCode );
                    }
                    //run
                    //mount -t cifs //192.168.1.88/shares /mnt/share -o username=USERNAME,password=PASSWD,vers=3.0
                    var arguments = "-t cifs //" + mHost + "/" + mShare + " " + mPath + " -o vers=3.0" + (!String.IsNullOrEmpty(mUsername) ? ",username=" + mUsername + ",password=" + mPassword : "");
                    processResult = await ProcessUtils.ExecuteNativeProcessAsync("mount", arguments, default);
                    if (processResult.ExitCode  != 0) {
                        processResult = await ProcessUtils.ExecuteNativeProcessAsync("rmdir", mPath, default);
                        var cmdToLog = arguments;
                        if (!string.IsNullOrEmpty(StringUtils.GetConnectionStringVariable(arguments, "password"))) {
                            cmdToLog = cmdToLog.Replace(StringUtils.GetConnectionStringVariable(arguments, "password"), "******");
                        }
                        throw new Exception("Error mounting FilesystemSmb //" + mHost + "/" + mShare + " on " + mPath + ": " + processResult.Output + processResult.Error + " (code " + processResult.ExitCode + ", cmd: mount " + cmdToLog + ", username: " + mUsername + ", password: " + (mPassword + "     ").Substring(0, 5) + "..." + ")");
                    }
                }
            });
        }
        public void Dispose() {
            //unmount remote
            if (EnvironmentUtils.IsWindows()) {
            } else {
                AsyncUtils.RunSync(async () => {
                    await ProcessUtils.ExecuteNativeProcessAsync("umount", mPath, default);
                });
            }
        }


        //properties
        public override string Url {
            get {
                return "smb://" + (mUsername != null ? UrlUtils.UrlEncode(mUsername) + ":" + UrlUtils.UrlEncode(mPassword) + "@" : "") + mHost + "/" + mShare;
            }
        }

    }

}


