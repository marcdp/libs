using System;
using System.IO;
using System.Threading.Tasks;
using System.Threading;


namespace DProjects.Streams {


    public class SpongeOutputStream : OutputStream {


        //variables
        private Action<Stream>? mHandlerSync;
        private Func<Stream, Task>? mHandlerAsync;
        private MemoryStream mMemoryStream;
        private string? mTempFileName;
        private FileStream? mTempFileStream;
        private Stream? mOutputStream;

        //ctr
        public SpongeOutputStream(int bufferSize, Action<Stream> handler) {
            mMemoryStream = new MemoryStream(bufferSize);
            mHandlerSync = handler;
        }
        public SpongeOutputStream(int bufferSize, Func<Stream, Task> handler) {
            mMemoryStream = new MemoryStream(bufferSize);
            mHandlerAsync = handler;
        }
        public SpongeOutputStream(int bufferSize, Stream output) {
            mMemoryStream = new MemoryStream(bufferSize);
            mOutputStream = output;
        }
        protected override void Dispose(bool disposing) {
            if (mHandlerSync != null) {
                if (mTempFileStream != null) {
                    using var stream = mTempFileStream;
                    stream.Seek(0, SeekOrigin.Begin);
                    mHandlerSync(stream);
                    stream.Dispose();
                } else {
                    using var stream = mMemoryStream;
                    stream.Seek(0, SeekOrigin.Begin);
                    mHandlerSync(stream);
                }
                if (mTempFileName != null) {
                    File.Delete(mTempFileName);
                }
                mHandlerSync = null;
            } else if (mOutputStream!= null) {
                if (mTempFileStream != null) {
                    using var stream = mTempFileStream;
                    stream.Seek(0, SeekOrigin.Begin);
                    stream.CopyTo(mOutputStream);
                    stream.Dispose();
                } else {
                    using var stream = mMemoryStream;
                    stream.Seek(0, SeekOrigin.Begin);
                    stream.CopyTo(mOutputStream);
                }
                if (mTempFileName != null) {
                    File.Delete(mTempFileName);
                }
                mOutputStream = null;
            }
        }
        public async ValueTask DisposeAsync() {
            if (mHandlerAsync != null) {
                if (mTempFileStream != null) {
                    using var stream = mTempFileStream;
                    stream.Seek(0, SeekOrigin.Begin);
                    await mHandlerAsync(stream);
                    stream.Dispose();
                } else {
                    using var stream = mMemoryStream;
                    stream.Seek(0, SeekOrigin.Begin);
                    await mHandlerAsync(stream);
                }
                if (mTempFileName != null) {
                    File.Delete(mTempFileName);
                }
                mHandlerAsync = null;
            } else if (mOutputStream!= null) {
                if (mTempFileStream != null) {
                    using var stream = mTempFileStream;
                    stream.Seek(0, SeekOrigin.Begin);
                    await stream.CopyToAsync(mOutputStream);
                    stream.Dispose();
                } else {
                    using var stream = mMemoryStream;
                    stream.Seek(0, SeekOrigin.Begin);
                    await stream.CopyToAsync(mOutputStream);
                }
                if (mTempFileName != null) {
                    File.Delete(mTempFileName);
                }
                mOutputStream = null;
            }
        }


        //methods
        public override void Flush() {
        }
        public override Task FlushAsync(CancellationToken cancellationToken) {
            return Task.CompletedTask;
        }
        public override void Write(byte[] buffer, int offset, int count) {
            var remaining = (int) mMemoryStream.Capacity - (int) mMemoryStream.Length;
            if (remaining > 0) {
                var bytesToWrite = Math.Min(remaining, count);
                mMemoryStream.Write(buffer, offset, bytesToWrite);
                offset += bytesToWrite;
                count -= bytesToWrite;
            }
            if (count > 0) {
                if (mTempFileStream == null) {
                    mTempFileName = Path.GetTempFileName();
                    mTempFileStream = new FileStream(mTempFileName, FileMode.Create, FileAccess.ReadWrite);
                    mMemoryStream.WriteTo(mTempFileStream);
                }
                mTempFileStream.Write(buffer, offset, count);
            }
        }
        public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) {
            var remaining = (int)mMemoryStream.Capacity - (int)mMemoryStream.Length;
            if (remaining > 0) {
                var bytesToWrite = Math.Min(remaining, count);
                mMemoryStream.Write(buffer, offset, bytesToWrite);
                offset += bytesToWrite;
                count -= bytesToWrite;
            }
            if (count > 0) {
                if (mTempFileStream == null) {
                    mTempFileName = Path.GetTempFileName();
                    mTempFileStream = new FileStream(mTempFileName, FileMode.Create, FileAccess.ReadWrite);
                    mMemoryStream.WriteTo(mTempFileStream);
                }
                await mTempFileStream.WriteAsync(buffer, offset, count);
            }
        }

    }

}
