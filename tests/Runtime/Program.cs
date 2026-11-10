using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Menu;
using MenuManager;

int checks=0;
void Check(bool condition, string label) { checks++; if (!condition) throw new Exception(label); }
MenuManagerCore Reset()
{
    Control.Clear(); Utilities.Players.Clear(); Utilities.Scans=0;
    var plugin=new MenuManagerCore(); Control.Init(plugin); plugin.OnConfigParsed(new());
    plugin.Localizer["menumanager.selected_item"]=new() { Value="<b>[MENUITEM]</b>" };
    plugin.Localizer["menumanager.empty"]=new() { Value="empty" };
    plugin.Localizer["menumanager.footer"]=new() { Value="footer" };
    return plugin;
}
CCSPlayerController Player(int slot=0) => new() { Slot=slot, Handle=slot+100,
    EntityHandle=new() { Raw=(uint)(slot+100) }, PlayerPawn=new() { Value=new() {
        Handle=slot+200, EntityHandle=new() { Raw=(uint)(slot+200) }, VelocityModifier=.75f } } };
ButtonMenu Menu(string title="menu", Action<CCSPlayerController,ChatMenuOption>? action=null)
{
    var menu=new ButtonMenu(title);
    for(int i=0;i<10;i++) menu.AddMenuOption($"{{Red}}item{i}",action ?? ((_,_)=>{}));
    return menu;
}
void Press(CCSPlayerController player, PlayerButtons buttons) { player.Buttons=buttons; Control.OnPluginTick(); }

if(args.Contains("--benchmark"))
{
    var rows=new List<object>();
    foreach(int count in new[] { 0,1,8,32 })
    foreach(bool changing in new[] { false,true })
    {
        Reset();
        for(int i=0;i<count;i++) { var p=Player(i); p.RetainHtml=false; Utilities.Players.Add(p); Menu().Open(p); }
        for(int i=0;i<32;i++) Control.OnPluginTick();
        for(int window=0;window<3;window++)
        {
            foreach(var p in Utilities.Players) p.Sends=0;
            int scans=Utilities.Scans;
            var timer=new System.Diagnostics.Stopwatch();
            long allocated=GC.GetAllocatedBytesForCurrentThread(); timer.Start();
            const int rounds=128;
            for(int frame=0;frame<rounds;frame++)
            {
                if(changing && frame%64==0)
                    foreach(var menu in Control.menus) menu.menu.MenuOptions[1].Text=$"value {frame}";
                Control.OnPluginTick();
            }
            timer.Stop(); allocated=GC.GetAllocatedBytesForCurrentThread()-allocated;
            Check(Utilities.Scans==scans,"benchmark never scans all players");
            Check(Utilities.Players.All(p=>p.Sends==rounds),"benchmark retains every send");
            var html=Utilities.Players.Select(p=>p.LastHtml).ToArray();
            rows.Add(new { count,dynamic=changing,window,rounds,elapsed_ms=timer.Elapsed.TotalMilliseconds,allocated_bytes=allocated,
                sends=Utilities.Players.Sum(p=>p.Sends),html_sha256=Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(string.Join("\n",html)))) });
        }
    }
    Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(rows)); return;
}

foreach(int count in new[] { 0,1,8,32 })
{
    Reset();
    for(int i=0;i<count;i++) { var p=Player(i); Utilities.Players.Add(p); Menu().Open(p); }
    Control.OnPluginTick(); Control.OnPluginTick();
    Check(Utilities.Scans==0,$"{count}: tick never scans all players");
    Check(Control.menus.Count==count,$"{count}: active count");
    foreach(var p in Utilities.Players) Check(p.Html.Count==2 && ReferenceEquals(p.Html[0],p.Html[1]),$"{count}: native send every tick, cached HTML");
    Control.Clear();
    foreach(var p in Utilities.Players) Check(p.PlayerPawn.Value!.VelocityModifier==.75f,$"{count}: clear restores movement");
}
var plugin=Reset(); var player=Player(); int selects=0;
Menu(action:(_,_)=>selects++).Open(player);
Press(player,PlayerButtons.Use); Control.OnPluginTick();
Check(selects==1,"select held does not repeat");
Press(player,0); Press(player,PlayerButtons.Use); Check(selects==2,"select release and repress");
Press(player,PlayerButtons.Back|PlayerButtons.Use);
Check(Control.menus[0].Selected()==1 && selects==2,"combined buttons retain priority");
plugin.Config.ButtonsConfig.SelectButton=PlayerButtons.Duck|PlayerButtons.Use;
Press(player,PlayerButtons.Duck); Check(selects==2,"combined custom binding requires all bits");
Press(player,PlayerButtons.Duck|PlayerButtons.Use); Check(selects==3,"combined custom binding selects");
Press(player,PlayerButtons.Reload);
Check(Control.menus.Count==0 && player.PlayerPawn.Value!.VelocityModifier==.75f,"exit releases movement");

plugin=Reset(); player=Player();
var first=Menu("old",(p,_)=>Menu("new").Open(p)); first.PostSelectAction=PostSelectAction.Close; first.Open(player);
Press(player,PlayerButtons.Use);
Check(Control.menus.Count==1 && Control.menus[0].menu.Title=="new","selection callback replacement survives old close");
Check(player.Html.Count==1 && player.Html[0].Contains("new") && !player.Html[0].Contains("old"),"only replacement renders");
Control.CloseMenu(player); Control.OnPluginTick();
Check(Control.menus.Count==0 && player.PlayerPawn.Value!.VelocityModifier==.75f,"closed menu releases with held buttons");

plugin=Reset(); player=Player(); var back=Menu("old"); back.BackAction=p=>Menu("back").Open(p); back.Open(player);
Press(player,PlayerButtons.Duck); Check(Control.menus.Single().menu.Title=="back" && player.Html.Single().Contains("back"),"back callback replacement");
Control.Clear(); Check(player.PlayerPawn.Value!.VelocityModifier==.75f,"replaced menu restores original modifier");
Menu(action:(_,_)=>Control.Clear()).Open(player); Press(player,PlayerButtons.Use);
Check(Control.menus.Count==0 && player.PlayerPawn.Value!.VelocityModifier==.75f,"callback clears collection safely");

plugin=Reset(); player=Player(); var dynamic=Menu(); dynamic.Open(player); Control.OnPluginTick();
Press(player,PlayerButtons.Moveright); Press(player,0); Press(player,PlayerButtons.Moveright);
Check(Control.menus[0].Selected()==9,"fast page navigation");
dynamic.MenuOptions.RemoveRange(1,9); Control.OnPluginTick();
Check(Control.menus[0].Selected()==0 && player.Html.Last().Contains("item0"),"dynamic shrink via actual tick");
dynamic.MenuOptions[0].Disabled=true; int disabled=0; dynamic.MenuOptions[0].OnSelect=(_,_)=>disabled++;
Press(player,PlayerButtons.Use); Check(disabled==0 && player.Html.Last().Contains("#aaaaaa"),"disabled item cannot select");
dynamic.MenuOptions.Clear(); Press(player,PlayerButtons.Back); Check(player.Html.Last().Contains("empty"),"empty menu navigation");

plugin=Reset(); player=Player(); Menu().Open(player); Control.OnPluginTick();
plugin.Config.MoveWhileOpenMenu=true; Control.OnPluginTick();
Check(player.PlayerPawn.Value!.VelocityModifier==.75f,"config unlock restores movement");
player.PlayerPawn.Value.VelocityModifier=.9f; Control.Clear();
Check(player.PlayerPawn.Value.VelocityModifier==.9f,"clear does not restore a lock already released");

plugin=Reset(); player=Player(); Menu().Open(player); Control.OnPluginTick();
player.PlayerPawn.Value=new() { Handle=200, EntityHandle=new() { Raw=4296 }, VelocityModifier=.9f };
Control.OnPluginTick(); Check(player.PlayerPawn.Value.VelocityModifier==0,"replacement pawn gets its own menu lock");
Control.Clear(); Check(player.PlayerPawn.Value.VelocityModifier==.9f,"reused pawn address restores new pawn modifier");

plugin=Reset(); player=Player(); Menu().Open(player); Control.OnPluginTick();
player.EntityHandle.Raw+=4096; player.PlayerPawn.Value!.VelocityModifier=.9f;
int sends=player.Html.Count; Control.OnPluginTick();
Check(Control.menus.Count==0 && player.Html.Count==sends && player.PlayerPawn.Value.VelocityModifier==.9f,"reused controller address cannot inherit menu or lock");

plugin=Reset(); player=Player(); Menu().Open(player); Control.OnPluginTick();
player.Connected=PlayerConnectedState.Disconnected; Control.OnPluginTick();
Check(Control.menus.Count==0 && player.PlayerPawn.Value!.VelocityModifier==.75f,"disconnect releases valid original pawn");

plugin=Reset(); player=Player(); Menu(action:(p,_)=>p.IsValid=false).Open(player);
Press(player,PlayerButtons.Use); Check(Control.menus.Count==0 && player.Html.Count==0,"callback disconnect cannot render stale controller");

plugin=Reset(); player=Player(); plugin.Load(false); Menu().Open(player); Control.OnPluginTick();
((Listeners.OnMapEnd)plugin.ListenersByType[typeof(Listeners.OnMapEnd)])();
Check(Control.menus.Count==0 && player.PlayerPawn.Value!.VelocityModifier==.75f,"registered map end clears and releases");
Menu().Open(player); Control.OnPluginTick(); plugin.OnMapStart("next");
Check(Control.menus.Count==0 && player.PlayerPawn.Value.VelocityModifier==.75f,"map start safety cleanup");
Menu().Open(player); Control.OnPluginTick(); plugin.Unload(true);
Check(Control.menus.Count==0 && player.PlayerPawn.Value.VelocityModifier==.75f,"unload cleanup");
foreach(var type in new[] { MenuType.ChatMenu,MenuType.ConsoleMenu,MenuType.CenterMenu,MenuType.ButtonMenu,MenuType.MetamodMenu })
{
    plugin=Reset(); player=Player(); var api=new CMenuApi(plugin);
    var routed=api.GetMenuForcetype("route",type); routed.AddMenuOption("item",(_,_)=>{}); routed.Open(player);
    bool buttons=type is MenuType.ButtonMenu or MenuType.MetamodMenu;
    Check(buttons ? Control.menus.Count==1 : player.Commands.Count==1,$"{type}: preserved routing (unavailable Metamod falls back)");
}
Console.WriteLine($"O3 production runtime: {checks} checks passed");
