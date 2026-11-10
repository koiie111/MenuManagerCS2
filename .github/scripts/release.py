"""Resolve stable CSS versions and package server files without bundling CSS itself."""
import hashlib
import json
import os
from pathlib import Path
import re
import sys
from urllib.request import urlopen
from zipfile import ZipFile, ZIP_DEFLATED


def resolve():
    with urlopen('https://api.nuget.org/v3-flatcontainer/counterstrikesharp.api/index.json', timeout=60) as response:
        versions = json.load(response)['versions']
    stable = [v for v in versions if re.fullmatch(r'\d+\.\d+\.\d+', v)]
    version = max(stable, key=lambda v: tuple(map(int, v.split('.'))))
    sha = os.environ['GITHUB_SHA']
    tag = f'build-{sha}-css-{version}'
    with open(os.environ['GITHUB_OUTPUT'], 'a', encoding='utf-8') as output:
        output.write(f'css={version}\ntag={tag}\n')


def package():
    version, sha = sys.argv[2:4]
    root = Path('dist')
    root.mkdir(exist_ok=True)
    archive = root / 'MenuManagerCS2.zip'
    source = Path('MenuManagerCore/bin/Release/net10.0')
    # Explicit allowlist: do not distribute the host API, optional PlayerSettings
    # provider, or the example plugin as private copies in the core directory.
    files = {
        'MenuManagerCore.dll': 'plugins/MenuManagerCore/MenuManagerCore.dll',
        'MenuManagerCore.deps.json': 'plugins/MenuManagerCore/MenuManagerCore.deps.json',
        'MenuManagerCore.pdb': 'plugins/MenuManagerCore/MenuManagerCore.pdb',
        'MenuManagerApi.dll': 'shared/MenuManagerApi/MenuManagerApi.dll',
    }
    manifest = {'commit': sha, 'counterstrikesharp_api': version, 'sha256': {}}
    with ZipFile(archive, 'w', ZIP_DEFLATED) as output:
        for name, destination in files.items():
            data = (source / name).read_bytes()
            target = f'addons/counterstrikesharp/{destination}'
            output.writestr(target, data)
            manifest['sha256'][target] = hashlib.sha256(data).hexdigest()
        output.writestr('build-manifest.json', json.dumps(manifest, indent=2) + '\n')
        output.write('UPGRADE.md', 'UPGRADE.md')
    (root / 'SHA256SUMS.txt').write_text(
        f'{hashlib.sha256(archive.read_bytes()).hexdigest()}  {archive.name}\n', encoding='utf-8')


if __name__ == '__main__':
    {'resolve': resolve, 'package': package}[sys.argv[1]]()
