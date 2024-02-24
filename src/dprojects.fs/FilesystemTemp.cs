
using DProjects.Utils;
using System;

namespace DProjects.Fs {


    public class FilesystemTemp : FilesystemLocal, IDisposable {

        //vars
        private bool mFile;

        //constructor
        public FilesystemTemp(string path, bool file) : base(path, false, true, file) {
            mFile = file;
        }
        public void Dispose() {
            FileUtils.Delete(mPath);
        }


        //properties
        public override string Url => "temp://" + (mFile ? "?file=true" : "");

    }


}

