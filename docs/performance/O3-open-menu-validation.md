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

## Accepted live measurement — 10 November 2026

Historical 1.4.1 → fixed 1.4.1-perf.2, server #6, **one real client and one
open ButtonMenu**, no bots. CSSProfiler 1.0.0 instrumented mode, three 30-second
windows per version/scenario (12 accepted windows). Units below are **ms per
complete MenuManager OnTick call**, including native HTML send and CSS dispatch.

| Scenario | Mean old → new | Mean reduction | p95 old → new | p99 old → new |
|---|---:|---:|---:|---:|
| Unchanged menu | 2.591349 → 0.175126 | 93.24% | 3.859733 → 0.238008 | 4.417740 → 0.255629 |
| Visible label updated at 1 Hz | 2.541225 → 0.212114 | 91.65% | 3.434560 → 0.237102 | 4.421681 → 2.580856 |

The 1 Hz scenario includes the expensive rebuild calls in its tail; they were
not filtered out. Means are weighted over all calls; quantiles use linear
interpolation of raw evented speedscope O/C intervals. The results cover this
handler and one client, not full-server speed or 8/32-client capacity. Allocations,
network RTT and client rendering cost were not measured. Other games on the
host continued running. Interrupted setup/input windows were excluded; completed
valid windows were retained when the series resumed. Observer checks were
discrete, approximately every 5–7 seconds. The client confirmed visible HTML,
then kept the console open to prevent navigation input during the later windows.

Live integration reproduced the old shrinking-page defect and verified the
fixed remaining option, empty-menu and line-count behavior. The unchanged
standard menu HTML matched the historical binary by SHA256. All 21 managed
checks and Release build passed. The fixed package remains on #6 only; original
config and shared APIs are retained. After the client left, temporary diagnostics
were removed and a scoped cold start restored consistent dependent API bindings.
No fleet rollout or merge was performed.

Fixed DLL SHA256: `d0c3cd4e6914d8427801c08950c40aaf1a5023dd3617101507291e29f112afcd`. Code commit: `98113f6`.

## Follow-up review

The final PR now includes offline-reviewed `1.4.1-perf.3` movement ownership,
controller/pawn serial guards and map cleanup. The figures above remain measurements
of perf.2. All 12 original raw profiles, an independent verifier, the new production
callback suite and release/canary limits are in
[review-20261110](review-20261110/README.md). Perf.3 has not been deployed in this review.
