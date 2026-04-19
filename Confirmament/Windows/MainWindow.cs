using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using Lumina.Excel.Sheets;
using Dalamud.Plugin.Services;
using System.Linq;
using System.Threading.Tasks;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.ClientState.Objects.Types;

namespace Confirmament.Windows;

public class MainWindow : Window, IDisposable
{
    private readonly string goatImagePath;
    private readonly Plugin plugin;

    public MainWindow(Plugin plugin, string goatImagePath)
        : base("My Amazing Window##With a hidden ID", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
    {
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(375, 330),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };

        this.goatImagePath = goatImagePath;
        this.plugin = plugin;
    }

    public void Dispose() { }

    public override void Draw()
    {
        ImGui.Text($"The random config bool is {plugin.Configuration.SomePropertyToBeSavedAndWithADefault}");

        if (ImGui.Button("Show Settings"))
        {
            plugin.ToggleConfigUi();
        }

        ImGui.Spacing();

        if (!IsHandinRunning)
        {
            if (ImGui.Button("Begin hand-in"))
            {
                IsHandinRunning = true;
                statusMessage = "Starting kupo voucher automation...";
                BeginHandinAutomation();
            }
        }
        else
        {
            ImGui.TextColored(new Vector4(1, 1, 0, 1), statusMessage);
            if (ImGui.Button("Cancel"))
            {
                IsHandinRunning = false;
                statusMessage = "Automation cancelled.";
            }
        }

        ImGui.Spacing();

        using (var child = ImRaii.Child("SomeChildWithAScrollbar", Vector2.Zero, true))
        {
            if (child.Success)
            {
                ImGui.Text("Have a goat:");
                var goatImage = Plugin.TextureProvider.GetFromFile(goatImagePath).GetWrapOrDefault();
                if (goatImage != null)
                {
                    using (ImRaii.PushIndent(55f))
                    {
                        ImGui.Image(goatImage.Handle, goatImage.Size);
                    }
                }
                else
                {
                    ImGui.Text("Image not found.");
                }

                ImGuiHelpers.ScaledDummy(20.0f);

                var playerState = Plugin.PlayerState;
                if (!playerState.IsLoaded)
                {
                    ImGui.Text("Our local player is currently not logged in.");
                    return;
                }
                
                if (!playerState.ClassJob.IsValid)
                {
                    ImGui.Text("Our current job is currently not valid.");
                    return;
                }
                
                ImGui.AlignTextToFramePadding();
                ImGui.Text($"Current job:");
                
                ImGui.SameLine(120 * ImGuiHelpers.GlobalScale);
                
                var jobIconId = 62100 + playerState.ClassJob.RowId;
                var iconTexture = Plugin.TextureProvider.GetFromGameIcon(new GameIconLookup(jobIconId)).GetWrapOrEmpty();
                ImGui.Image(iconTexture.Handle, new Vector2(28, 28) * ImGuiHelpers.GlobalScale);
                
                ImGui.SameLine();
                ImGui.Text(playerState.ClassJob.Value.Abbreviation.ToString());
                
                ImGui.SameLine();
                ImGui.Text($" [Level {playerState.Level}]");
                
                var territoryId = Plugin.ClientState.TerritoryType;
                if (Plugin.DataManager.GetExcelSheet<TerritoryType>().TryGetRow(territoryId, out var territoryRow))
                {
                    ImGui.Text($"Current location:");
                    ImGui.SameLine(120 * ImGuiHelpers.GlobalScale);
                    ImGui.Text(territoryRow.PlaceName.Value.Name.ToString());
                }
                else
                {
                    ImGui.Text("Invalid territory.");
                }
            }
        }
    }

    private async void BeginHandinAutomation()
    {
        statusMessage = "Locating Lizbeth...";

        // 1. Find Lizbeth by name (or by known object kind/id if available)
        var lizbeth = Plugin.ObjectTable.FirstOrDefault(obj => obj is not null && obj.Name.TextValue == "Lizbeth" && obj.ObjectKind == Dalamud.Game.ClientState.Objects.Enums.ObjectKind.EventNpc);
        if (lizbeth is null)
        {
            statusMessage = "Lizbeth not found nearby.";
            IsHandinRunning = false;
            return;
        }

        // 2. Target Lizbeth
        Plugin.TargetManager.Target = lizbeth;
        await Task.Delay(300);


        // 3. Interact (simulate talk) using struct shadowing (Questionable method)
        InteractWithObject(lizbeth.Address);
        statusMessage = "Interacting with Lizbeth...";
        await Task.Delay(1000);

        // 4. Select 'Yes' on AddonSelectYesno (confirmation dialog)
        statusMessage = "Selecting 'Yes' on confirmation dialog...";
        // TODO: Use Dalamud's IGameGui or UI interaction to select 'Yes' on AddonSelectYesno
        // Example: GameGui.GetAddonByName("SelectYesno") and click Yes
        // await SelectYesOnDialog();
        await Task.Delay(800);

        // 5. Wait for HWDLottery window and pick a random crown
        statusMessage = "Waiting for kupo window...";
        // TODO: Use GameGui.GetAddonByName("HWDLottery") and click a random Button Component Node
        // await InteractWithKupoWindow();
        await Task.Delay(1200);

        // 6. Click 'Close' and repeat if possible
        statusMessage = "Closing kupo window and checking for next voucher...";
        // TODO: Detect and click 'Close', repeat if 'continue' is available
        await Task.Delay(800);

        statusMessage = "Hand-in automation complete.";
        IsHandinRunning = false;
    }

    // Struct shadowing for FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject
    private unsafe struct ShadowGameObject { }

    private unsafe void InteractWithObject(IntPtr address)
    {
        // Get TargetSystem singleton
        var targetSystemType = Type.GetType("FFXIVClientStructs.FFXIV.Client.Game.TargetSystem, FFXIVClientStructs");
        var instanceProperty = targetSystemType?.GetProperty("Instance", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        var targetSystem = instanceProperty?.GetValue(null);
        if (targetSystem == null) return;

        // Get InteractWithObject method pointer
        var method = targetSystemType.GetMethod("InteractWithObject", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        if (method == null) return;

        // Prepare parameters
        method.Invoke(targetSystem, new object[] { address, false });
    }

    public bool IsHandinRunning { get; private set; } = false;
    private string statusMessage = string.Empty;
}
