using System.Net;
using System.Net.Http;
using System.Text;
using DProjects.Utils;

namespace DProjects.Fs.Aws.Test;

public class FilesystemS3HardeningTests {
    private const string AccessKey = "TESTACCESS123";
    private const string Secret = "SUPER_SECRET_DO_NOT_EXPOSE";

    [Fact]
    public void S3UrlOmitsRawAndEncodedSecret() {
        using var filesystem = CreateFilesystem();

        Assert.Contains(AccessKey, filesystem.Url);
        Assert.Contains("bucket.s3-eu-west-1.amazonaws.com", filesystem.Url);
        Assert.Contains("autogzip=true", filesystem.Url);
        Assert.DoesNotContain(Secret, filesystem.Url);
        Assert.DoesNotContain(UrlUtils.UrlEncode(Secret), filesystem.Url);
    }

    [Fact]
    public void S3BucketsUrlOmitsRawAndEncodedSecret() {
        using var filesystem = new FilesystemS3Buckets(
            "eu-west-1",
            AccessKey,
            Secret,
            "",
            "",
            true,
            false,
            "",
            false,
            new Dictionary<string, string>(),
            false);

        Assert.Contains(AccessKey, filesystem.Url);
        Assert.DoesNotContain(Secret, filesystem.Url);
        Assert.DoesNotContain(UrlUtils.UrlEncode(Secret), filesystem.Url);
    }

    [Fact]
    public async Task PreCancelledS3OperationDoesNotReachHttpHandler() {
        using var handler = new RecordingHandler();
        using var filesystem = CreateFilesystem(handler);
        using var source = new CancellationTokenSource();
        source.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            filesystem.GetEntryAsync("/file", source.Token));

        Assert.Equal(0, handler.RequestCount);
    }

    [Fact]
    public async Task DisposingS3WithInjectedHandlerDoesNotDisposeSharedHandler() {
        using var handler = new RecordingHandler();
        var filesystemA = CreateFilesystem(handler);
        using var filesystemB = CreateFilesystem(handler);

        filesystemA.Dispose();
        var result = await filesystemB.GetEntryAsync("/missing", TestContext.Current.CancellationToken);

        Assert.Null(result);
        Assert.False(handler.IsDisposed);
        Assert.Equal(1, handler.RequestCount);
    }

    [Fact]
    public async Task DisposingStandaloneS3DisposesItsHttpClient() {
        var filesystem = CreateFilesystem();
        filesystem.Dispose();

        await Assert.ThrowsAsync<ObjectDisposedException>(() =>
            filesystem.GetEntryAsync("/missing", TestContext.Current.CancellationToken));
    }

    private static FilesystemS3 CreateFilesystem(HttpClientHandler? handler = null) =>
        new("bucket", "eu-west-1", AccessKey, Secret, "/", true, false, false, handler);

    private sealed class RecordingHandler : HttpClientHandler {
        public int RequestCount { get; private set; }
        public bool IsDisposed { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) {
            RequestCount++;
            cancellationToken.ThrowIfCancellationRequested();
            const string xml = """
                <?xml version="1.0" encoding="UTF-8"?>
                <ListBucketResult xmlns="http://s3.amazonaws.com/doc/2006-03-01/">
                  <IsTruncated>false</IsTruncated>
                </ListBucketResult>
                """;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) {
                Content = new StringContent(xml, Encoding.UTF8, "application/xml")
            });
        }

        protected override void Dispose(bool disposing) {
            IsDisposed = true;
            base.Dispose(disposing);
        }
    }
}
