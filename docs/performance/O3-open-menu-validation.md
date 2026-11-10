# Open-menu performance validation and dynamic-page correction

The follow-up compares the historical installed 1.4.1 binary with 1.4.1-perf.2
using CSSProfiler and real connected CS2 clients. Numeric results and the tested
client count are recorded after the run; synthetic renderer timings are not
substituted for the complete `OnTick` handler.

## Reproduced defect

With ten options, move to the last option so the displayed page starts after the
first row. Remove all options except the first while the menu remains open.
Previously `selected` and `offset` retained their old values: `GetText()` returned
the title and footer without the remaining option. Reducing `MenuLinesCount`
could similarly leave the selected row outside the displayed page. Inheriting a
page offset from an older menu could hide the first selected row.

`PlayerInfo.GetText()` now normalizes selection and page offset when the option
count or configured line count changes. Empty lists reset both to zero; a
nonempty list keeps the selected row within the list and visible page. The
normalization precedes cache validation, so the next rendered HTML uses the
correct page. Unchanged menus retain the cached rendering path.

Four regression checks reproduce shrinking a later page, clearing the menu,
reducing its line count and inheriting an incompatible offset. The original
seventeen checks also pass. Public interfaces and configuration formats are
unchanged.

## Measurement safeguards

The diagnostic resolves `_api` from the actual **Loaded** MenuManager plugin
context. CSS capability providers retain previous suppliers across hot reloads,
so resolving only the `menu:nfcore` capability can accidentally open a menu in
an unloaded core instance. Every accepted window checks the loaded core version
and MVID, live menu count, selected row/page, connected client identity and pawn.
The historical binary must have no `cachedText` field; the candidate must have
that field and a populated cache. Setup attempts failing these checks are
excluded. No tested DLL is patched to permit bots or bypass native HTML sends.

The helper explicitly removes only its own menu during fixture cleanup because
the historical core can retain a closed menu until buttons change. It restores
the captured movement modifier only on the original pawn. This fixture cleanup
is outside the measured windows and does not change either renderer.

Hot reloading the core can leave dependent plugins holding old API instances.
A final scoped cold start after testing is needed to restore consistent API
bindings before considering the server fully returned to service.
