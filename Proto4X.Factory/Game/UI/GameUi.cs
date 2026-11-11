using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using MonoGameLibrary.Input;
using MonoGameLibrary.UI;
using Proto4X.Factory.Input;
using Proto4X.Factory.Simulation;
using Proto4X.Factory.Simulation.Data;
using Proto4X.Factory.Simulation.Grid;
using Proto4X.Factory.Simulation.Power;
using Proto4X.Factory.Simulation.Ships;
using Proto4X.Factory.Simulation.Space;
using Proto4X.Factory.Simulation.Systems;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Proto4X.Factory.UI
{
    /// <summary>What the mouse is for: building, commanding the fleet, or designing ships.</summary>
    public enum InputMode
    {
        Build,
        Fleet,
        Designer,
    }

    /// <summary>
    /// Every panel, declared fresh each frame on the immediate-mode <see cref="Gui"/> (plans/01): the
    /// HUD, the toolbar, the inspector, the warehouse banner, the fleet panel and the ship designer.
    /// Reads the simulation and acts only by enqueueing <see cref="Command"/>s, like the controllers.
    /// </summary>
    public sealed class GameUi(Universe universe, Gui gui, BuildController build, FleetController fleet)
    {
        private const int SideWidth = 300;

        private ShipDesign draft = ShipPresets.Picket().With(name: "Design 4");
        private PartKind brush = PartKind.Hull;
        private Direction facing = Direction.North;
        private int designSlot = 3;

        public InputMode Mode { get; set; } = InputMode.Build;

        public void Build()
        {
            var viewport = gui.Viewport;
            Hud(new Rectangle(viewport.Right - SideWidth - 8, 8, SideWidth, 292));
            switch (Mode)
            {
                case InputMode.Build:
                    Toolbar(viewport);
                    Inspector(new Rectangle(viewport.Right - SideWidth - 8, 308, SideWidth, viewport.Height - 316));
                    Banner(viewport);
                    break;
                case InputMode.Fleet:
                    FleetPanel(new Rectangle(8, viewport.Bottom - 148, 340, 140));
                    break;
                case InputMode.Designer:
                    Designer(new Rectangle(8, 8, Math.Min(900, viewport.Width - SideWidth - 32), Math.Min(640, viewport.Height - 16)));
                    break;
            }

            if (fleet.DragBox is { } box && Mode == InputMode.Fleet)
            {
                gui.Outline(box, gui.Style.Accent);
            }
        }

        // ---------------------------------------------------------------- HUD

        private void Hud(Rectangle rect)
        {
            var space = universe.Space;
            // Widget ids are scoped by panel title, so titles stay constant: a ticking clock in one would drop clicks.
            gui.BeginPanel(rect, "Status");
            gui.Value($"T+{universe.Tick / Universe.TicksPerSecond / 60}:{universe.Tick / Universe.TicksPerSecond % 60:00}", Describe(space.Outcome), space.Outcome == Outcome.Lost ? gui.Style.Bad : gui.Style.Good);
            HealthBar("Station", space.Snapshot.FirstOrDefault(v => v.Role == BodyRole.Station), gui.Style.Accent);
            HealthBar("Bastion", space.Snapshot.FirstOrDefault(v => v.Role == BodyRole.Bastion), gui.Style.Bad);
            var ships = space.Snapshot.Count(v => v.Faction == Faction.Player && v.Role == BodyRole.Ship);
            var raiders = space.Snapshot.Count(v => v.Faction == Faction.Raider && v.Role == BodyRole.Ship);
            gui.Value("Fleet / raiders", $"{ships} / {raiders}");
            var untilWave = Math.Max(0, space.NextWaveTick - universe.Tick) / Universe.TicksPerSecond;
            gui.Value($"Wave {space.WavesSpawned + 1}", space.Peaceful ? "held (peaceful)" : $"in {untilWave / 60}:{untilWave % 60:00}",
                space.Peaceful ? gui.Style.TextDim : untilWave < 30 ? gui.Style.Warning : null);

            var peaceful = space.Peaceful;
            if (gui.Toggle("Peaceful (F6)", ref peaceful))
            {
                universe.Enqueue(new Command(CommandKind.SetPeaceful, -1, 0, 0, Config: peaceful ? 1 : 0));
            }

            gui.BeginColumns(3);
            if (gui.ButtonAt(gui.NextRow(), "Build", selected: Mode == InputMode.Build)) Mode = InputMode.Build;
            if (gui.ButtonAt(gui.NextRow(), "Fleet F", selected: Mode == InputMode.Fleet)) EnterFleetMode();
            if (gui.ButtonAt(gui.NextRow(), "Ships B", selected: Mode == InputMode.Designer)) Mode = InputMode.Designer;

            var outside = universe.Surfaces.Where(s => s.Kind != SurfaceKind.Interior).ToArray();
            gui.BeginColumns(outside.Length);
            foreach (var surface in outside)
            {
                if (gui.ButtonAt(gui.NextRow(), $"{surface.Name}##tab", enabled: universe.Live is null, selected: build.Surface == surface))
                {
                    build.ShowSurface(surface.Index);
                }
            }

            gui.EndPanel();
        }

        private void HealthBar(string name, ShipView view, Color colour)
        {
            var alive = view.MaxHp > 0;
            gui.ProgressBar(alive ? view.Hp / view.MaxHp : 0f, colour, alive ? $"{name} {view.Hp:0}/{view.MaxHp:0}" : $"{name} destroyed");
        }

        private static string Describe(Outcome outcome) => outcome switch
        {
            Outcome.Won => "BASTION DESTROYED - YOU WIN",
            Outcome.Lost => "STATION LOST",
            _ => string.Empty,
        };

        public void EnterFleetMode()
        {
            Mode = InputMode.Fleet;
            if (universe.Station is { } station)
            {
                build.ShowSurface(station.Index);
            }
        }

        // ---------------------------------------------------------------- toolbar

        private void Toolbar(Rectangle viewport)
        {
            var tools = build.Toolbar;
            var maxWidth = viewport.Width - SideWidth - 32;
            var options = OptionsFor(build.Tool);
            var rows = 1 + (options.Count > 0 ? 1 : 0);
            var buttonHeight = gui.RowHeight;

            // Lay the tool buttons out left to right, wrapping, then size the panel to fit.
            var cells = new List<(Rectangle Rect, string Label, Action Click, bool Selected)>();
            var x = 0;
            var y = 0;
            void Add(string label, Action click, bool selected)
            {
                var width = gui.MeasureWidth(label.Split("##")[0]) + 16;
                if (x > 0 && x + width > maxWidth - 12)
                {
                    x = 0;
                    y += buttonHeight + 4;
                }

                cells.Add((new Rectangle(x, y, width, buttonHeight), label, click, selected));
                x += width + 4;
            }

            Add("Inspect Q", () => build.SelectTool(BuildingKind.None), build.Tool == BuildingKind.None);
            for (var i = 0; i < tools.Length; i++)
            {
                var kind = tools[i];
                Add($"{(i < 12 ? (i == 10 ? "-" : i == 11 ? "=" : ((i + 1) % 10).ToString()) + " " : "")}{ShortName(kind)}", () => build.SelectTool(kind), build.Tool == kind);
            }

            if (options.Count > 0)
            {
                x = 0;
                y += buttonHeight + 8;
                foreach (var (label, click, selected) in options)
                {
                    Add(label, click, selected);
                }
            }

            var height = y + buttonHeight + gui.Style.Padding * 2;
            var panel = new Rectangle(8, viewport.Bottom - height - 8, maxWidth, height);
            gui.BeginPanel(panel);
            foreach (var (rect, label, click, selected) in cells)
            {
                if (gui.ButtonAt(new Rectangle(panel.X + gui.Style.Padding + rect.X, panel.Y + gui.Style.Padding + rect.Y, rect.Width, rect.Height), label, selected: selected))
                {
                    click();
                }
            }

            gui.EndPanel();
            _ = rows;
        }

        /// <summary>The second toolbar row: the recipe new assemblers get, or the item new input docks pass.</summary>
        private List<(string Label, Action Click, bool Selected)> OptionsFor(BuildingKind tool)
        {
            var condition = Surface.ConditionOf(build.Surface.Kind);
            return tool switch
            {
                BuildingKind.Assembler => [.. Recipes.AssemblerRecipes.Where(r => (Recipes.Get(r).AllowedOn & condition) != 0)
                    .Select(r => ($"{r}##recipe", (Action)(() => build.SelectRecipe(r)), build.Recipe == r))],
                BuildingKind.InputDock => [.. DockItems.Select(i => ($"{i}##dock", (Action)(() => build.SelectDockItem(i)), build.DockItem == i))],
                _ => [],
            };
        }

        private static readonly ItemId[] DockItems = [.. Enum.GetValues<ItemId>().Where(i => i != ItemId.None)];

        private static string ShortName(BuildingKind kind) => kind switch
        {
            BuildingKind.LaunchSilo => "Silo",
            BuildingKind.InputDock => "In dock",
            BuildingKind.OutputDock => "Out dock",
            BuildingKind.SolarPanel => "Solar",
            _ => kind.ToString(),
        };

        // ---------------------------------------------------------------- inspector

        private void Inspector(Rectangle rect)
        {
            if (build.Selected is not { } selected || (uint)selected.Surface >= (uint)universe.Surfaces.Count)
            {
                gui.BeginPanel(rect with { Height = gui.RowHeight * 3 + 8 }, "Inspector");
                gui.Label("Q, then click a building.", gui.Style.TextDim);
                gui.EndPanel();
                return;
            }

            var surface = universe.Surfaces[selected.Surface];
            var slot = surface.Grid.OccupantAt(selected.X, selected.Y);
            if (slot < 0 || !surface.Buildings.IsUsable(slot))
            {
                build.Selected = null;
                return;
            }

            var buildings = surface.Buildings;
            var kind = buildings.KindOf(slot);
            var (ox, oy) = (buildings.OriginX(slot), buildings.OriginY(slot));
            gui.BeginPanel(rect, $"{ShortName(kind)} ({ox},{oy})");
            var network = surface.Power.IndexOf(slot);
            if (Buildings.Spec(kind).ActiveDraw > 0 || kind == BuildingKind.Warehouse)
            {
                var power = network > 0 ? surface.Power.SatisfactionOfIndex(network) / (float)PowerGrid.Full : 0f;
                gui.ProgressBar(power, power >= 0.99f ? gui.Style.Good : power > 0 ? gui.Style.Warning : gui.Style.Bad, network > 0 ? $"power {power:P0}" : "no power");
            }

            switch (kind)
            {
                case BuildingKind.Assembler:
                    AssemblerPanel(surface, ox, oy, buildings.RecipeOf(slot));
                    break;
                case BuildingKind.InputDock:
                    DockPanel(surface, ox, oy, (ItemId)buildings.ConfigOf(slot));
                    break;
                case BuildingKind.Warehouse:
                    WarehousePanel(surface, slot, ox, oy);
                    break;
                case BuildingKind.Shipyard:
                    ShipyardPanel(surface, slot, ox, oy);
                    break;
                case BuildingKind.LaunchSilo:
                    Stacks(surface, slot);
                    gui.Label("Station requests:", gui.Style.TextDim);
                    var wanted = Enumerable.Range(0, Items.Count).Where(i => universe.Requests[i] > 0).Select(i => $"{(ItemId)i} {universe.Requests[i]}").ToArray();
                    gui.Paragraph(wanted.Length == 0 ? "nothing" : string.Join(", ", wanted));
                    break;
                default:
                    Stacks(surface, slot);
                    break;
            }

            gui.EndPanel();
        }

        private void Stacks(Surface surface, int slot, int first = 0, int count = int.MaxValue)
        {
            var start = surface.Buildings.InventoryStart(slot);
            if (start < 0)
            {
                return;
            }

            var store = surface.Inventories;
            var end = start + Math.Min(surface.Buildings.InventoryLength(slot), first + count);
            for (var stack = start + first; stack < end; stack++)
            {
                if (store.ItemAt(stack) != ItemId.None)
                {
                    var limit = store.LimitAt(stack);
                    gui.Value(store.ItemAt(stack).ToString(), limit > 0 ? $"{store.CountAt(stack)}/{limit}" : store.CountAt(stack).ToString());
                }
            }
        }

        private void AssemblerPanel(Surface surface, int x, int y, RecipeId current)
        {
            var condition = Surface.ConditionOf(surface.Kind);
            var recipes = Recipes.AssemblerRecipes.Where(r => (Recipes.Get(r).AllowedOn & condition) != 0).ToArray();
            for (var i = 0; i < recipes.Length; i += 2)
            {
                gui.BeginColumns(2);
                for (var c = i; c < i + 2; c++)
                {
                    if (c < recipes.Length && gui.ButtonAt(gui.NextRow(), $"{recipes[c]}##set", selected: recipes[c] == current))
                    {
                        universe.Enqueue(new Command(CommandKind.SetRecipe, surface.Index, x, y, Recipe: recipes[c]));
                    }
                    else if (c >= recipes.Length)
                    {
                        gui.NextRow();
                    }
                }
            }

            var recipe = Recipes.Get(current);
            gui.Paragraph($"{string.Join(" + ", recipe.Ingredients.Select(n => $"{n.Amount} {n.Item}"))} -> {recipe.ResultAmount} {recipe.Result}, {recipe.Ticks / (float)Universe.TicksPerSecond:0.#}s", gui.Style.TextDim);
        }

        private void DockPanel(Surface surface, int x, int y, ItemId current)
        {
            gui.Label("Passes in:", gui.Style.TextDim);
            for (var i = 0; i < DockItems.Length; i += 2)
            {
                gui.BeginColumns(2);
                for (var c = i; c < i + 2; c++)
                {
                    if (c < DockItems.Length && gui.ButtonAt(gui.NextRow(), $"{DockItems[c]}##filter", selected: DockItems[c] == current))
                    {
                        universe.Enqueue(new Command(CommandKind.SetConfig, surface.Index, x, y, Config: (int)DockItems[c]));
                    }
                    else if (c >= DockItems.Length)
                    {
                        gui.NextRow();
                    }
                }
            }
        }

        private void WarehousePanel(Surface surface, int slot, int x, int y)
        {
            var id = surface.Buildings.ConfigOf(slot);
            var blueprint = (uint)id < (uint)universe.Blueprints.Count ? universe.Blueprints[id] : null;
            gui.Value("Blueprint", blueprint?.Name ?? "none");
            if (blueprint is not null)
            {
                var rating = blueprint.Rating;
                gui.Paragraph($"In/min: {(rating.Inputs.Length == 0 ? "-" : string.Join(", ", rating.Inputs.Select(p => $"{p.Amount} {p.Item}")))}", gui.Style.TextDim);
                gui.Paragraph($"Out/min: {(rating.Outputs.Length == 0 ? "-" : string.Join(", ", rating.Outputs.Select(p => $"{p.Amount} {p.Item}")))}", gui.Style.TextDim);
                gui.Value("Lines (islands)", rating.Islands.Length.ToString());
            }

            var live = universe.LiveInteriorOf(surface.Index, slot) is not null;
            if (gui.Button(live ? "Leave (E)" : "Enter (E)"))
            {
                if (live) build.Leave(); else build.Enter(x, y);
            }

            gui.BeginColumns(2);
            if (gui.ButtonAt(gui.NextRow(), "Copy blueprint", enabled: blueprint is not null))
            {
                universe.Enqueue(new Command(CommandKind.NewBlueprint, surface.Index, x, y, Config: id));
            }

            if (gui.ButtonAt(gui.NextRow(), "New empty"))
            {
                universe.Enqueue(new Command(CommandKind.NewBlueprint, surface.Index, x, y, Config: -1));
            }

            foreach (var other in universe.Blueprints)
            {
                if (gui.Selectable($"{other.Name}##bp{other.Id}", other.Id == id))
                {
                    universe.Enqueue(new Command(CommandKind.SetConfig, surface.Index, x, y, Config: other.Id));
                }
            }

            gui.Separator();
            Stacks(surface, slot);
        }

        private void ShipyardPanel(Surface surface, int slot, int x, int y)
        {
            var order = surface.Buildings.ConfigOf(slot);
            var designIndex = ShipyardOrder.DesignOf(order);
            var mode = ShipyardOrder.ModeOf(order);
            gui.BeginColumns(3);
            foreach (var m in (ShipyardMode[])[ShipyardMode.Off, ShipyardMode.Once, ShipyardMode.Repeat])
            {
                if (gui.ButtonAt(gui.NextRow(), m.ToString(), selected: m == mode))
                {
                    universe.Enqueue(new Command(CommandKind.SetConfig, surface.Index, x, y, Config: ShipyardOrder.Pack(designIndex, m)));
                }
            }

            for (var d = 0; d < universe.Designs.Length; d++)
            {
                if (universe.Designs[d] is not { } design)
                {
                    continue;
                }

                if (gui.Selectable($"{d + 1}. {design.Name}{(design.Stats.IsValid ? "" : " (invalid)")}##yard{d}", d == designIndex, design.Stats.IsValid))
                {
                    universe.Enqueue(new Command(CommandKind.SetConfig, surface.Index, x, y, Config: ShipyardOrder.Pack(d, mode)));
                }
            }

            if (designIndex < universe.Designs.Length && universe.Designs[designIndex] is { } ordered)
            {
                gui.Label("Stock / bill:", gui.Style.TextDim);
                var start = surface.Buildings.InventoryStart(slot);
                foreach (var (item, count) in ordered.Stats.Cost)
                {
                    var have = surface.Inventories.CountAt(start + Array.IndexOf(Buildings.ShipyardStacks, item));
                    gui.Value(item.ToString(), $"{have} / {count}", have >= count ? gui.Style.Good : gui.Style.Warning);
                }

                gui.Value("Build time", $"{ordered.Stats.BuildTicks / (float)Universe.TicksPerSecond:0.#}s");
            }
        }

        // ---------------------------------------------------------------- banner and fleet

        private void Banner(Rectangle viewport)
        {
            if (universe.Live is not { } live)
            {
                return;
            }

            var outside = universe.Surfaces[live.Surface];
            var id = outside.Buildings.ConfigOf(live.Slot);
            var name = (uint)id < (uint)universe.Blueprints.Count ? universe.Blueprints[id].Name : "?";
            var rect = new Rectangle(8, 8, Math.Min(620, viewport.Width - SideWidth - 32), gui.RowHeight * 3 + 16);
            gui.BeginPanel(rect, $"Inside warehouse: {name}  (E to leave)");
            gui.Paragraph("Live: input docks fill from the warehouse's input buffer, output docks empty into its output buffer. Leaving saves this floor as the blueprint and re-rates it; every warehouse using it changes.", gui.Style.TextDim);
            gui.EndPanel();
        }

        private void FleetPanel(Rectangle rect)
        {
            var ships = universe.Space.Snapshot.Count(v => v.Faction == Faction.Player && v.Role == BodyRole.Ship);
            gui.BeginPanel(rect, "Fleet");
            gui.Value("Selected", $"{fleet.Selected.Count} of {ships}");
            gui.Label("Drag to select, right-click to move.", gui.Style.TextDim);
            gui.BeginColumns(2);
            if (gui.ButtonAt(gui.NextRow(), "Select all (A)"))
            {
                fleet.SelectAll();
            }

            if (gui.ButtonAt(gui.NextRow(), "Back to build"))
            {
                Mode = InputMode.Build;
            }

            gui.EndPanel();
        }

        // ---------------------------------------------------------------- designer

        /// <summary>
        /// Cosmoteer's ship editor, simplified: paint parts on a grid, see the stats change, save to a
        /// slot shipyards build from. Crew is replaced by the control budget (KSP probe cores).
        /// </summary>
        private void Designer(Rectangle rect)
        {
            var keyboard = InputManager.Instance.KeyboardInfo;
            if (keyboard.WasKeyJustPressed(Keys.R))
            {
                facing = facing.RotateClockwise();
            }

            gui.BeginPanel(rect, $"Ship designer: {draft.Name}  (B to close)");
            var cell = Math.Min(24, (rect.Height - 80) / ShipDesign.GridSize);
            var grid = new Rectangle(rect.X + 12, rect.Y + gui.RowHeight + 12, cell * ShipDesign.GridSize, cell * ShipDesign.GridSize);
            DesignGrid(grid, cell);

            // Right of the grid: palette, facing, stats, slots.
            var side = new Rectangle(grid.Right + 16, grid.Y, rect.Right - grid.Right - 28, rect.Bottom - grid.Y - 8);
            gui.BeginPanel(side);
            for (var i = 0; i < Parts.All.Length; i += 2)
            {
                gui.BeginColumns(2);
                for (var c = i; c < i + 2; c++)
                {
                    if (c >= Parts.All.Length)
                    {
                        gui.NextRow();
                        continue;
                    }

                    var spec = Parts.All[c];
                    if (gui.ButtonAt(gui.NextRow(), $"{spec.Name}##part", selected: brush == spec.Kind))
                    {
                        brush = spec.Kind;
                    }

                    gui.Tooltip($"{spec.Width}x{spec.Height}, mass {spec.Mass}, hp {spec.Hp}, power {spec.Power:+0;-0;0}, control {spec.Control:+0;-0;0}, cost {string.Join(" + ", spec.Cost.Select(p => $"{p.Count} {p.Item}"))}");
                }
            }

            gui.Value("Facing (R)", facing.ToString());
            var stats = draft.Stats;
            gui.Value("Mass / HP", $"{stats.Mass} / {stats.Hp}");
            gui.Value("Accel fwd/back/side", $"{stats.Acceleration[(int)Direction.North]:0.0}/{stats.Acceleration[(int)Direction.South]:0.0}/{Math.Min(stats.Acceleration[(int)Direction.East], stats.Acceleration[(int)Direction.West]):0.0}");
            gui.Value("Top speed / turn", $"{stats.TopSpeed:0.0} / {stats.AngularAcceleration:0.00}");
            gui.Value("DPS / range", $"{stats.Dps:0} / {stats.Range:0}");
            gui.Value("Power", $"{stats.PowerSupply} / {stats.PowerDraw}", stats.PowerSupply >= stats.PowerDraw ? null : gui.Style.Bad);
            gui.Value("Control", $"{stats.ControlSupply} / {stats.ControlLoad}", stats.ControlSupply >= stats.ControlLoad ? null : gui.Style.Bad);
            gui.Value("Efficiency", $"{stats.Efficiency:P0}", stats.Efficiency >= 1f ? gui.Style.Good : gui.Style.Warning);
            gui.Value("Build time", $"{stats.BuildTicks / (float)Universe.TicksPerSecond:0.#}s");
            gui.Paragraph("Cost: " + string.Join(", ", stats.Cost.Select(p => $"{p.Count} {p.Item}")), gui.Style.TextDim);
            foreach (var error in stats.Errors)
            {
                gui.Paragraph(error, gui.Style.Bad);
            }

            if (stats.BlockedThrusters.Count > 0)
            {
                gui.Paragraph($"{stats.BlockedThrusters.Count} thruster(s) blocked by parts behind them.", gui.Style.Warning);
            }

            gui.Label("Slot (load / save):", gui.Style.TextDim);
            gui.BeginColumns(DesignSlotsShown);
            for (var s = 0; s < DesignSlotsShown; s++)
            {
                if (gui.ButtonAt(gui.NextRow(), $"{s + 1}##slot", selected: s == designSlot))
                {
                    designSlot = s;
                    if (universe.Designs[s] is { } existing)
                    {
                        draft = existing;
                    }
                }

                gui.Tooltip(universe.Designs[s]?.Name ?? "empty");
            }

            gui.BeginColumns(2);
            if (gui.ButtonAt(gui.NextRow(), $"Save to {designSlot + 1}", enabled: stats.IsValid))
            {
                var named = draft.Name.StartsWith("Design ", StringComparison.Ordinal) || universe.Designs[designSlot] is null ? draft.With(name: $"Design {designSlot + 1}") : draft;
                universe.Enqueue(new Command(CommandKind.SaveDesign, -1, 0, 0, Config: designSlot, Payload: named));
                draft = named;
            }

            if (gui.ButtonAt(gui.NextRow(), "Clear"))
            {
                draft = new ShipDesign($"Design {designSlot + 1}", []);
            }

            gui.EndPanel();
            gui.EndPanel();
        }

        private const int DesignSlotsShown = Universe.DesignSlots;

        private void DesignGrid(Rectangle grid, int cell)
        {
            gui.Fill(grid, new Color(10, 12, 18));
            for (var i = 0; i <= ShipDesign.GridSize; i++)
            {
                gui.Fill(new Rectangle(grid.X + i * cell, grid.Y, 1, grid.Height), new Color(30, 36, 48));
                gui.Fill(new Rectangle(grid.X, grid.Y + i * cell, grid.Width, 1), new Color(30, 36, 48));
            }

            for (var p = 0; p < draft.Parts.Count; p++)
            {
                var part = draft.Parts[p];
                var (w, h) = part.Size;
                var r = new Rectangle(grid.X + part.X * cell + 1, grid.Y + part.Y * cell + 1, w * cell - 2, h * cell - 2);
                gui.Fill(r, PartColour(part.Kind));
                gui.Outline(r, draft.Stats.BlockedThrusters.Contains(p) ? gui.Style.Bad : new Color(20, 22, 28));
                if (Parts.Spec(part.Kind).Directional)
                {
                    // A notch on the side it pushes or points towards.
                    var centre = r.Center;
                    var notch = new Point(centre.X + part.Facing.Dx() * (r.Width / 2 - 3) - 2, centre.Y + part.Facing.Dy() * (r.Height / 2 - 3) - 2);
                    gui.Fill(new Rectangle(notch.X, notch.Y, 5, 5), Color.White);
                }
            }

            if (!grid.Contains(gui.MousePosition))
            {
                return;
            }

            var cx = (gui.MousePosition.X - grid.X) / cell;
            var cy = (gui.MousePosition.Y - grid.Y) / cell;
            var candidate = new PartPlacement(brush, cx, cy, Parts.Spec(brush).Directional ? facing : Direction.North);
            var (cw, ch) = candidate.Size;
            var fits = draft.CanAdd(candidate);
            gui.Outline(new Rectangle(grid.X + cx * cell, grid.Y + cy * cell, cw * cell, ch * cell), fits ? gui.Style.Good : gui.Style.Bad);

            var interaction = gui.Interact(grid, "designer-grid");
            if (interaction.Clicked && fits)
            {
                draft = draft.With(parts: [.. draft.Parts, candidate]);
            }
            else if (interaction.RightClicked && draft.PartAt(cx, cy) is var index and >= 0)
            {
                draft = draft.With(parts: draft.Parts.Where((_, i) => i != index));
            }
        }

        private static Color PartColour(PartKind kind) => kind switch
        {
            PartKind.Hull => new Color(150, 156, 166),
            PartKind.Armor => new Color(84, 96, 116),
            PartKind.Core => new Color(110, 196, 255),
            PartKind.Reactor => new Color(120, 230, 255),
            PartKind.Thruster => new Color(232, 124, 60),
            PartKind.Gyro => new Color(214, 170, 52),
            _ => new Color(220, 80, 100),
        };
    }
}
