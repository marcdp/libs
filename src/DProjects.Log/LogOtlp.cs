using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Threading.Channels;

using DProjects.Utils;
using System.Threading;

namespace DProjects.Log {

    public class LogOtlp : Log {


        //vars
        private readonly string mHost;
        private readonly int mPort;
        private readonly string mServiceName;
        private readonly string mScopeName;        
        private readonly HttpClient mHttpClient;
        private readonly Channel<Serializers.LogEntrySerializerOtlp.LogRecord?> mQueue;

        private readonly Task mExportTask;

        private readonly int mMaxWaitTime = 10000;
        private readonly int mMaxBatchSize = 100;


        //constructor
        public LogOtlp(string host, int port, string serviceName, string scopeName) : base(false, false) {
            mHost = host;
            mPort = port;
            mServiceName = serviceName;
            mScopeName = scopeName;
            mQueue = Channel.CreateUnbounded<Serializers.LogEntrySerializerOtlp.LogRecord?>();
            mExportTask = Task.Run(() => ExportAsync(default));
            var httpClientHandler = new HttpClientHandler();
            mHttpClient = new HttpClient(httpClientHandler);
            mHttpClient.BaseAddress = new Uri("http://" + host + ":" + port);

            throw new NotImplementedException();
            
        }
        public override void Dispose() {
            mQueue.Writer.TryWrite(null);
            mQueue.Writer.Complete();
            //wait until complete
        }


        //methods
        protected override void ProcessEntry(LogEntry logEntry) {
            mQueue.Writer.TryWrite(new Serializers.LogEntrySerializerOtlp(mServiceName, mScopeName).CreateLogRecord(logEntry));
        }
        private async Task ExportAsync(CancellationToken cancellationToken) {
            var buffer = new List<Serializers.LogEntrySerializerOtlp.LogRecord>(mMaxBatchSize);
            while (!mQueue.Reader.Completion.IsCompleted) {
                var timeoutTask = Task.Delay(mMaxWaitTime, cancellationToken);
                //clear buffer
                buffer.Clear();
                //read batch
                while (buffer.Count < mMaxBatchSize) {
                    var readTask = mQueue.Reader.WaitToReadAsync(cancellationToken).AsTask();
                    var completedTask = await Task.WhenAny(readTask, timeoutTask);
                    if (completedTask == timeoutTask) {
                        break; // timeout hit
                    }
                    while (mQueue.Reader.TryRead(out var item)) {
                        if (item == null) continue;
                        buffer.Add(item);
                        if (buffer.Count >= mMaxBatchSize)
                            break;
                    }
                }
                //send
                if (buffer.Count > 0) {
                    await SendBatchAsync(buffer.ToArray(), cancellationToken);
                }
            }
            //remaining
            await foreach (var logEntry in mQueue.Reader.ReadAllAsync(cancellationToken)) {
                if (logEntry == null) continue;
                buffer.Add(logEntry);
            }
            if (buffer.Count > 0) {
                await SendBatchAsync(buffer.ToArray(), cancellationToken);
            }
            //

        }
        private async Task SendBatchAsync(Serializers.LogEntrySerializerOtlp.LogRecord[] records, CancellationToken cancellationToken) {
            // TODO: Implement the logic to send the batch of log records to the OTLP endpoint using mHttpClient.
        }



    }

}

