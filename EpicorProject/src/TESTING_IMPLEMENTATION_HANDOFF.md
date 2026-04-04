# Testing Implementation Handoff

This document provides complete context for an agent (or developer) resuming the
implementation of dev-time unit and integration tests for the `DownloadAssemblies`
and `GetAvailableReferences` Epicor Functions.

---

## Current Status

**Branch:** `copilot/create-implementation-plan-for-tests`  
**Last commit:** merge from main — adds `Ice.Customization.Sandbox.dll`, `Erp.Data.Model.dll`, `library.db.cs`, `library.db.interface.cs`

**Completed work:**
- Phase 0 (DLL investigation) — fully done; findings below
- Phase 1 (test project) — `EpicorProject.Tests/EpicorProject.Tests.csproj` created
- Phase 2B (partial) — `IServiceCallProvider` interface and `CallService<T>` shadow in `function.base.cs`; `InternalsVisibleTo` on main project
- Pre-existing build bug fixed — `EnableDefaultCompileItems=false` + explicit `<Compile>` glob
- `Ice.Data.Model.dll` added from main → resolves `Ice.Tables` namespace ✅
- `Ice.Lib.Bpm.Shared.dll` added from main ✅
- `Ice.Customization.Sandbox.dll` added from main → resolves `Ice.Customization.Sandbox` ✅
- `Erp.Data.Model.dll` added from main → resolves `Erp.Tables`, `Erp.ErpContext` as types ✅
- `CallService<TService>` shadow constraint corrected to `where TService : class, IDisposable` ✅

**Still blocked on 1 remaining assembly gap.** 1 build error remains (see next section).

---

## Blocker: Remaining Missing `lib/` Assembly

### What has been resolved

| DLL | Namespace it provides | Status |
|---|---|---|
| `Ice.Data.Model.dll` | `Ice.Tables` | ✅ Added from main |
| `Ice.Lib.Bpm.Shared.dll` | `Ice.Lib.Bpm.*`, `Epicor.Customization.*` | ✅ Added from main |
| `Ice.Customization.Sandbox.dll` | `Ice.Customization.Sandbox` | ✅ Added from main |
| `Erp.Data.Model.dll` | `Erp.Tables`, `Erp.ErpContext` (entity types) | ✅ Added from main |

### Still missing (1 build error)

```
error CS1061: 'ErpContext' does not contain a definition for 'RollbackChangesAtReadOnlyTables'
   → library.db.cs:51
```

`Erp.Data.Model.dll` defines the EF entity types but **not** the runtime methods on `ErpContext`
(such as `RollbackChangesAtReadOnlyTables`). Those methods live in the full data-context
implementation assembly, typically named `Erp.Data.dll`.

| Method needed | Most likely DLL filename | Used in |
|---|---|---|
| `ErpContext.RollbackChangesAtReadOnlyTables()` | `Erp.Data.dll` | `library.db.cs:51` |

**Where to get it:** any machine with Epicor ERP server installed — look under
`<EpicorServer>/Server/bin/`. Must match version `5.1.100.0`.

**Validation:** after dropping it in, run:
```sh
dotnet build EpicorProject/EpicorProject.csproj
```
The build must produce **0 errors** before continuing with the phases below.

---

## Phase 0 Investigation Findings (Reference)

These facts were determined by runtime reflection against the DLLs in `lib/`.
They govern every architectural decision in Phases 2–5.

### `IFunctionHost` members (from `Epicor.Functions.Core.dll`)

```
EnvironmentType         { get; }          // property; return type in Ice.Lib.Bpm.Shared (unavailable locally)
CreateContext<TContext, TLibContext>(Func<TContext, bool, TLibContext>) → TLibContext
GetBpmContext()         → Epicor.Customization.Bpm.IBpmContext
GetFunctionAdapter(ref FunctionRefId)     → IFunctionAdapter
GetIceContext()         → (return type in Ice.Data.Model; unavailable locally)
```

`IFunctionHost` inherits `Epicor.Customization.Common.IHost`, which in turn adds:
```
Session                 { get; }          → Epicor.Hosting.ISession
BelongsToUserGroup(string) → bool
GetMailer(bool)         → Ice.Mail.ISmtpMailer
ResolveService<T>()     → T               // ← this is the service-resolution entry point
```

### Why Path B (seam) was chosen instead of Path A (mock host)

`IFunctionHost` references two assemblies that were originally unavailable locally:
- `Ice.Lib.Bpm.Shared` (needed for `EnvironmentType` return type)
- `Ice.Data.Model` (needed for `GetIceContext` return type)

Both DLLs have since been added to `lib/`. However, Path B (shadow `CallService<T>`
with an injectable `IServiceCallProvider`) remains the better approach because
it keeps test code simpler — the test project never needs to provide a full
`IFunctionHost` implementation with all its transitive dependencies.

### `FunctionBase<TDataContext, TInput, TOutput>` constructor

```csharp
// The only constructor used by all function implementations:
FunctionBase(IFunctionHost host)
FunctionBase(IFunctionHost host, IOperationLogger logger)
FunctionBase(IFunctionHost host, IOperationLogger logger, TemporarySessionFluentFactory factory)
```

### `FunctionBase.CallService<T>` IL

The 19-byte IL body was decoded. `CallService<T>`:
1. Reads the `host` field (stored at field token `0x0A000027`)
2. Reads the `logger` field (stored at field token `0x0A000028`)
3. Calls an internal method that goes through `IHost.ResolveService<T>()` on the host

**Conclusion:** `CallService<T>` is NOT host-independent; it uses `IHost.ResolveService<T>()`.
Path B (shadow the method) is still the cleanest approach regardless — it avoids needing
to wire a full `IFunctionHost` implementation in tests.

### `IFunctionRestHost` members

```
DefaultSerializer  { get; }    → Newtonsoft.Json.JsonSerializer
EnsureAuthorized(LibraryId, FunctionId)
PrepareCall(LibraryId, FunctionId)  → object
FinalizeCall(object, bool)
```

Not useful for unit testing (it is the server-side REST transport contract).

---

## Architecture Decisions (Already Implemented)

### `IServiceCallProvider` (in `EpicorProject/src/IServiceCallProvider.cs`)

```csharp
internal interface IServiceCallProvider
{
    void CallService<TService>(Action<TService> action);
}
```

### `Function<TInput, TOutput>` seam (in `EpicorProject/src/function.base.cs`)

```csharp
internal IServiceCallProvider? ServiceCallProvider { get; set; }

protected new void CallService<TService>(Action<TService> action)
{
    if (this.ServiceCallProvider != null)
        this.ServiceCallProvider.CallService(action);
    else
        base.CallService(action);  // production path unchanged
}
```

**How to use in tests:**
```csharp
var impl = new DownloadAssembliesImpl(host);
impl.ServiceCallProvider = new MockServiceCallProvider()
    .Register<EcfToolsSvcContract>(mock.Object);
```

The `new` keyword shadows (not overrides) the non-virtual `FunctionBase.CallService<T>`.
This is intentional and correct — the C# compiler dispatches to the shadow because all
call sites inside `Run()` are inside the derived `Function<,>` class, where the shadowing
method is in scope.

### `InternalsVisibleTo` (in `EpicorProject/EpicorProject.csproj`)

```xml
<AssemblyAttribute Include="System.Runtime.CompilerServices.InternalsVisibleTo">
  <_Parameter1>EpicorProject.Tests</_Parameter1>
</AssemblyAttribute>
```

This lets the test project set `ServiceCallProvider` and construct `*Impl` classes
(which are `internal sealed`).

---

## Remaining Phases

All phases below assume the missing DLLs have been added and `dotnet build` passes.

---

### Phase 3 — `MockServiceCallProvider`

**File:** `EpicorProject.Tests/src/Testing/MockServiceCallProvider.cs`

Implement `IServiceCallProvider` with a type-keyed registry:

```csharp
internal class MockServiceCallProvider : IServiceCallProvider
{
    private readonly Dictionary<Type, object> _registry = new();

    public MockServiceCallProvider Register<T>(T instance)
    {
        _registry[typeof(T)] = instance!;
        return this;
    }

    public void CallService<TService>(Action<TService> action)
    {
        if (_registry.TryGetValue(typeof(TService), out var svc))
            action((TService)svc);
        else
            throw new InvalidOperationException(
                $"No mock registered for {typeof(TService).Name}. " +
                "Call Register<T>() in test setup.");
    }
}
```

---

### Phase 4 — `LocalFunctionHost`

**File:** `EpicorProject.Tests/src/Testing/LocalFunctionHost.cs`

The test project cannot implement `IFunctionHost` directly (missing DLL members), but
it needs a host to pass to the `*Impl` constructors. Use `Castle.DynamicProxy` or
`Moq` to create a partial mock that stubs only the members the constructors touch:

```csharp
// Using Moq — the IFunctionHost constructor call does not call any interface
// members directly; it stores the reference. So a bare Mock<IFunctionHost>
// is sufficient for constructing the Impl classes.
internal static class LocalFunctionHost
{
    public static IFunctionHost Create() =>
        new Moq.Mock<IFunctionHost>().Object;

    // For cross-function call tests, also wire up GetFunctionAdapter:
    public static IFunctionHost CreateWithLibrary()
    {
        var mock = new Moq.Mock<IFunctionHost>();
        var lib = new LibraryImpl();

        mock.Setup(h => h.GetFunctionAdapter(It.IsAny<FunctionRefId>()))
            .Returns((FunctionRefId r) => lib.CreateAdapter(r.FunctionId, mock.Object));

        return mock.Object;
    }
}
```

> **Note:** `Moq.Mock<IFunctionHost>` will work only if none of the interface members
> that throw `TypeLoadException` (the two depending on the missing DLLs) are called
> during the test.  Constructing `*Impl` only stores the host reference; it never calls
> `EnvironmentType` or `GetIceContext`, so Moq works fine.

---

### Phase 5 — REST Fallback (Integration Tests)

**File:** `EpicorProject.Tests/src/Testing/EpicorRestServiceFactory.cs`

Reads `Epicor:ApiUrl` and `Epicor:ApiKey` from `appsettings.json` and provides thin
REST clients.  When `ApiUrl` is empty the helper returns `null` and integration tests
must call `Skip.If(host == null, "ApiUrl not configured")`.

Key contracts to implement:

#### `Ice.Contracts.BpMethodSvcContract` (used in `GetAvailableReferences.cs`)

The only method called is:
```csharp
List<ReferenceInfo> GetAvailableReferences(string typeFilter)
```
where `ReferenceInfo` is in `Ice.Contracts.BO.BpMethod.ReferenceInfo`.

Epicor REST endpoint pattern:
```
GET {ApiUrl}/api/v2/efx/{Company}/Ice/BO/BpMethod/GetAvailableReferences?typeFilter=Assemblies
Authorization: Bearer {ApiKey}
```

#### `Ice.Contracts.EcfToolsSvcContract` (used in `DownloadAssemblies.cs`)

The only method called is:
```csharp
byte[] GetAssemblyBytes(string assemblyName)
```

Epicor REST endpoint pattern:
```
POST {ApiUrl}/api/v2/efx/{Company}/Ice/Lib/EcfTools/GetAssemblyBytes
Body: { "assemblyName": "..." }
Authorization: Bearer {ApiKey}
```

---

### Phase 6 — Write the Test Classes

#### `EpicorProject.Tests/src/GetAvailableReferencesTests.cs`

```csharp
using EFx.References.Implementation;
using Moq;

public class GetAvailableReferencesTests
{
    private static GetAvailableReferencesImpl BuildImpl(IServiceCallProvider scp)
    {
        var host = LocalFunctionHost.Create();
        var impl = new GetAvailableReferencesImpl(host);
        impl.ServiceCallProvider = scp;
        return impl;
    }

    [Fact]
    public void Run_ReturnsTwoAssemblies_WhenBpMethodReturnsTwo()
    {
        var mock = new Mock<Ice.Contracts.BpMethodSvcContract>();
        mock.Setup(m => m.GetAvailableReferences("Assemblies"))
            .Returns(new List<ReferenceInfo>
            {
                new() { Name = "Assembly.A", FileName = "Assembly.A.dll", Version = "1.0" },
                new() { Name = "Assembly.B", FileName = "Assembly.B.dll", Version = "2.0" },
            });

        var scp = new MockServiceCallProvider().Register(mock.Object);
        var impl = BuildImpl(scp);

        var output = impl.Run(new Epicor.Functions.FunctionInput());

        // Epicor.System is always hardcoded + 2 from mock = 3 rows
        output.Assemblies.Tables["Assemblies"].Rows.Should().HaveCount(3);
    }

    [Fact]
    public void Run_SetsSuccessTrue_WhenNoExceptions()
    {
        var mock = new Mock<Ice.Contracts.BpMethodSvcContract>();
        mock.Setup(m => m.GetAvailableReferences(It.IsAny<string>()))
            .Returns(new List<ReferenceInfo>());

        var impl = BuildImpl(new MockServiceCallProvider().Register(mock.Object));
        var output = impl.Run(new Epicor.Functions.FunctionInput());

        output.Success.Should().BeTrue();
    }

    [Fact]
    public void Run_SetsSuccessFalse_PopulatesMessage_WhenServiceThrows()
    {
        var mock = new Mock<Ice.Contracts.BpMethodSvcContract>();
        mock.Setup(m => m.GetAvailableReferences(It.IsAny<string>()))
            .Throws(new InvalidOperationException("simulated failure"));

        var impl = BuildImpl(new MockServiceCallProvider().Register(mock.Object));
        var output = impl.Run(new Epicor.Functions.FunctionInput());

        output.Success.Should().BeFalse();
        output.Message.Should().Contain("simulated failure");
    }

    [Fact]
    public void Run_AlwaysIncludesEpicorSystemRow()
    {
        var mock = new Mock<Ice.Contracts.BpMethodSvcContract>();
        mock.Setup(m => m.GetAvailableReferences(It.IsAny<string>()))
            .Returns(new List<ReferenceInfo>());

        var impl = BuildImpl(new MockServiceCallProvider().Register(mock.Object));
        var output = impl.Run(new Epicor.Functions.FunctionInput());

        var rows = output.Assemblies.Tables["Assemblies"].AsEnumerable();
        rows.Any(r => r.Field<string>("AssemblyName") == "Epicor.System").Should().BeTrue();
    }
}
```

#### `EpicorProject.Tests/src/DownloadAssembliesTests.cs`

```csharp
using EFx.References.Implementation;
using Moq;
using System.Data;

public class DownloadAssembliesTests
{
    private static DownloadAssembliesImpl BuildImpl(IServiceCallProvider scp)
    {
        var host = LocalFunctionHost.Create();
        var impl = new DownloadAssembliesImpl(host);
        impl.ServiceCallProvider = scp;
        return impl;
    }

    private static DownloadAssembliesInput MakeInput(params (string name, string file)[] rows)
    {
        var ds = new DataSet();
        var t = ds.Tables.Add("Assemblies");
        t.Columns.Add("AssemblyName", typeof(string));
        t.Columns.Add("FileName", typeof(string));
        foreach (var (name, file) in rows)
            t.Rows.Add(name, file);
        return new DownloadAssembliesInput { Assemblies = ds };
    }

    [Fact]
    public void Run_ReturnsZipBase64_WhenAssembliesProvided()
    {
        var mock = new Mock<Ice.Contracts.EcfToolsSvcContract>();
        mock.Setup(m => m.GetAssemblyBytes("Assembly.A"))
            .Returns(new byte[] { 1, 2, 3 });

        var input = MakeInput(("Assembly.A", "Assembly.A.dll"));
        var impl = BuildImpl(new MockServiceCallProvider().Register(mock.Object));
        var output = impl.Run(input);

        output.ZipBase64.Should().NotBeNullOrEmpty();
        Convert.FromBase64String(output.ZipBase64).Should().NotBeEmpty();
    }

    [Fact]
    public void Run_SetsSuccessTrue_WhenZipSucceeds()
    {
        var mock = new Mock<Ice.Contracts.EcfToolsSvcContract>();
        mock.Setup(m => m.GetAssemblyBytes(It.IsAny<string>()))
            .Returns(new byte[] { 0xFF });

        var input = MakeInput(("A", "A.dll"));
        var impl = BuildImpl(new MockServiceCallProvider().Register(mock.Object));
        var output = impl.Run(input);

        output.Success.Should().BeTrue();
    }

    [Fact]
    public void Run_ExitsEarly_WhenAssembliesTableIsEmpty()
    {
        // CallService must NOT be called when the table is empty
        var mock = new Mock<Ice.Contracts.EcfToolsSvcContract>(MockBehavior.Strict);
        // No setups → any call throws

        var input = MakeInput(); // empty table
        var impl = BuildImpl(new MockServiceCallProvider().Register(mock.Object));
        var output = impl.Run(input);

        // ZipBase64 is null (early return before assignment)
        output.ZipBase64.Should().BeNullOrEmpty();
    }

    [Fact]
    public void Run_SetsSuccessFalse_PopulatesMessage_WhenEcfToolsThrows()
    {
        var mock = new Mock<Ice.Contracts.EcfToolsSvcContract>();
        mock.Setup(m => m.GetAssemblyBytes(It.IsAny<string>()))
            .Throws(new InvalidOperationException("ecf failure"));

        var input = MakeInput(("A", "A.dll"));
        var impl = BuildImpl(new MockServiceCallProvider().Register(mock.Object));
        var output = impl.Run(input);

        output.Success.Should().BeFalse();
        output.Message.Should().Contain("ecf failure");
    }
}
```

#### `EpicorProject.Tests/src/CrossFunctionCallTests.cs`

```csharp
using EFx.References.Implementation;
using Moq;

public class CrossFunctionCallTests
{
    [Fact]
    public void GetAvailableReferences_ThenDownloadAssemblies_EndToEnd()
    {
        // Build a host that routes GetFunctionAdapter back to LibraryImpl
        var host = LocalFunctionHost.CreateWithLibrary();

        // Mock BpMethod for GetAvailableReferences
        var bpMock = new Mock<Ice.Contracts.BpMethodSvcContract>();
        bpMock.Setup(m => m.GetAvailableReferences("Assemblies"))
            .Returns(new List<ReferenceInfo>
            {
                new() { Name = "Epicor.Ice", FileName = "Epicor.Ice.dll", Version = "5.1" }
            });

        // Mock EcfTools for DownloadAssemblies
        var ecfMock = new Mock<Ice.Contracts.EcfToolsSvcContract>();
        ecfMock.Setup(m => m.GetAssemblyBytes(It.IsAny<string>()))
            .Returns(new byte[] { 1, 2, 3 });
        ecfMock.Setup(m => m.GetAssemblyBytes("Epicor.System"))
            .Returns(new byte[] { 4, 5, 6 });

        var scp = new MockServiceCallProvider()
            .Register(bpMock.Object)
            .Register(ecfMock.Object);

        // Step 1: GetAvailableReferences
        var garImpl = new GetAvailableReferencesImpl(host);
        garImpl.ServiceCallProvider = scp;
        var garOutput = garImpl.Run(new Epicor.Functions.FunctionInput());
        garOutput.Success.Should().BeTrue();

        // Step 2: DownloadAssemblies using the DataSet from step 1
        var daImpl = new DownloadAssembliesImpl(host);
        daImpl.ServiceCallProvider = scp;
        var daOutput = daImpl.Run(new DownloadAssembliesInput { Assemblies = garOutput.Assemblies });
        daOutput.Success.Should().BeTrue();
        daOutput.ZipBase64.Should().NotBeNullOrEmpty();
    }
}
```

#### Integration test stubs

**File:** `EpicorProject.Tests/src/IntegrationTests.cs`

```csharp
public class IntegrationTests
{
    // Set Epicor:ApiUrl and Epicor:ApiKey in EpicorProject/appsettings.json to enable.
    private static string? ApiUrl =>
        new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true)
            .Build()["Epicor:ApiUrl"];

    [Fact]
    public void GetAvailableReferences_WithRealEpicor_ReturnsNonEmptyAssemblyList()
    {
        Skip.If(string.IsNullOrEmpty(ApiUrl), "Epicor:ApiUrl not configured");
        // TODO: wire EpicorRestServiceFactory → scp → GetAvailableReferencesImpl
        throw new NotImplementedException();
    }

    [Fact]
    public void DownloadAssemblies_WithRealEpicor_ReturnsValidZip()
    {
        Skip.If(string.IsNullOrEmpty(ApiUrl), "Epicor:ApiUrl not configured");
        // TODO: wire EpicorRestServiceFactory → scp → DownloadAssembliesImpl
        throw new NotImplementedException();
    }
}
```

---

### Phase 7 — Wire Tests into Dev Workflow

#### 1. Add test project to solution

```sh
dotnet sln EpicorProject.slnx add EpicorProject.Tests/EpicorProject.Tests.csproj
```

#### 2. Create `run-tests.sh` in repo root

```bash
#!/usr/bin/env bash
set -euo pipefail
dotnet test EpicorProject.Tests/EpicorProject.Tests.csproj --logger "console;verbosity=normal"
```

Make it executable: `chmod +x run-tests.sh`

#### 3. Add VS Code task to `.devcontainer/devcontainer.json`

In the `customizations.vscode` block, add a `tasks` entry (VS Code tasks live in
`.vscode/tasks.json` — create that file):

**`.vscode/tasks.json`**
```json
{
  "version": "2.0.0",
  "tasks": [
    {
      "label": "Run Tests",
      "type": "shell",
      "command": "dotnet test EpicorProject.Tests/EpicorProject.Tests.csproj --logger console;verbosity=normal",
      "group": { "kind": "test", "isDefault": true },
      "presentation": { "reveal": "always" },
      "problemMatcher": "$msCompile"
    }
  ]
}
```

#### 4. Update `devcontainer.json` `postCreateCommand`

```json
"postCreateCommand": "dotnet restore && dotnet build && dotnet test EpicorProject.Tests/EpicorProject.Tests.csproj --no-build || echo 'Tests completed'"
```

---

## Key Type Reference

| Type | Assembly | Used in tests for |
|---|---|---|
| `Ice.Contracts.BpMethodSvcContract` | `Ice.Contracts.BO.BpMethod.dll` | Mock for `GetAvailableReferences.Run()` |
| `Ice.Contracts.EcfToolsSvcContract` | `Ice.Contracts.Lib.EcfTools.dll` | Mock for `DownloadAssemblies.Run()` |
| `Ice.Contracts.BO.BpMethod.ReferenceInfo` | `Ice.Contracts.BO.BpMethod.dll` | Return type of `BpMethodSvcContract.GetAvailableReferences` |
| `Epicor.Functions.FunctionInput` | `Epicor.Functions.Core.dll` | No-arg input type for `GetAvailableReferences` |
| `Epicor.Functions.IFunctionHost` | `Epicor.Functions.Core.dll` | Constructor arg for all `*Impl` classes |
| `Epicor.Functions.IFunctionAdapter` | `Epicor.Functions.Core.dll` | Returned by `LibraryImpl.CreateAdapter` |
| `Epicor.Functions.LibraryId` | `Epicor.Functions.Core.dll` | Used in `LocalFunctionHost.CreateWithLibrary` |
| `Epicor.Functions.FunctionRefId` | `Epicor.Functions.Core.dll` | Used in `GetFunctionAdapter` mock setup |
| `DownloadAssembliesImpl` | main project (internal) | Requires `InternalsVisibleTo` |
| `GetAvailableReferencesImpl` | main project (internal) | Requires `InternalsVisibleTo` |

---

## File Layout (Target State)

```
EpicorProject/
  lib/
    ... (existing DLLs)
    Ice.Data.Model.dll              ← ✅ ADDED (resolves Ice.Tables)
    Ice.Lib.Bpm.Shared.dll          ← ✅ ADDED
    Ice.Customization.Sandbox.dll   ← ✅ ADDED (resolves Ice.Customization.Sandbox)
    Erp.Data.Model.dll              ← ✅ ADDED (resolves Erp.Tables, Erp.ErpContext types)
    Erp.Data.dll                    ← ❌ STILL NEEDED (ErpContext.RollbackChangesAtReadOnlyTables in library.db.cs)
  src/
    function.base.cs                ← MODIFIED (seam + Db/Dispose; constraint fixed)
    IServiceCallProvider.cs         ← NEW (seam interface; constraint fixed)
    library.db.cs                   ← NEW (from main; blocked on Erp.Data.dll)
    library.db.interface.cs         ← NEW (from main)
    ... (all other src files unchanged)

EpicorProject.Tests/
  EpicorProject.Tests.csproj        ← ALREADY CREATED
  src/
    Testing/
      MockServiceCallProvider.cs    ← TODO (Phase 3)
      LocalFunctionHost.cs          ← TODO (Phase 4)
      EpicorRestServiceFactory.cs   ← TODO (Phase 5, integration only)
    GetAvailableReferencesTests.cs  ← TODO (Phase 6)
    DownloadAssembliesTests.cs      ← TODO (Phase 6)
    CrossFunctionCallTests.cs       ← TODO (Phase 6)
    IntegrationTests.cs             ← TODO (Phase 6)

.vscode/
  tasks.json                        ← TODO (Phase 7)

run-tests.sh                        ← TODO (Phase 7)
```

---

## Quick Checklist for Resuming Agent

1. [x] `Ice.Data.Model.dll` added to `lib/` — resolves `Ice.Tables`
2. [x] `Ice.Lib.Bpm.Shared.dll` added to `lib/`
3. [x] `Ice.Customization.Sandbox.dll` added to `lib/` — resolves `Ice.Customization.Sandbox`
4. [x] `Erp.Data.Model.dll` added to `lib/` — resolves `Erp.Tables`, `Erp.ErpContext` types
5. [x] `CallService<TService>` shadow constraint corrected to `where TService : class, IDisposable`
6. [ ] Add `Erp.Data.dll` to `lib/` — resolves `ErpContext.RollbackChangesAtReadOnlyTables()` in `library.db.cs`
7. [ ] `dotnet build EpicorProject/EpicorProject.csproj` → 0 errors
8. [ ] Create `EpicorProject.Tests/src/Testing/MockServiceCallProvider.cs` (Phase 3)
9. [ ] Create `EpicorProject.Tests/src/Testing/LocalFunctionHost.cs` (Phase 4)
10. [ ] `dotnet build EpicorProject.Tests/EpicorProject.Tests.csproj` → 0 errors
11. [ ] Create `GetAvailableReferencesTests.cs` (Phase 6)
12. [ ] Create `DownloadAssembliesTests.cs` (Phase 6)
13. [ ] Create `CrossFunctionCallTests.cs` (Phase 6)
14. [ ] Create `IntegrationTests.cs` stub (Phase 6)
15. [ ] `dotnet test EpicorProject.Tests` → all unit tests pass; integration tests skip
16. [ ] (Optional) Create `EpicorRestServiceFactory.cs` (Phase 5 — REST fallback)
17. [ ] `dotnet sln EpicorProject.slnx add EpicorProject.Tests/EpicorProject.Tests.csproj`
18. [ ] Create `run-tests.sh` and `.vscode/tasks.json` (Phase 7)
19. [ ] Update `devcontainer.json` `postCreateCommand` (Phase 7)
