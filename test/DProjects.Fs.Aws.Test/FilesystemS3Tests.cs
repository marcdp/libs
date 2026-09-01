using DProjects.Fs.Test;

namespace DProjects.Fs.Aws.Test {

    [Trait("Category", "Integration")]
    public class FilesystemS3Tests : FilesystemTests {

        public FilesystemS3Tests() : base("user-secret:s3-bucket", typeof(DProjects.Fs.Aws.Assembly).Assembly) {
        }

    }
}
