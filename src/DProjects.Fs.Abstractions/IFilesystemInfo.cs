
namespace DProjects.Fs {


    public interface IFilesystemInfo {


        //properties
        bool IsReadonly { get; set; }
        /// <summary>
        /// Returns a safe filesystem identity URL for diagnostics. Secret credentials are omitted, so the URL may not recreate an authenticated
        /// filesystem.
        /// </summary>
        string Url { get; }

    }


}

