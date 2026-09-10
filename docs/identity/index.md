# Identity

> **Status: Experimental**
>
> `DProjects.Identity` is an evolving set of abstractions for simple, implementation-independent identity operations. Its current architecture
> explores a separation between authentication (`ISignIn`), membership (`IMembership`), and standard .NET claims-based identity.
>
> The subsystem is not considered mature or API-stable. Current implementations primarily exercise and validate the abstractions, and the contracts
> may evolve as additional authentication and membership scenarios are explored.

## Purpose

The Identity subsystem establishes a small architectural foundation for answering two separate questions:

```text
Identity
├── SignIn       How does an identity become authenticated?
└── Membership   What identities, users, and roles exist, and how can they be accessed or managed?
```

Keeping these concerns separate lets application code depend on only the identity capability it needs. It also avoids requiring every provider to
combine authentication with user and role storage. A provider may support sign-in, membership, or both; the current architecture does not require
the two contracts to be implemented together.

The subsystem is deliberately small. It defines a foundation that concrete providers and real application scenarios can test and extend, rather
than attempting to specify a complete identity framework in advance.

## Architectural boundaries

The subsystem is split across two `netstandard2.0` projects:

```text
DProjects.Identity
        ↓
DProjects.Identity.Abstractions
```

`DProjects.Identity.Abstractions` contains the public sign-in and membership contracts and their shared models. `DProjects.Identity` contains the
current concrete implementations, URL factories, and an assembly marker for factory discovery. This dependency direction allows higher-level code
to depend on identity contracts without selecting a concrete provider in code, while future implementations can remain outside the conceptual core.

Authentication mechanism details belong behind `ISignIn`. Membership storage and management belong behind `IMembership`. URL-based construction is
a composition concern provided by the repository's factory architecture; it does not define identity semantics.

## Sign-in

`ISignIn` represents authentication without assuming that authentication means a username and password. Its public contract contains one
asynchronous operation accepting a `SignInRequest` and returning a `SignInResponse`:

```text
SignInRequest → ISignIn → SignInResponse
```

Requests and responses carry string headers, while a response has one of three statuses:

- `DataRequired` means that the implementation needs the client to provide additional information described by response fields.
- `Failure` means that the authentication attempt did not produce an authenticated principal.
- `Success` returns the authenticated identity as a `ClaimsPrincipal`.

This forms a small authentication conversation rather than a fixed credential DTO:

```text
Client
  |
  | SignIn(request)
  v
ISignIn
  |
  +-- DataRequired ---> collect/provide more data ---+
  |                                                  |
  +-- Failure                                        |
  |                                                  |
  +-- Success ---> ClaimsPrincipal                   |
                                                     |
             <---------------------------------------+
```

`DataRequired` and `SignInField` allow an implementation to describe information it needs instead of expanding `ISignIn` for each authentication
mechanism. This leaves room for static or local credentials, external identity providers, OAuth/OIDC-style flows, MFA or challenge exchanges,
tokens, certificates, passkeys, and other mechanisms. These are extension possibilities, not claims that those providers currently exist or that
every conceivable authentication protocol can already be represented.

`SignInField` currently combines an input requirement with presentation-oriented metadata such as a label, description, placeholder, requirement
flag, and field type. The field types also include display-oriented values such as `Title` and `Paragraph`. This is an evolving design boundary:
experience from future clients and providers will determine whether the combined representation remains useful or whether authentication
requirements and presentation concerns need clearer separation.

## Membership

`IMembership` is independent of authentication. It currently exposes asynchronous operations to retrieve, list, add, save, and remove users and
roles. It does not require a membership provider to authenticate those users, and an `ISignIn` implementation does not need to expose membership.

`IMembershipUserAccessor` already demonstrates a smaller capability alongside the broader membership contract: it exposes only user retrieval, and
`IMembership` extends it. The architectural direction is to prefer small, capability-oriented abstractions when concrete providers or consumers show
that they need only part of a larger contract. Further capabilities should be driven by real use cases rather than designed speculatively.

The current membership model represents a user as one or more `MembershipIdentity` instances. An identity carries profile values, roles, claims,
password data, tokens, and keys; roles have their own claims. This representation is exploratory. In particular, the correct boundary between
identity and profile information, authentication credentials, issued tokens, and keys or other authentication material remains open and should not
be treated as stable. Additional authentication mechanisms and providers are expected to inform how this model evolves.

## Claims-based identity boundary

The subsystem uses the standard .NET identity types as the boundary presented to application code. A successful `ISignIn` response exposes a
`ClaimsPrincipal`, and `MembershipUser` can translate its membership identities, roles, and claims into `ClaimsIdentity` instances collected in a
`ClaimsPrincipal`.

```text
many authentication mechanisms
            |
            v
         ISignIn
            |
            v
     ClaimsPrincipal
            |
            v
     application code
```

This keeps application-facing authorization and identity handling based on standard .NET claims primitives while allowing the authentication or
membership implementation behind them to be replaced. The repository does not introduce a separate authenticated-principal abstraction.

## Providers and factories

The current implementation project participates in the repository-wide `IFactoryByUrl<T>` architecture. It contains factories for both
`IFactoryByUrl<ISignIn>` and `IFactoryByUrl<IMembership>`, selected by protocol-bearing URLs. The assembly marker allows applications to include the
project when explicitly scanning assemblies for factories.

```text
configuration
     |
     v
provider URL
     |
     v
IFactoryByUrl<T>
     |
     +--> ISignIn
     |
     +--> IMembership
```

Factories are a configuration and composition mechanism: they select and construct an implementation, but they do not add semantics to `ISignIn`
or `IMembership`. Applications still choose which provider assemblies to register. See [Factories](../factories/index.md) for protocol dispatch,
assembly scanning, and URL compatibility boundaries.

## Current implementations

`DProjects.Identity` currently provides three simple/reference implementations that exercise the contracts and composition model:

- `SignInNull` always returns `Failure` and is selected by the `null` sign-in protocol.
- `SignInStatic` demonstrates a staged login/password exchange through `DataRequired`, then returns either `Failure` or a claims principal. Its
  factory uses the `static` protocol.
- `MembershipFsDir` stores membership users and roles as JSON files through an injected `IFilesystem`; its membership factory uses the `fs` protocol.

These implementations demonstrate the abstraction boundaries and URL-selected composition. They are not the complete set of intended real-world
identity mechanisms.

## Evolution and extension points

The intended extension path is conservative:

- keep authentication-specific behavior behind `ISignIn` and membership-specific behavior behind `IMembership`;
- converge authenticated identities on `ClaimsPrincipal` for application use;
- add implementations through the existing factory and assembly-registration model where URL selection is appropriate;
- introduce smaller capabilities only when concrete providers or consumers demonstrate the need;
- allow real-world authentication and membership providers to drive changes to the evolving contracts and shared models.

The architecture deliberately leaves open how richer challenges, provider-specific state, partial membership capabilities, and different forms of
authentication material should be represented. Those areas are extension boundaries, not promises of current support.

## Current status and limitations

Identity is architectural exploration with a coherent foundation, not a finished identity framework. The sign-in conversation, membership split,
claims boundary, and factory integration establish the current direction, but public contracts and data representations may change as the subsystem
encounters additional use cases.

In particular, clients should treat the membership credential/token/key model and the presentation metadata carried by `SignInField` as deliberately
unstable. OAuth/OIDC, external providers, MFA, certificates, passkeys, and similar mechanisms discussed above are possible directions only; the
current source implements static and null sign-in plus filesystem-directory membership.

Return to the [documentation index](../index.md), review the repository-wide [architecture](../architecture.md), or read the
[factory architecture](../factories/index.md).
