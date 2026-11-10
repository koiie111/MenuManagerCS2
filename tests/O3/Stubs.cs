namespace CounterStrikeSharp.API { [Flags] public enum PlayerButtons : ulong { Forward=8, Back=16, Use=32, Reload=8192 } }
namespace CounterStrikeSharp.API.Modules.Menu { public enum PostSelectAction { Nothing, Close, Reset } public class ChatMenuOption { public string Text; public bool Disabled; public Action<CounterStrikeSharp.API.Core.CCSPlayerController,ChatMenuOption> OnSelect; public ChatMenuOption(string text, bool disabled=false, Action<CounterStrikeSharp.API.Core.CCSPlayerController,ChatMenuOption>? action=null) { Text=text; Disabled=disabled; OnSelect=action ?? ((_,_)=>{}); } } }
namespace CounterStrikeSharp.API.Core {
    public class Identity { public uint Raw=1; }
    public class Pawn { public bool IsValid=true; public nint Handle; public float VelocityModifier=1; }
    public class PawnHandle { public Pawn? Value; public uint Raw => (uint)(Value?.Handle ?? 0); }
    public class CCSPlayerController { public bool IsValid=true; public Identity EntityHandle=new(); public CounterStrikeSharp.API.PlayerButtons Buttons; public PawnHandle PlayerPawn=new(); }
}
namespace CounterStrikeSharp.API.Core.Translations { }
namespace Serilog.Core { }
namespace Microsoft.Extensions.Logging { }
namespace MenuManager {
    using CounterStrikeSharp.API.Core;
    using CounterStrikeSharp.API.Modules.Menu;
    public class ButtonMenu { public string Title="Title"; public List<ChatMenuOption> MenuOptions=new(); public PostSelectAction PostSelectAction; public Action<CCSPlayerController>? ResetAction; }
    public class Config { public int MenuLinesCount=6; public bool IgnoreErrors=true; public string SoundScroll="",SoundDisabled="",SoundClick="",SoundExit=""; }
    public class Localized { public string Value=""; public static implicit operator string(Localized x)=>x.Value; public override string ToString()=>Value; }
    public class Localizer : Dictionary<string,Localized> { public new Localized this[string key] { get => base[key]; set => base[key]=value; } }
    public class Plugin { public Config Config=new(); public Localizer Localizer=new(); public Logger Logger=new(); }
    public class Logger { public void LogInformation(string text) {} }
    internal static class Control { public static Plugin Instance=new(); public static Plugin GetPlugin()=>Instance; public static void PlaySound(CCSPlayerController p,string s) {} public static bool HasOpenedMenu(CCSPlayerController p,PlayerInfo info)=>false; }
    public static class Misc { public static string ColorText(string text,bool colors=true)=>text; }
}
