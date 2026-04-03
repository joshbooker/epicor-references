# Function Host Investigation

This note summarizes whether a function can create its own `IFunctionHost` instance inside `Run()` in this project.

## Summary

Short answer: not from the assemblies currently present in this repository.

What we found:

1. The generated function code expects a host to be injected by the runtime.
2. `Epicor.Functions.Core.dll` exposes `IFunctionHost`, but the assembly scan did not reveal a public concrete host type that can be instantiated from user code.
3. A broader scan across the `lib` folder did not find a public concrete implementation of `Epicor.Functions.IFunctionHost`.
4. A host-like type was found in `Epicor.System.dll` (`Ice.Hosting.IceDbServerHost`), but it is non-public and therefore not constructible from normal function code.

Practical conclusion:

- You should assume the host is runtime-owned and injected.
- You should not plan on calling `new` on a function host inside `Run()`.
- If you need host behavior inside function code, the correct approach is to use the injected runtime context or expose the injected host from the base class.

## Evidence from source in this repo

The generated function implementations accept `IFunctionHost` through their constructors and pass it to the shared function base:

- `DownloadAssembliesImpl(Epicor.Functions.IFunctionHost host)`
- `GetAvailableReferencesImpl(Epicor.Functions.IFunctionHost host)`

That pattern appears in:

- `DownloadAssemblies.designer.cs`
- `GetAvailableReferences.designer.cs`
- `function.base.cs`

The base class does not construct a host. It only receives one and uses it to initialize `ThisLib`:

```csharp
protected Function(Epicor.Functions.IFunctionHost host)
    : base(host)
{
    this.thisLibraryLazy = new System.Lazy<ReferencesLibraryProxy>(
        () => new ReferencesLibraryProxy(host));
}
```

This shows the codegen/runtime contract is injection, not local host construction.

## Evidence from lib inspection

The following assembly exists in the project:

- `lib/Epicor.Functions.Core.dll`

Inspection of that assembly confirmed the presence of these public interface types:

- `Epicor.Functions.IFunctionHost`
- `Epicor.Functions.IFunctionRestHost`

No public concrete `FunctionHost` type was surfaced from `Epicor.Functions.Core.dll`.

## Broader scan across lib

A metadata scan across the DLLs in `lib` looked for:

1. Concrete types implementing `Epicor.Functions.IFunctionHost`
2. Concrete types with `Host` in the type name

Results:

- No public concrete implementation of `Epicor.Functions.IFunctionHost` was found.
- A host-related concrete type was discovered in `Epicor.System.dll`:
  - `Ice.Hosting.IceDbServerHost`
- That type is non-public.

This is the critical point: even if Epicor has an internal runtime host implementation, nothing in the current repo exposes a public constructible host class for your function code to use.

## What this means inside Run()

Inside `Run()`, you can reasonably rely on:

- inherited runtime behavior from the Epicor function base class
- helper patterns such as `CallService(...)`
- `ThisLib` for cross-function calls in the same library

Inside `Run()`, you should not assume you can do this:

```csharp
var host = new SomeEpicorFunctionHost(...);
```

There is no public type in the current repo that supports that pattern.

## Limits of the investigation

This conclusion is strong for the contents of this repository, but it is not a claim about every assembly in a full Epicor server installation.

Some assemblies in `lib` could not be fully traversed during metadata inspection because additional transitive dependencies were not present in this workspace. That means a full server environment may contain more internal types than this repo shows.

Even with that limitation, the practical answer for this project remains the same:

- there is no visible public constructible `IFunctionHost` implementation in the local `lib` set
- the supported model is runtime injection, not creating a second host in function code

## Recommended approach

If the real requirement is "I need host functionality inside function code," use one of these options:

1. Expose the already-injected host from `function.base.cs` as a protected property.
2. Continue using higher-level inherited APIs such as `CallService(...)`.
3. Use `ThisLib` for in-library function-to-function calls.

If direct host access is needed often, option 1 is the cleanest extension point in this codebase.