using Microsoft.Extensions.Logging;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Resources;
using System;
using System.Threading;

namespace DProjects.Log.OpenTelemetry {

    public class LogOtlp : LogLogger {


        //variables
        private readonly ILoggerFactory? mOwnedLoggerFactory;
        private int mDisposed;


        //constructors
        public LogOtlp(string host, int port)
            : this(host, port, "", "", LogLevel.Information) {
        }
        public LogOtlp(string host, int port, string serviceName, string scopeName, LogLevel level = LogLevel.Information)
            : this(CreateLoggerFactory(host, port, serviceName), scopeName, level, true) {
            Endpoint = CreateEndpoint(host, port);
            ServiceName = serviceName;
        }
        public LogOtlp(ILoggerFactory loggerFactory, string scopeName = "", LogLevel level = LogLevel.Information)
            : this(loggerFactory, scopeName, level, false) {
        }
        private LogOtlp(ILoggerFactory loggerFactory, string scopeName, LogLevel level, bool ownsLoggerFactory)
            : base(
                (loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory))).CreateLogger(GetScopeName(scopeName)),
                level
            ) {
            ScopeName = GetScopeName(scopeName);
            ServiceName = "";
            if (ownsLoggerFactory) {
                mOwnedLoggerFactory = loggerFactory;
            }
        }


        //properties
        public Uri? Endpoint { get; }
        public string ServiceName { get; }
        public string ScopeName { get; }


        //methods
        public override void Dispose() {
            if (Interlocked.Exchange(ref mDisposed, 1) != 0) return;
            base.Dispose();
            mOwnedLoggerFactory?.Dispose();
        }


        //private
        private static ILoggerFactory CreateLoggerFactory(string host, int port, string serviceName) {
            var endpoint = CreateEndpoint(host, port);
            return LoggerFactory.Create(builder => {
                builder.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Trace);
                builder.AddOpenTelemetry(options => {
                    options.IncludeFormattedMessage = true;
                    options.ParseStateValues = true;
                    if (!String.IsNullOrWhiteSpace(serviceName)) {
                        options.SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(serviceName));
                    }
                    options.AddOtlpExporter(exporter => {
                        exporter.Endpoint = endpoint;
                        exporter.Protocol = OtlpExportProtocol.HttpProtobuf;
                    });
                });
            });
        }
        private static Uri CreateEndpoint(string host, int port) {
            if (String.IsNullOrWhiteSpace(host)) {
                throw new ArgumentException("OTLP host cannot be empty.", nameof(host));
            }
            if (port < 1 || port > 65535) {
                throw new ArgumentOutOfRangeException(nameof(port), port, "OTLP port must be between 1 and 65535.");
            }
            var endpoint = new UriBuilder(Uri.UriSchemeHttp, host, port, "/v1/logs");
            return endpoint.Uri;
        }
        private static string GetScopeName(string scopeName) {
            return String.IsNullOrWhiteSpace(scopeName) ? typeof(LogOtlp).FullName! : scopeName;
        }

    }

}
