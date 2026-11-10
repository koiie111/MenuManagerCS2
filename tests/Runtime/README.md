# O3 production callbacks

Run `dotnet run --project tests/Runtime/Runtime.csproj -c Release`.

The project compiles the actual Main, Control, PlayerInfo, ButtonMenu, ButtonsConfig,
Misc, MenuInstance, CMenuApi and public API sources. CSS entity properties, transport,
event registration, capabilities, storage and native Metamod exports are managed
boundary doubles. It exercises the registered map callbacks and the actual tick loop;
it does not prove native ABI, client rendering, engine callback order or compatibility
with live VIP/SimpleAdmin/Emotes. Unavailable Metamod fallback and routing to CSS
menus are covered; a loaded native Metamod implementation remains a live check.

The config-unlock test failed on the original PR head with
`config unlock restores movement`. The corrected suite checks 0/1/8/32 active menus,
per-tick send/cache reuse, held/repressed/custom/combined buttons, callback replacement
and collection clearing, disabled/empty/dynamic menus, movement lock ownership,
respawn/pawn serial and controller reuse, disconnect, map and unload cleanup.

For the identical production tick harness:

```powershell
$env:DOTNET_TieredCompilation='0'
dotnet run --project tests/Runtime/Runtime.csproj -c Release -p:ProductionRoot=E:/path/to/baseline/ -- --benchmark
dotnet run --project tests/Runtime/Runtime.csproj -c Release -- --benchmark
```

Use an upstream `5d2892a` checkout for baseline and the reviewed tree for candidate.
Run sequentially: both builds share this test project's output directory. Each fixture
has 0/1/8/32 menus, 32 warmup ticks and 3 x 128 measured ticks. Dynamic labels change
every 64 ticks. Native sends count calls and retain the last HTML; allocations include
managed boundaries, mutations and the production renderer/ColorText. Verify identical
send counts and final HTML hashes. These are synthetic managed measurements, not
native calls or full server ticks. Existing O3 renderer tests use a simpler ColorText
double; their numbers must not be mixed with this harness.
