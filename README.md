# Info

## Automated releases

Every push to `main` builds and publishes a ZIP on the Releases page after all
checks pass. An hourly check (minute 17 UTC) resolves the latest stable
`CounterStrikeSharp.API` from NuGet and rebuilds when that version changes.
Prerelease packages are excluded. GitHub may delay scheduled runs or disable
schedules after 60 days of repository inactivity; re-enable the workflow in
Actions when needed. `Build and release` also supports manual runs.

Releases are identified by the full source commit and CSS package version;
already published combinations are skipped. Failed builds publish nothing and
are retried by the next hourly check. No additional secrets are required:
publication uses `GITHUB_TOKEN` with `contents: write`.

The projects now target .NET 10 because CSS API 1.0.379 requires it. Local builds
use the version in `Directory.Build.props`; CI overrides it with the latest stable
version. Install a compatible CounterStrikeSharp runtime before installing these
releases. Future breaking API changes may require source changes before CI passes.

Extract `MenuManagerCS2.zip` into the game directory. It contains the core plugin,
the shared MenuManager API, build metadata and upgrade notes. The CSS runtime,
PlayerSettings provider, configs and example plugin are not bundled. Existing
config and player preferences are preserved. Keep consumers of the shared API
compatible and follow `UPGRADE.md` when updating an existing server.

CI runs the O3 and runtime regression suites. Archived performance measurements
are verified against their recorded source commit; they do not certify newer
CSS runtimes or releases. The ZIP includes a manifest with file hashes and the
release includes a SHA256 checksum for the archive.

MenuManager main repository

CSDevs topic: https://csdevs.net/resources/menumanager.726/ (There is all info about buttons, config, demo and etc.)


MenuManagerCore - main core plugin

MenuManagerApi - api lib project

MenuManagerText - just an example how to use this plugin

# Disclaimer

Very often i get questions about move blocking method while MenuWhileOpenMenu is set to false and get requests to change it to modifying MoveType param from current. And every time i decline it. Why? Nothing hard

When player move blocked with modified MoveType he can abuse it to get advantage in match with some unusual positions (while jumping down from short to CTs on de_dust2 for example)

So method with setting VelocityModifier was taken as main one. Another method option could be added in future but i don't make a promise now...
