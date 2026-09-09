using System.Net.Mail;
using System.Reflection;
using System.Text;

using DProjects.Db;
using DProjects.Factories;
using DProjects.MailSender;

namespace DProjects.MailSender.Tests {

    public class MailSenderTests {

        // methods
        [Fact]
        public async Task NullSender_CompletesWithoutAnExternalEffect() {
            var sender = new MailSenderNull();
            using var message = CreateMessage();

            await sender.SendAsync(message, TestContext.Current.CancellationToken);
        }
        [Fact]
        public async Task SingleRecipient_EnqueuesCompleteLogicalDeliveryData() {
            var database = RecordingDbProxy.Create();
            var sender = new MailSenderDb(database.Connection, "queue.example");
            using var message = CreateMessage();
            var before = DateTime.Now;

            await sender.SendAsync(message, TestContext.Current.CancellationToken);
            var after = DateTime.Now;

            var call = Assert.Single(database.Calls);
            Assert.Contains("INSERT INTO MailToSend", call.Sql, StringComparison.Ordinal);
            Assert.Equal("sender@example.com", call.Parameters[0]);
            Assert.Equal("to@example.net", call.Parameters[1]);
            Assert.Equal("Test subject", call.Parameters[2]);
            Assert.Matches("^<[0-9a-f-]+@queue\\.example>$", Assert.IsType<string>(call.Parameters[3]));
            var eml = Assert.IsType<byte[]>(call.Parameters[4]);
            Assert.Equal(eml.Length, call.Parameters[5]);
            Assert.False(string.IsNullOrWhiteSpace(Assert.IsType<string>(call.Parameters[7])));
            Assert.InRange(Assert.IsType<DateTime>(call.Parameters[10]), before, after);
            Assert.Equal(TestContext.Current.CancellationToken, call.CancellationToken);
            AssertEmlContains(eml, "sender@example.com", "to@example.net", "Test subject", "plain body");
        }
        [Fact]
        public async Task ToCcAndBcc_AreExpandedIntoOneDistinctDeliveryPerRecipient() {
            var database = RecordingDbProxy.Create();
            var sender = new MailSenderDb(database.Connection, "queue.example");
            using var message = CreateMessage();
            message.To.Add("second@example.net");
            message.CC.Add("copy@example.net");
            message.Bcc.Add("blind-one@example.net");
            message.Bcc.Add("blind-two@example.net");

            await sender.SendAsync(message, TestContext.Current.CancellationToken);

            Assert.Equal(5, database.Calls.Count);
            Assert.Equal(new[] { "blind-one@example.net", "blind-two@example.net", "copy@example.net", "second@example.net", "to@example.net" },
                database.Calls.Select(call => Assert.IsType<string>(call.Parameters[1])).Order());
            Assert.Equal(5, database.Calls.Select(call => call.Parameters[7]).Distinct().Count());
        }
        [Fact]
        public async Task Bcc_IsRoutingMetadataAndIsNotExposedInGeneratedMessageHeaders() {
            const string blindAddress = "private-bcc@example.net";
            var database = RecordingDbProxy.Create();
            var sender = new MailSenderDb(database.Connection, "queue.example");
            using var message = CreateMessage();
            message.Bcc.Add(blindAddress);

            await sender.SendAsync(message, TestContext.Current.CancellationToken);

            Assert.Contains(database.Calls, call => Assert.IsType<string>(call.Parameters[1]) == blindAddress);
            foreach (var call in database.Calls) {
                var eml = Encoding.UTF8.GetString(Assert.IsType<byte[]>(call.Parameters[4]));
                Assert.DoesNotContain("Bcc:", eml, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain(blindAddress, eml, StringComparison.OrdinalIgnoreCase);
            }
        }
        [Theory]
        [InlineData("configured.example", "configured.example")]
        [InlineData("", "example.com")]
        public async Task MessageId_UsesConfiguredDomainOrSenderDomainFallback(string configuredDomain, string expectedDomain) {
            var database = RecordingDbProxy.Create();
            var sender = new MailSenderDb(database.Connection, configuredDomain);
            using var message = CreateMessage();

            await sender.SendAsync(message, TestContext.Current.CancellationToken);

            Assert.EndsWith("@" + expectedDomain + ">", Assert.IsType<string>(Assert.Single(database.Calls).Parameters[3]), StringComparison.Ordinal);
        }
        [Fact]
        public async Task EmptyConfiguredDomain_IsResolvedPerMessageWithoutMutatingSenderConfiguration() {
            var database = RecordingDbProxy.Create();
            var sender = new MailSenderDb(database.Connection, "");
            using var first = CreateMessage();
            using var second = CreateMessage();
            first.From = new MailAddress("sender@first.example");
            second.From = new MailAddress("sender@second.example");

            await sender.SendAsync(first, TestContext.Current.CancellationToken);
            await sender.SendAsync(second, TestContext.Current.CancellationToken);

            Assert.EndsWith("@first.example>", Assert.IsType<string>(database.Calls[0].Parameters[3]), StringComparison.Ordinal);
            Assert.EndsWith("@second.example>", Assert.IsType<string>(database.Calls[1].Parameters[3]), StringComparison.Ordinal);
        }
        [Fact]
        public async Task HtmlAndAttachment_ArePreservedInGeneratedEml() {
            var database = RecordingDbProxy.Create();
            var sender = new MailSenderDb(database.Connection, "queue.example");
            using var message = CreateMessage();
            message.IsBodyHtml = true;
            message.Body = "<strong>html body</strong>";
            message.Attachments.Add(new Attachment(new MemoryStream(Encoding.UTF8.GetBytes("attachment payload")), "note.txt", "text/plain"));

            await sender.SendAsync(message, TestContext.Current.CancellationToken);

            var eml = Assert.IsType<byte[]>(Assert.Single(database.Calls).Parameters[4]);
            AssertEmlContains(eml, "text/html", "html body", "note.txt", "multipart/mixed");
        }
        [Fact]
#pragma warning disable xUnit1051 // deliberately verifies propagation of the exact caller token
        public async Task CancellationToken_IsPropagatedToDatabaseCall() {
            var database = RecordingDbProxy.Create(cancelWhenRequested: true);
            var sender = new MailSenderDb(database.Connection, "queue.example");
            using var message = CreateMessage();
            using var source = new CancellationTokenSource();
            source.Cancel();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => sender.SendAsync(message, source.Token));

            Assert.Equal(source.Token, Assert.Single(database.Calls).CancellationToken);
        }
#pragma warning restore xUnit1051
        [Fact]
        public async Task DatabaseFailure_IsPropagated() {
            var expected = new InvalidOperationException("synthetic database failure");
            var database = RecordingDbProxy.Create(exception: expected);
            var sender = new MailSenderDb(database.Connection, "queue.example");
            using var message = CreateMessage();

            var actual = await Assert.ThrowsAsync<InvalidOperationException>(() => sender.SendAsync(message, TestContext.Current.CancellationToken));

            Assert.Same(expected, actual);
        }
        [Fact]
        public void EmlFailure_DoesNotLeaveItsTemporaryPickupDirectory() {
            var database = RecordingDbProxy.Create();
            var sender = new MailSenderDb(database.Connection, "queue.example");
            using var invalidMessage = new MailMessage { Subject = "missing sender and recipient" };
            var before = GetOwnedTempDirectories();

            Assert.ThrowsAny<Exception>(() => sender.MaiMessageToEmlBuffer(invalidMessage));

            Assert.Empty(GetOwnedTempDirectories().Except(before));
        }
        [Fact]
        public void Factories_CreateExpectedSendersAndForwardDatabaseSource() {
            var database = RecordingDbProxy.Create();
            var connectionFactory = new RecordingConnectionFactory(database.Connection);

            Assert.IsType<MailSenderDb>(new MailSenderDbFactory(connectionFactory).Create("db:queue-connection"));
            Assert.Equal("queue-connection", connectionFactory.Source);
            Assert.IsType<MailSenderNull>(new MailSenderNullFactory().Create("null:"));
        }

        // methods (private)
        private static MailMessage CreateMessage() {
            var result = new MailMessage {
                From = new MailAddress("sender@example.com"),
                Subject = "Test subject",
                Body = "plain body"
            };
            result.To.Add("to@example.net");
            return result;
        }
        private static void AssertEmlContains(byte[] content, params string[] expectedValues) {
            var text = Encoding.UTF8.GetString(content);
            foreach (var expected in expectedValues) Assert.Contains(expected, text, StringComparison.OrdinalIgnoreCase);
        }
        private static HashSet<string> GetOwnedTempDirectories() {
            return Directory.GetDirectories(Path.GetTempPath(), "DProjects.MailSender-*").ToHashSet(StringComparer.OrdinalIgnoreCase);
        }

        private sealed class RecordingConnectionFactory(IDBConnection connection) : IFactoryByUrl<IDBConnection> {

            // props
            public string Source { get; private set; } = "";

            // methods
            public IDBConnection Create(string src) {
                Source = src;
                return connection;
            }
        }

        public class RecordingDbProxy : DispatchProxy {

            // vars
            private Exception? mException;
            private bool mCancelWhenRequested;

            // props
            public IDBConnection Connection { get; private set; } = null!;
            public List<DbCall> Calls { get; } = [];

            // methods
            public static RecordingDbProxy Create(Exception? exception = null, bool cancelWhenRequested = false) {
                var connection = DispatchProxy.Create<IDBConnection, RecordingDbProxy>();
                var result = (RecordingDbProxy)(object)connection;
                result.Connection = connection;
                result.mException = exception;
                result.mCancelWhenRequested = cancelWhenRequested;
                return result;
            }

            // methods (private)
            protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) {
                if (targetMethod?.Name == nameof(IDBConnection.ExecuteNonQueryAsync)) {
                    var call = new DbCall((string)args![0]!, (object?[]?)args[1] ?? [], (CancellationToken)args[2]!);
                    Calls.Add(call);
                    if (mException != null) return Task.FromException<long>(mException);
                    if (mCancelWhenRequested && call.CancellationToken.IsCancellationRequested) return Task.FromCanceled<long>(call.CancellationToken);
                    return Task.FromResult(1L);
                }
                if (targetMethod?.Name == nameof(IDisposable.Dispose)) return null;
                throw new NotSupportedException(targetMethod?.Name);
            }
        }

        public sealed record DbCall(string Sql, object?[] Parameters, CancellationToken CancellationToken);
    }
}
