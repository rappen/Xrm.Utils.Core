# Xrm.Utils.Core

Extensions and helpers that make Dataverse / Dynamics 365 / CRM code quicker to write: a small
"execution container" that carries the organization service and a logger, extension methods on
`Entity`, `EntityCollection` and the container, metadata lookups with caching, sectioned logging,
and a few serialization and query utilities.

It is used by [Xrm.Shuffle](https://github.com/rappen/Xrm.Shuffle) (the Shuffle tools for
XrmToolBox and their pipeline tasks), which is also where it is tested.

## How it is packaged

The code is organized as three **shared projects** (`.shproj` / `.projitems`), not as assemblies.
A project that imports one compiles its source files into its own assembly, so there is no
`Xrm.Utils.Core.dll` to deploy - and no version conflict when several tools in the same host
(XrmToolBox, a plugin sandbox) each carry their own copy.

| Project | Contents | Needs |
|---|---|---|
| **Xrm.Utils.Core.Common** | Everything below: container, extensions, fluent API, loggers, utilities. Usable in console apps, [XrmToolBox](https://www.xrmtoolbox.com/) tools, pipelines, plugins and workflows. | - |
| **Xrm.Utils.Core.Plugin** | `PluginBase` and `PluginContainer` for plugin development, plus message and parameter name constants. | Common |
| **Xrm.Utils.Core.Workflow** | `ActivityBase` and `ActivityContainer` for custom workflow activities. | Common |

The Plugin types live in Common's namespaces (`Xrm.Utils.Core.Common`, `.Interfaces`, `.Misc`,
`.Constants`, `.Extensions`); the Workflow types are in `Xrm.Utils.Core.Workflow`.

## Using it in a project

1. Add the repository as a git submodule, for example under `Xrm.Utils.Core`.
2. Import the shared project in your `.csproj`:

   ```xml
   <Import Project="..\Xrm.Utils.Core\Xrm.Utils.Core.Common\Xrm.Utils.Core.Common.projitems" Label="Shared" />
   ```

3. Reference what the code needs:
   - `Microsoft.CrmSdk.CoreAssemblies` (NuGet), for `Microsoft.Xrm.Sdk` and `Microsoft.Crm.Sdk.Proxy`
   - the framework assemblies `System.Runtime.Caching`, `System.Runtime.Serialization`,
     `System.ServiceModel`, `System.Data` and `System.Xml`

For Plugin or Workflow, import Common as well, and for Workflow also reference
`Microsoft.CrmSdk.Workflow` (NuGet) and `System.Activities`.

It builds for .NET Framework 4.6.2 and later; Xrm.Shuffle compiles Common for both 4.6.2 (its
pipeline tasks) and 4.8 (XrmToolBox).

Because the source is compiled into your project, a change here reaches you when you move the
submodule pointer - and only then.

## The execution container

Almost everything hangs off `IExecutionContainer`:

```csharp
public interface IExecutionContainer
{
    dynamic Values { get; }               // a property bag for the caller's own state
    ILoggable Logger { get; }
    IOrganizationService Service { get; }
}
```

Implement it once for your host - Shuffle's `CintContainer` wraps an `IOrganizationService` and a
`FileLogger` - and the extension methods in `Xrm.Utils.Core.Common.Extensions` become available:

```csharp
using Xrm.Utils.Core.Common.Extensions;

var accounts = container.RetrieveAll(new QueryExpression("account") { ColumnSet = new ColumnSet("name") });
container.Log($"Read {accounts.Entities.Count} accounts");
var status = account.AttributeAsString("statuscode", "<none>", true);
```

### In a plugin or workflow activity

`PluginBase` builds a `PluginContainer` from the service provider - with the plugin execution
context, an organization service for the context's user, a `CRMLogger` on the tracing service,
and the target and images as `Entities` - then calls `Validate` and, if that returns true,
`Execute`. An exception is logged and rethrown.

```csharp
using Microsoft.Xrm.Sdk;
using Xrm.Utils.Core.Common;
using Xrm.Utils.Core.Common.Constants;
using Xrm.Utils.Core.Common.Extensions;
using Xrm.Utils.Core.Common.Interfaces;

public class AccountPlugin : PluginBase
{
    public override bool Validate(IPluginExecutionContext context) =>
        context.PrimaryEntityName == "account" && context.MessageName == MessageName.Update;

    public override void Execute(IPluginExecutionContainer container)
    {
        container.Log($"Depth {container.Context.Depth}");
    }
}
```

`ActivityBase` wraps a custom workflow activity the same way, without the `Validate` step: it
builds an `ActivityContainer`, which also reads and writes the activity's in and out arguments,
and calls `Execute(ActivityContainer)`.

## What is in Common

### Container extensions (`Extensions/Container`)

| Area | Methods |
|---|---|
| Service calls | `Create`, `Update`, `Save`, `Delete`, `Retrieve`, `RetrieveMultiple`, `RetrieveAll`, `Associate`, `Disassociate`, `SetState`, `Reload`, `Ensure`, `Merge` |
| Metadata | `Execute(RetrieveEntityRequest)` and `Execute(RetrieveAttributeRequest)` (cached for 5 minutes), `GetActiveStates`, `GetCrmVersion` |
| Logging | `Log`, `StartSection`, `EndSection` - shorthands for `container.Logger` |
| Values | `AttributeAsBaseType` (unwraps `AliasedValue`, `EntityReference`, `OptionSetValue`, `Money` to their base values) |
| Serialization | `CreateEntityCollection`, `InitFromTextLine`, `Serialize`, `Deserialize`, `Convert` (QueryExpression to FetchXml) |

### Entity and collection extensions

- **`Entity`**: `GetAttribute<T>`, `SetAttribute`, `RemoveAttribute`, `Contains(name, notnull)`,
  `AttributeAsString`, `IsActive`, `CloneAttributes`, `CloneId`, `Merge`, `ToStringExt`,
  `GetPrimaryIdAttribute`, `GetPrimaryNameAttribute`, `GetRelated`, `GetRelating`,
  `GetAssociated`, `Assign`, `SetOwner`, `GrantAccessTo`, `GetAccessFor`, `RevokeAccessFrom`
- **`EntityCollection`**: `Add`, `AddRange`, `Contains`, `Count`, `Get`, `Sort`,
  `ToEntityReferenceCollection`, `Serialize`
- **`IServiceProvider`** (plugins): `GetOrganizationService`, `GetOrganizationServiceFactory`,
  `GetPluginExecutionContext`, `GetTracingService`

### Fluent API (`Fluent`)

`container.Entity(...)`, `container.Attribute(name)` and `container.Principal(...)` start chains
such as `container.Entity("account").PrimaryIdAttribute` or
`container.Principal(record).On(owner).Assign()`. For reading a value as text, prefer
`entity.AttributeAsString(name)`: the fluent `container.Attribute(name).On(entity).ToString()`
opens a log section on every call, which floods a log when it runs per record.

### Loggers (`Loggers`)

- **`FileLogger`** writes a plain-text log file. Each line starts with the milliseconds since the
  previous line (blank when no time passed) and is indented by section depth; sections are
  drawn as `↓ name` / `↑ name (ms)`.
- **`CRMLogger`** writes to a plugin's `ITracingService`.

### Utilities (`Misc`, `CsvHelper`)

`Query` (adding conditions and links to a `QueryExpression`), `FetchXML` (building FetchXml
documents), `ValueText` (culture-independent value text), `Serialization` and `XML`, `EntityComparer` and `SortAttribute` (sorting records by
attributes), `PPH` (populating placeholders from a record), `Constants`, and a small CSV
reader/writer.

## Things worth knowing

- **`RetrieveMultiple` returns one page; `RetrieveAll` returns everything.** Dataverse caps a
  page at 5000 records. `RetrieveAll` follows the paging cookie until the last page, for a
  `QueryExpression` and for a `FetchExpression` alike. A query with `TopCount`, or FetchXML with
  `top` or `aggregate`, is sent as it is, since those cannot be combined with paging; a FetchXML
  `count` is kept as the page size.
- **Values in data files do not depend on the machine's culture.** `ValueText` writes numbers in
  the invariant culture (`1234.5`) and dates as round-trip `O` (`2026-10-05T12:30:00.0000000Z`),
  and `SetAttribute` reads them back the same way, with dates returned as UTC. A number written
  the old way, in a comma-decimal culture, is still read on a machine with that culture, and is
  rejected with a clear error - never misread as a larger number - anywhere else. `Double`
  columns are supported alongside `Decimal` and `Money`.
- **Keep log sections balanced.** Every `StartSection` needs exactly one `EndSection`, or the
  rest of the log is nested wrong and timings are attributed to the wrong section. Where an
  exception can pass through and the caller carries on, end the section in a `finally`:

  ```csharp
  container.StartSection("ImportBlock");
  try
  {
      // ...
  }
  finally
  {
      container.EndSection();
  }
  ```

- **Metadata is cached.** The primary id and primary name attribute of a table are cached for
  the lifetime of the process, and `Execute(RetrieveEntityRequest)` responses for 5 minutes.
  A long-lived process does not see a metadata change until then.
- **`AttributeAsString` never queries the server.** It returns the label from
  `FormattedValues` when the record has one, otherwise a string of the value; an
  `EntityReference` without a name becomes `logicalname:id`. A format string applies to dates and
  numbers, and to text that parses as either.

## Testing

There is no test project in this repository. Common is tested through
[Xrm.Shuffle](https://github.com/rappen/Xrm.Shuffle)'s `tests/Xrm.Shuffle.Core.Tests`, which
imports it as a submodule and exercises it against a fake organization service that pages
results at 5000 the way Dataverse does. Nothing currently tests Plugin and Workflow beyond
compiling them.

## History

These helpers have evolved over 10-15 years: they started at Cinteros, continued at Innofactor,
and are no longer tied to any company - but they keep improving.
