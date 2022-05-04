using System;
using System.Reflection;
using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.Command;
using Dalamud.Plugin;
using FFXIVClientStructs.FFXIV.Component.GUI;
using System.Runtime.InteropServices;

namespace ChatHide
{
    public class Plugin : IDalamudPlugin
    {
        public static readonly string Version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "0.1.0.0";

        public string Name => "ChatHide";

        private DalamudPluginInterface _pluginInterface;
        private CommandManager _commandManager;
        private ClientState _clientState;
        private Condition _condition;
        private bool _enabled = false;
        private bool _visible = true;

        public Plugin(
            ClientState clientState,
            CommandManager commandManager,
            Condition condition,
            DalamudPluginInterface pluginInterface
        )
        {
            _commandManager = commandManager;
            _pluginInterface = pluginInterface;
            _condition = condition;
            _clientState = clientState;

            _commandManager.AddHandler(
                "/ch",
                new CommandInfo(PluginCommand)
                {
                    HelpMessage = "Toggle chat hiding.",
                    ShowInHelp = true
                }
            );

            _pluginInterface.UiBuilder.Draw += Draw;
        }

        public void Draw()
        {
            if (_clientState.LocalPlayer == null ||
                _condition[ConditionFlag.BetweenAreas] ||
                !_clientState.IsLoggedIn)
            {
                return;
            }

            if (_enabled)
            {
                bool focused = this.CheckIfFocused("ChatLog");
                if (_visible != focused)
                {
                    UpdateAddonVisibility("ChatLog", focused);
                    _visible = focused;
                }
            }
        }

        
        private void PluginCommand(string command, string arguments)
        {
            _enabled ^= true;
            if (!_enabled)
            {
                UpdateAddonVisibility("ChatLog", true);
            }
        }

        public unsafe void UpdateAddonVisibility(string addonName, bool visible)
        {
            AtkStage* stage = AtkStage.GetSingleton();
            AtkUnitList* loadedUnitsList = &stage->RaptureAtkUnitManager->AtkUnitManager.AllLoadedUnitsList;
            AtkUnitBase** addonList = &loadedUnitsList->AtkUnitEntries;

            for (var i = 0; i < loadedUnitsList->Count; i++)
            {
                AtkUnitBase* addon = addonList[i];
                string? name = Marshal.PtrToStringAnsi(new IntPtr(addon->Name));

                if (name != null && name.StartsWith(addonName))
                {
                    if (visible)
                    {
                        if (addon->UldManager.NodeListCount == 0)
                        {
                            addon->UldManager.UpdateDrawNodeList();
                        }
                    }
                    else
                    {
                        if (addon->UldManager.NodeListCount != 0)
                        {
                            addon->UldManager.NodeListCount = 0;
                        }
                    }
                }
            }
        }

        private unsafe bool CheckIfFocused(string name)
        {
            var stage = AtkStage.GetSingleton();
            var focusedUnitsList = &stage->RaptureAtkUnitManager->AtkUnitManager.FocusedUnitsList;
            var focusedAddonList = &focusedUnitsList->AtkUnitEntries;

            for (var i = 0; i < focusedUnitsList->Count; i++)
            {
                var addon = focusedAddonList[i];
                var addonName = Marshal.PtrToStringAnsi(new IntPtr(addon->Name));

                if (addonName == name)
                {
                    return true;
                }
            }

            return false;
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_enabled || !_visible)
                {
                    UpdateAddonVisibility("ChatLog", true);
                }

                _commandManager.RemoveHandler("/ch");
                _pluginInterface.UiBuilder.Draw -= Draw;
            }
        }
    }
}
