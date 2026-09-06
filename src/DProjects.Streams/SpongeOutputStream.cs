using System;
using System.IO;
using System.Threading.Tasks;
using System.Threading;


namespace DProjects.Streams {


    public class SpongeOutputStream : OutputStream, IDisposable {


        //variables
        private Action<Stream>? mHandler;
        private Func<Stream, Task>? mHandlerAsync;
        private MemoryStream mMemoryStream;
        private string? mTempFileName;
        private FileStream? mTempFileStream;
        private Stream? mOutputStream;

        //ctr
        public SpongeOutputStream(int bufferSize, Action<Stream> handler) {
            mMemoryStream = new MemoryStream(bufferSize);
            mHandler = handler;
        }
        public SpongeOutputStream(int bufferSize, Func<Stream, Task> handler) {
            mMemoryStream = new MemoryStream(bufferSize);
            mHandlerAsync = handler;
        }
        public SpongeOutputStream(int bufferSize, Stream output) {
            mMemoryStream = new MemoryStream(bufferSize);
            mOutputStream = output;
        }


        private static readonly TaskFactory mMyTaskFactory = new(CancellationToken.None,
            TaskCreationOptions.None, TaskContinuationOptions.None, TaskScheduler.Default);
        public static TResult RunSync<TResult>(Func<Task<TResult>> func) {
            var cultureUi = System.Globalization.CultureInfo.CurrentUICulture;
            var culture = System.Globalization.CultureInfo.CurrentCulture;
            return mMyTaskFactory.StartNew(() => {
                Thread.CurrentThread.CurrentCulture = culture;
                Thread.CurrentThread.CurrentUICulture = cultureUi;
                return func();
            }).Unwrap().GetAwaiter().GetResult();
        }
        public static void RunSync(Func<Task> func) {
            var cultureUi = System.Globalization.CultureInfo.CurrentUICulture;
            var culture = System.Globalization.CultureInfo.CurrentCulture;
            mMyTaskFactory.StartNew(() => {
                Thread.CurrentThread.CurrentCulture = culture;
                Thread.CurrentThread.CurrentUICulture = cultureUi;
                return func();
            }).Unwrap().GetAwaiter().GetResult();
        }

        protected override void Dispose(bool disposing) {
            if (!disposing) return;
            if (mHandler != null) {
                if (mTempFileStream != null) {
                    using var stream = mTempFileStream;
                    stream.Seek(0, SeekOrigin.Begin);
                    mHandler(stream);
                    stream.Dispose();
                } else {
                    using var stream = mMemoryStream;
                    stream.Seek(0, SeekOrigin.Begin);
                    mHandler(stream);
                }
                if (mTempFileName != null) {
                    File.Delete(mTempFileName);
                }
                mHandler = null;
            } else if (mHandlerAsync != null) {
                RunSync(() => CompleteAsync(CancellationToken.None));
            } else if (mOutputStream != null) {
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
            base.Dispose(disposing);
        }
        public async ValueTask DisposeAsync() {
            if (mHandlerAsync != null) {
                await CompleteAsync(CancellationToken.None).ConfigureAwait(false);
            } else {
                Dispose();
            }
            GC.SuppressFinalize(this);
        }

        private async Task CompleteAsync(CancellationToken cancellationToken) {
            var handler = mHandlerAsync;
            if (handler == null) return;
            mHandlerAsync = null;
            try {
                var stream = (Stream?)mTempFileStream ?? mMemoryStream;
                stream.Seek(0, SeekOrigin.Begin);
                await handler(stream).ConfigureAwait(false);
            } finally {
                mTempFileStream?.Dispose();
                mMemoryStream.Dispose();
                if (mTempFileName != null) File.Delete(mTempFileName);
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
                await mTempFileStream.WriteAsync(buffer, offset, count, cancellationToken);
            }
        }

    }

}
