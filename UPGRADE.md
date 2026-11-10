# MenuManager 1.4.1-perf.3

## Automated builds after the .NET 10 migration

New automated releases target .NET 10 and CSS API 1.0.379 or newer. They require
a compatible CounterStrikeSharp runtime. The historical .NET 8 build and live
measurements described below apply only to the recorded artifacts. The ZIP now
includes a rebuilt shared MenuManagerApi; coordinate its update with consumers.
Do not install these builds on older .NET 8 CSS runtimes.

The O3 optimization and lifecycle corrections preserve the public MenuManagerApi,
PlayerSettingsApi, config format, button bindings, HTML send cadence and menu routing.
Build with `dotnet build MenuManagerCore/MenuManagerCore.csproj -c Release` using
.NET 8-compatible SDK tooling. The project still targets net8.0 and CSS API 1.0.305;
the previous live perf.2 test used CSS runtime 1.0.378.

The review release manifest identifies the exact local artifact. Rebuilds in another
checkout/commit may have different binary hashes. The perf.2 one-client measurement
does not certify perf.3 or 8/32-client behavior.

For a separately authorized deployment, back up the entire current core directory,
config, translations and compatible shared API hashes outside autoload. Preserve the
installed MenuManagerApi and PlayerSettingsApi; do not replace the CSS runtime or
player preferences with build output. Install only MenuManagerCore.dll and its matching
deps/PDB if used. Keep existing dependencies, translations and configuration.

Use a scoped cold stop/start of the selected instance after confirming its agreed test
window. Hot reload of the capability provider can leave consumers bound to an unloaded
core; the previous test required a cold start to restore coherent API bindings.
Check loaded module version/SHA, menu display/input/close, movement restoration,
respawn, reconnect, map change and competing overlays with actual clients.

On stale menus, stuck movement, native errors or a repeatable latency regression,
restore the backed-up compatible core/dependency/config set with the same scoped cold
cycle. Restore shared APIs/preferences only if that deployment changed them; this
package requires no such change. No deployment, restart or profiling is performed
by these instructions or by CI.
