namespace CounterStrikeSharp.API
{
    [Flags] public enum PlayerButtons : ulong { Forward=8, Back=16, Use=32, Moveleft=512, Moveright=1024, Reload=8192, Duck=4 }
    public static class Utilities
    {
        public static List<Core.CCSPlayerController> Players = new();
        public static int Scans;
        public static List<Core.CCSPlayerController> GetPlayers() { Scans++; return new(Players); }
    }
}
namespace CounterStrikeSharp.API.Core
{
    public class BasePluginConfig { }
    public interface IPluginConfig<T> { T Config { get; set; } void OnConfigParsed(T config); }
    public enum HookResult { Continue }
    public enum HookMode { Pre, Post }
    public class GameEventInfo { }
    public class EventPlayerDisconnect { public CCSPlayerController Userid=null!; }
    public static class Listeners
    {
        public delegate void OnTick(); public delegate void OnMapStart(string map); public delegate void OnMapEnd();
    }
    public abstract class BasePlugin
    {
        public abstract string ModuleName { get; }
        public abstract string ModuleVersion { get; }
        public abstract string ModuleAuthor { get; }
        public abstract string ModuleDescription { get; }
        public Dictionary<string,MenuManager.Localized> Localizer=new();
        public MenuManager.Logger Logger=new();
        public Dictionary<Type,Delegate> ListenersByType=new();
        public Action<EventPlayerDisconnect>? Disconnect;
        public virtual void Load(bool hotReload) { }
        public virtual void Unload(bool hotReload) { }
        public virtual void OnAllPluginsLoaded(bool hotReload) { }
        public void RegisterListener<T>(T callback) where T:Delegate => ListenersByType[typeof(T)]=callback;
        public void RegisterEventHandler<T>(Func<T,GameEventInfo,HookResult> callback,HookMode mode) =>
            Disconnect=e=>callback((T)(object)e,new());
    }
    public enum PlayerConnectedState { PlayerConnected, PlayerDisconnected }
    public class EntityIdentity { public uint Raw { get; set; } }
    public class Pawn { public bool IsValid=true; public nint Handle; public EntityIdentity EntityHandle=new(); public float VelocityModifier=1; }
    public class PawnHandle { public Pawn? Value; public uint Raw => Value?.EntityHandle.Raw ?? uint.MaxValue; }
    public class CCSPlayerController
    {
        public bool IsValid=true, IsBot, IsHLTV;
        public int Slot;
        public nint Handle;
        public EntityIdentity EntityHandle=new();
        public PlayerConnectedState Connected=PlayerConnectedState.PlayerConnected;
        public string PlayerName="player";
        public CounterStrikeSharp.API.PlayerButtons Buttons;
        public PawnHandle PlayerPawn=new();
        public List<string> Html=new(), Commands=new();
        public bool RetainHtml=true;
        public int Sends;
        public string? LastHtml;
        public void PrintToCenterHtml(string text, int duration) { Sends++; LastHtml=text; if(RetainHtml) Html.Add(text); }
        public void ExecuteClientCommand(string command) => Commands.Add(command);
        public void PrintToChat(string text) { }
    }
}
namespace CounterStrikeSharp.API.Modules.Entities { }
namespace CounterStrikeSharp.API.Core.Translations { }
namespace Serilog.Core { }
namespace Microsoft.Extensions.Logging { }
namespace CounterStrikeSharp.API.Modules.Menu
{
    public enum PostSelectAction { Nothing, Close, Reset }
    public class ChatMenuOption
    {
        public string Text; public bool Disabled;
        public Action<CounterStrikeSharp.API.Core.CCSPlayerController,ChatMenuOption> OnSelect;
        public ChatMenuOption(string text, bool disabled, Action<CounterStrikeSharp.API.Core.CCSPlayerController,ChatMenuOption> action)
        { Text=text; Disabled=disabled; OnSelect=action; }
    }
    public interface IMenu
    {
        string Title { get; set; } bool ExitButton { get; set; }
        PostSelectAction PostSelectAction { get; set; }
        List<ChatMenuOption> MenuOptions { get; }
        ChatMenuOption AddMenuOption(string text,Action<CounterStrikeSharp.API.Core.CCSPlayerController,ChatMenuOption> action,bool disabled=false);
        void Open(CounterStrikeSharp.API.Core.CCSPlayerController player);
        void OpenToAll();
    }
    public class ChatMenu : IMenu
    {
        public ChatMenu(string title) { Title=title; }
        public string Title { get; set; }
        public bool ExitButton { get; set; }
        public PostSelectAction PostSelectAction { get; set; }
        public List<ChatMenuOption> MenuOptions { get; }=new();
        public ChatMenuOption AddMenuOption(string text,Action<CounterStrikeSharp.API.Core.CCSPlayerController,ChatMenuOption> action,bool disabled=false)
        { var option=new ChatMenuOption(text,disabled,action); MenuOptions.Add(option); return option; }
        public void Open(CounterStrikeSharp.API.Core.CCSPlayerController player)=>player.Commands.Add(GetType().Name+":"+Title);
        public void OpenToAll() { }
    }
    public class ConsoleMenu(string title) : ChatMenu(title) { }
    public class CenterHtmlMenu(string title,CounterStrikeSharp.API.Core.BasePlugin plugin) : ChatMenu(title) { }
    public static class MenuManager { public static void CloseActiveMenu(CounterStrikeSharp.API.Core.CCSPlayerController player) { } }
}
namespace CounterStrikeSharp.API.Core.Attributes.Registration
{
    [AttributeUsage(AttributeTargets.Method)] public class ConsoleCommandAttribute(string command,string description):Attribute { }
}
namespace CounterStrikeSharp.API.Core.Capabilities
{
    public class PluginCapability<T>(string name) { public T? Get()=>default; }
    public static class Capabilities { public static void RegisterPluginCapability<T>(PluginCapability<T> capability,Func<T> getter) { } }
}
namespace CounterStrikeSharp.API.Modules.Commands { public class CommandInfo { } }
namespace PlayerSettings
{
    public interface ISettingsApi
    {
        string GetPlayerSettingsValue(CounterStrikeSharp.API.Core.CCSPlayerController player, string key, string fallback);
        void SetPlayerSettingsValue(CounterStrikeSharp.API.Core.CCSPlayerController player, string key, string? value);
    }
}
namespace MenuManager
{
    public class Localized
    {
        public string Value="";
        public static implicit operator string(Localized value)=>value.Value;
        public override string ToString()=>Value;
    }
    public class Logger { public void LogInformation(string text) { } public void LogWarning(string text) { } }
    internal static class MenusMM
    {
        public static void Init() { }
        public static void ClearCallbackInfo(int slot) { }
        public static bool Hooked()=>false;
        public static void PassMenuToMM(CounterStrikeSharp.API.Core.CCSPlayerController player,MenuInstance menu) { }
        public static bool IsMenuOpen(int slot)=>false;
        public static void ClosePlayerMenu(int slot) { }
    }
}
