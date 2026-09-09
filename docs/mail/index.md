# Mail

The mail family defines one responsibility: accept a .NET `MailMessage` asynchronously. The public contract lives in
`DProjects.MailSender.Abstractions`; `DProjects.MailSender` supplies a database-spooling implementation, a null implementation, and URL factories.

```text
DProjects.MailSender
        ↓
DProjects.MailSender.Abstractions
```

Despite the package name, this is not a general SMTP client framework. The concrete sender in this repository prepares messages for another system
to deliver.

## Message and backend boundary

`IMailSender.SendAsync` accepts a caller-created `System.Net.Mail.MailMessage` and a cancellation token. The abstraction does not define transport
credentials, delivery receipts, retry policy, batching, or sender disposal.

`MailSenderDb` converts the message to an EML buffer using a temporary pickup directory, then inserts one `MailToSend` row for each To, CC, and Bcc
recipient. Each row receives a generated message ID and unique recipient suffix. The database connection performs parameterized insertion; actual
delivery, retry scheduling, and interpretation of the table are outside this repository.

The conversion uses local temporary files and synchronous `SmtpClient.Send` in pickup-directory mode before the asynchronous database writes. The
cancellation token reaches database execution but cannot cancel that synchronous EML-generation phase. Exceptions from message conversion, local I/O,
or database execution propagate to the caller; no retry or compensation is implemented here. If one recipient insert fails after earlier inserts,
the sender does not establish an all-recipient transaction.

`MailSenderNull` completes successfully without retaining or delivering the message. It is an explicit sink and must not be interpreted as evidence
of delivery.

## Factory and ownership boundaries

The assembly marker supports factory discovery:

- `db:` delegates its nested connection text to `IFactoryByUrl<IDBConnection>` and constructs `MailSenderDb`;
- `null:` creates the no-op sender.

Authentication and connection details belong to the selected database connection URL and provider. `IMailSender` is not disposable, while the
database factory can create a retained connection. The current API does not express who closes that connection, so consumers should not assume the
sender propagates ownership or disposal.

The sender does not dispose the caller's `MailMessage`. Callers remain responsible for the message and any attachment streams according to
`MailMessage` ownership rules.

## Delivery semantics and verification

A successful call means the implemented work completed: for `MailSenderDb`, EML generation and the corresponding row inserts; for `MailSenderNull`,
only a no-op completion. It does not mean an SMTP server accepted the message or a recipient received it.

There is no dedicated mail test project or shared sender contract suite. Recipient expansion, MIME preservation, temporary-directory cleanup on
failure, database schema compatibility, partial insert behavior, cancellation, factory parsing, and connection lifecycle are not directly verified.
The family is consequently a narrow compatibility surface with limited evidence; see [Support and status](../support.md).

Return to the [documentation index](../index.md), or see [Database](../database/index.md) and [Factories](../factories/index.md) for the dependencies
used by the database sender.
