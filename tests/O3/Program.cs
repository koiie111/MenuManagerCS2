using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Menu;
using MenuManager;

static void Check(bool condition, string name) { if (!condition) throw new Exception(name); }
Control.Instance.Localizer["menumanager.selected_item"] = new() { Value = "<b>[MENUITEM]</b>" };
Control.Instance.Localizer["menumanager.empty"] = new() { Value = "empty" };
Control.Instance.Localizer["menumanager.footer"] = new() { Value = "footer" };
var pawn = new Pawn { Handle = 11, VelocityModifier = .75f };
var player = new CCSPlayerController { PlayerPawn = new() { Value = pawn }, Buttons = PlayerButtons.Forward };
var menu = new ButtonMenu { Title = "first" };
for (var i=0; i<10; i++) menu.MenuOptions.Add(new($"item{i}"));
var info = new PlayerInfo(player, menu);
var initial = info.GetText();
Check(ReferenceEquals(initial, info.GetText()), "steady state must reuse HTML");
Check(info.IsEqualButtons(PlayerButtons.Forward), "held input");
Check(!info.IsEqualButtons(PlayerButtons.Forward | PlayerButtons.Use), "combined input change");
Check(info.IsEqualButtons(PlayerButtons.Forward | PlayerButtons.Use), "held combined input");
menu.Title = "second"; Check(info.GetText() != initial, "title mutation");
var before = info.GetText(); menu.MenuOptions[0].Text = "changed"; Check(info.GetText() != before, "direct text mutation");
before = info.GetText(); menu.MenuOptions[0].Disabled = true; Check(info.GetText() != before, "direct disabled mutation");
before = info.GetText(); menu.MenuOptions[0] = new("replacement"); Check(info.GetText() != before, "option replacement");
before = info.GetText(); info.MoveDown(); Check(info.GetText() != before, "selection");
before = info.GetText(); Control.Instance.Config.MenuLinesCount = 3; Check(info.GetText() != before, "line count");
before = info.GetText(); Control.Instance.Localizer["menumanager.footer"].Value = "new footer"; Check(info.GetText() != before, "locale");
before = info.GetText(); menu.MenuOptions.RemoveAt(0); Check(info.GetText() != before, "option count");
before = info.GetText(); menu.MenuOptions.Reverse(); Check(info.GetText() != before, "option order");
var emptyMenu = new ButtonMenu();
var emptyInfo = new PlayerInfo(player, emptyMenu);
Check(emptyInfo.GetText().Contains("empty"), "empty menu");
Check(ReferenceEquals(emptyInfo.GetText(), emptyInfo.GetText()), "empty menu cache");
info.ApplyMovementLock(); Check(pawn.VelocityModifier == 0, "movement locked");
info.RestoreMovement(); Check(pawn.VelocityModifier == .75f, "movement restored");
player.PlayerPawn.Value = new Pawn { Handle = 12, VelocityModifier = .9f };
info.ApplyMovementLock(); info.RestoreMovement(); Check(player.PlayerPawn.Value.VelocityModifier == .9f, "new pawn untouched");
Console.WriteLine("O3 behavior: 17 checks passed");

string OldRender(ButtonMenu m, int offset, int selected)
{
    var text = $"<font class='mono-spaced-font'>{m.Title}</font><font class='fontSize-sm stratum-font'>";
    if (m.MenuOptions.Count > 0)
        for (var i = offset; i < Math.Min(offset + Control.Instance.Config.MenuLinesCount, m.MenuOptions.Count); i++)
        {
            var line = m.MenuOptions[i].Text;
            if (m.MenuOptions[i].Disabled) line = $"<font color='#aaaaaa'>{Misc.ColorText(line, false)}</font>";
            if (i == selected) line = Control.Instance.Localizer["menumanager.selected_item"].Value.Replace("[MENUITEM]", line);
            text += "<br>" + line;
        }
    else text = $"{text}<br><font color='#aaaaaa'>{Control.Instance.Localizer["menumanager.empty"]}</font>";
    return Misc.ColorText(text + $"</font><br><font class='fontSize-s'>{Control.Instance.Localizer["menumanager.footer"]}</font>");
}
Control.Instance.Config.MenuLinesCount = 6;
foreach (var n in new[] { 0, 1, 8, 32 })
{
    var infos = new List<PlayerInfo>();
    var menus = new List<ButtonMenu>();
    for (var j = 0; j < n; j++)
    {
        var m = new ButtonMenu();
        for (var k = 0; k < 10; k++) m.MenuOptions.Add(new($"entry{k}"));
        var p = new CCSPlayerController { PlayerPawn = new() { Value = new Pawn { Handle = j + 100 } } };
        menus.Add(m); infos.Add(new PlayerInfo(p, m));
        var actual = infos[^1].GetText(); var expected = OldRender(m, 0, 0);
        if (actual != expected) Console.WriteLine($"actual={actual}\nexpected={expected}");
        Check(actual == expected, "baseline HTML equality");
    }
    const int rounds = 10000;
    for (var warm = 0; warm < 100; warm++) foreach (var x in infos) x.GetText();
    var alloc0 = GC.GetAllocatedBytesForCurrentThread();
    var sw = System.Diagnostics.Stopwatch.StartNew();
    for (var r = 0; r < rounds; r++) foreach (var m in menus) OldRender(m, 0, 0);
    sw.Stop(); var oldMs = sw.Elapsed.TotalMilliseconds; var oldBytes = GC.GetAllocatedBytesForCurrentThread() - alloc0;
    alloc0 = GC.GetAllocatedBytesForCurrentThread(); sw.Restart();
    for (var r = 0; r < rounds; r++) foreach (var x in infos) x.GetText();
    sw.Stop(); var newMs = sw.Elapsed.TotalMilliseconds; var newBytes = GC.GetAllocatedBytesForCurrentThread() - alloc0;
    Console.WriteLine($"synthetic open={n} rounds={rounds} old_ms={oldMs:F2} new_ms={newMs:F2} old_bytes={oldBytes} new_bytes={newBytes}");
}
