"""Offline verification of all 12 original one-client perf.2 CSSProfiler windows."""
import hashlib
import json
import math
import statistics
import zipfile
from collections import defaultdict
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1] / "docs/performance/review-20261110"


def digest(data, text=False):
    return hashlib.sha256(data.replace(b"\r\n", b"\n") if text else data).hexdigest()


def describe(values):
    values = sorted(values)

    def percentile(p):
        x = (len(values) - 1) * p
        lo = int(x)
        hi = min(lo + 1, len(values) - 1)
        return values[lo] + (values[hi] - values[lo]) * (x - lo)

    return dict(calls=len(values), mean_us=statistics.mean(values) * 1000,
                median_us=statistics.median(values) * 1000, p95_us=percentile(.95) * 1000,
                p99_us=percentile(.99) * 1000, max_us=max(values) * 1000, total_ms=sum(values))


def equal(actual, expected):
    assert actual.keys() == expected.keys()
    for key in actual:
        assert math.isclose(actual[key], expected[key], rel_tol=1e-9, abs_tol=1e-7), (key, actual[key], expected[key])


def main():
    manifest = json.loads((ROOT / "live-perf2-sha256.json").read_text())
    expected = json.loads((ROOT / "live-perf2-analysis.json").read_text())
    windows = json.loads((ROOT / "live-perf2-windows.json").read_text())
    grouped = defaultdict(list)
    durations = {}
    for name in ["live-perf2-raw.zip", "live-perf2-analysis.json", "live-perf2-windows.json"]:
        assert digest((ROOT / name).read_bytes(), name.endswith(".json")) == manifest[name], name
    with zipfile.ZipFile(ROOT / "live-perf2-raw.zip") as archive:
        assert len(archive.namelist()) == len(set(archive.namelist())) == len(windows) == 12
        for window in windows:
            label = window["label"]
            name = label + ".speedscope.json"
            raw = archive.read(name)
            assert digest(raw) == manifest[name], name
            data = json.loads(raw)
            names = [f["name"] for f in data["shared"]["frames"]]
            values = []
            seconds = 0
            for profile in data["profiles"]:
                assert profile["type"] == "evented" and profile["unit"] == "milliseconds"
                stack = []
                previous = profile["startValue"]
                seconds += (profile["endValue"] - previous) / 1000
                for event in profile["events"]:
                    at = event["at"]
                    assert math.isfinite(at) and previous <= at <= profile["endValue"]
                    previous = at
                    assert 0 <= event["frame"] < len(names)
                    if event["type"] == "O":
                        stack.append(event)
                    else:
                        assert event["type"] == "C" and stack
                        start = stack.pop()
                        assert start["frame"] == event["frame"]
                        if names[event["frame"]] == "MenuManager [Core] | listener:OnTick":
                            values.append(at - start["at"])
                assert not stack
            assert len(values) == 1919 and 29 <= seconds <= 31, label
            version = label.rsplit("-", 1)[1]
            for probe in [window["probe_before"], *window["observations"], window["probe_after"]]:
                assert probe["coreMvid"] == window["probe_before"]["coreMvid"]
                assert probe["humanPlayers"] == probe["openMenus"] == 1 and probe["botPlayers"] == 0
                assert probe["targetMenu"] and not probe["closed"] and probe["selected"] == probe["offset"] == 0
                assert probe["options"] == 10
                assert probe["coreVersion"] == ("1.4.1" if version == "old" else "1.4.1-perf.2")
                assert probe["cacheField"] == (version == "new")
                if version == "new":
                    assert probe["cached"]
            row = next(r for r in expected["windows"] if r["label"] == label)
            equal(describe(values), {k: v for k, v in row.items() if k not in ("label", "menus")})
            grouped[(label.rsplit("-", 2)[0], version)].extend(values)
            durations[label] = seconds
    for row in expected["aggregate"]:
        equal(describe(grouped[(row["scenario"], row["version"])]),
              {k: v for k, v in row.items() if k not in ("scenario", "version")})
    for row in expected["comparisons"]:
        scenario = row["scenario"]
        old = statistics.mean(grouped[(scenario, "old")])
        new = statistics.mean(grouped[(scenario, "new")])
        equal(dict(reduction_percent=(1-new/old)*100, speedup=old/new, saving_us_per_tick=(old-new)*1000),
              {k:v for k,v in row.items() if k != "scenario"})
    for scenario in ("static-m1", "dynamic-m1"):
        effects = []
        for old_round, new_round in ((1, 2), (4, 3), (5, 6)):
            old = next(r for r in expected["windows"] if r["label"] == f"{scenario}-r{old_round}-old")
            new = next(r for r in expected["windows"] if r["label"] == f"{scenario}-r{new_round}-new")
            effects.append((1-new["mean_us"]/old["mean_us"])*100)
        print(f"{scenario}: paired mean reductions {effects}; median {statistics.median(effects):.2f}%")
    print(f"Verified 12 balanced raw profiles, {sum(len(v) for v in grouped.values())} OnTick calls; all windows and pooled distributions match")
    synthetic = json.loads((ROOT / "synthetic-manifest.json").read_text())
    for name, sha in synthetic["source_sha256_lf"].items():
        assert digest((ROOT.parents[2] / name).read_bytes(), True) == sha, name
    for name, sha in synthetic["data_sha256_lf"].items():
        assert digest((ROOT / name).read_bytes(), True) == sha, name
    old = json.loads((ROOT / "synthetic-baseline.json").read_text())
    new = json.loads((ROOT / "synthetic-candidate.json").read_text())
    assert len(old) == len(new) == 24
    for baseline, candidate in zip(old, new):
        for key in ("count", "dynamic", "window", "rounds", "sends", "html_sha256"):
            assert baseline[key] == candidate[key], (key, baseline, candidate)
        assert baseline["sends"] == baseline["count"] * baseline["rounds"]
        if not candidate["dynamic"]:
            assert candidate["allocated_bytes"] == 0
    print("Verified 24 synthetic windows per version, source hashes, identical sends/HTML and allocation-free unchanged-menu path")


if __name__ == "__main__":
    main()
