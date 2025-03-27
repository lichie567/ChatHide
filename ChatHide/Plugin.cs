using System;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.Command;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Component.GUI;
using ImGuiNET;

namespace ChatHide
{
    public class Plugin : IDalamudPlugin
    {
        public static readonly string Version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "0.1.0.1";

        public string Name => "ChatHide";

        private const string AddonName = "ChatLog";

        private IDalamudPluginInterface _pluginInterface;
        private ICommandManager _commandManager;
        private IClientState _clientState;
        private ICondition _condition;
        private bool _enabled = false;
        private bool _visible = true;

        public Plugin(
            IClientState clientState,
            ICommandManager commandManager,
            ICondition condition,
            IDalamudPluginInterface pluginInterface
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
                bool show = this.CheckIfHovered(AddonName) || this.CheckIfFocused(AddonName);
                if (_visible != show)
                {
                    UpdateAddonVisibility(AddonName, show);
                    _visible = show;
                }
            }
        }

        
        private void PluginCommand(string command, string arguments)
        {
            _enabled ^= true;
            if (!_enabled)
            {
                UpdateAddonVisibility(AddonName, true);
            }
        }

        public unsafe void UpdateAddonVisibility(string addonName, bool visible)
        {
            AtkStage* stage = AtkStage.Instance();
            AtkUnitList* loadedUnitsList = &stage->RaptureAtkUnitManager->AtkUnitManager.AllLoadedUnitsList;

            for (var i = 0; i < loadedUnitsList->Count; i++)
            {
                AtkUnitBase* addon = *(AtkUnitBase**)Unsafe.AsPointer(ref loadedUnitsList->Entries[i]);
                string? name = addon->NameString;

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

        private unsafe bool CheckIfHovered(string addonName)
        {
            var stage = AtkStage.Instance();

            AtkUnitList* loadedUnitsList = &stage->RaptureAtkUnitManager->AtkUnitManager.AllLoadedUnitsList;
            for (var i = 0; i < loadedUnitsList->Count; i++)
            {
                AtkUnitBase* addon = *(AtkUnitBase**)Unsafe.AsPointer(ref loadedUnitsList->Entries[i]);
                string? name = addon->NameString;

                if (name != null && name.Equals(addonName))
                {
                    Vector2 a = new Vector2(addon->X, addon->Y);
                    Vector2 b = new Vector2(a.X + addon->GetScaledWidth(true), a.Y + addon->GetScaledHeight(true));
                    Vector2 mouse = ImGui.GetMousePos();
                    
                    if (mouse.X >= a.X && mouse.X <= b.X &&
                        mouse.Y >= a.Y && mouse.Y <= b.Y)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private unsafe bool CheckIfFocused(string addonName)
        {
            var stage = AtkStage.Instance();
            var focusedUnitsList = &stage->RaptureAtkUnitManager->AtkUnitManager.FocusedUnitsList;
            for (var i = 0; i < focusedUnitsList->Count; i++)
            {
                AtkUnitBase* addon = *(AtkUnitBase**)Unsafe.AsPointer(ref focusedUnitsList->Entries[i]);
                string? name = addon->NameString;

                if (addonName.Equals(name))
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
                    UpdateAddonVisibility(AddonName, true);
                }

                _commandManager.RemoveHandler("/ch");
                _pluginInterface.UiBuilder.Draw -= Draw;
            }
        }
    }
}
