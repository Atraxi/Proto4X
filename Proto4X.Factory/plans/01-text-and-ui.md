# Plan 1 — Text and basic UI

Independent of plans 2 and 3. Engine-level (`MonoGameLibrary/UI/`), because every game on the
engine wants it and none of it is factory-specific. Written on the `deep-dive-prototype-spike`
experiment branch, where engine edits are in scope; each engine piece is its own commit so it can
be lifted into main by hand, or dropped.

## What "basic UI" has to cover for the prototype

Derived from the screens plan 3 needs, not from a generic widget wishlist:

| Need | Used by |
|---|---|
| Text at a fixed pixel size, measured for layout | every panel, numbers in the HUD, wave timer |
| Panels (a box with a title) | warehouse panel, shipyard panel, dock filter, design editor |
| Buttons that report a click | "Enter", "Save design", design slots, recipe pickers |
| Toggles | shipyard "repeat", peaceful mode |
| Progress bars | build progress, wave countdown, power satisfaction |
| A list you pick one row from | design list, item filter, blueprint list |
| Tooltips | part stats in the designer, building under cursor |
| **Mouse capture** | a click on a panel must never also place a building underneath it |
| Keyboard capture | not needed yet: there is no text entry. Designs get default names. |

Text entry, scrolling regions, drag-and-drop, docking and theming are out of scope for the first
cut. Each would be a self-contained addition later.

## Decisions

### Text: a runtime-built pixel font, served through MonoGame's own `SpriteFont`

Options considered:

1. **Content-pipeline `SpriteFont`** (`.spritefont` XML naming a TTF, compiled by MGCB). The usual
   MonoGame answer. It needs a TTF in the repo or a system font name, which ties the build to the
   machine's installed fonts and brings a font licence into a project that has deliberately shipped
   no art.
2. **FontStashSharp** (TTF rasterised at runtime, any size). Best quality, and the de facto library
   for this. It's a new dependency plus a TTF, so the same licence question applies, and it's more
   than a prototype needs.
3. **A hand-authored 5×7 pixel font, rasterised into a texture at load and wrapped in a real
   `SpriteFont`** via MonoGame's public constructor. **Chosen.**

Why 3: there's no content step, no licence, and no dependency. `SpriteBatch.DrawString` and
`SpriteFont.MeasureString` work unchanged, so a later swap to option 1 or 2 is local to font
creation. It renders crisp at integer scales (×2 is the UI default). The glyph data is authored here
as readable `#`/`.` rows, so it is original and can be checked by eye.

Limits, accepted for now: one size (scaled in whole steps), uppercase only (lowercase maps to
uppercase, the classic pixel-font look), ASCII only. Upgrade path: swap `PixelFont.Create` for
FontStashSharp once real typography matters.

### UI: immediate mode

Copied from **Dear ImGui** (and Unity's IMGUI). The widget *is* the function call: `if
(ui.Button("Enter")) …`. There's no retained widget tree, no event wiring, and no object lifetime to
manage, so the UI is always a pure function of game state. That fits an ECS game, where the state
lives in the sim and the UI only reads it and issues commands.

Shape:

```
Gui (engine, MonoGameLibrary/UI/Gui.cs)
  Begin(mouse, keyboard, viewport)   once per frame, before any game input handling
  Panel(rect, title) / EndPanel()    a box; pushes a layout cursor
  Label / Button / Toggle / ProgressBar / Selectable / Tooltip
  Row(height) / Columns(n)           minimal layout: a vertical cursor, optional equal columns
  End()                              records WantsMouse for this frame
  WantsMouse                         true if the cursor is over any panel → game input skips picking
  DrawCommands                       a flat list (rect | text | outline), drawn by GuiRenderer
GuiRenderer (engine, IDrawSystem, DrawSpace.Screen)
  draws the command list with SpriteBatch + a 1×1 pixel + the font
```

**Mouse capture** uses the standard immediate-mode trick: panel rects recorded *last* frame decide
`WantsMouse` *this* frame, so game input can test it before the UI code for this frame has run.
That's one frame of latency on a panel appearing under a stationary cursor, which is invisible in
practice. Clicks fire on **release** inside the same widget the press started on (ImGui's "active
id"), so dragging off a button cancels it.

**Ids**: a widget's id is a hash of its label plus the panel it sits in, with `##suffix` to
disambiguate identical labels, which is ImGui's own convention.

**Drawing**: the UI code runs in `Update`, where input is, and records commands. Drawing happens in
the screen pass. Recording lets UI logic run where the game's input handling runs, so a click and
the command it causes land in the same update.

## Engine pieces (each a separate commit)

1. `MonoGameLibrary/UI/PixelFont.cs`: glyph table plus `Create(GraphicsDevice) : SpriteFont`.
   It's pure data plus a rasteriser, and the rasteriser's CPU half (`RasterizeAtlas`) is
   device-free, so a headless check can verify every glyph is present and non-empty.
2. `MonoGameLibrary/UI/Gui.cs`: the immediate-mode core described above.
3. `MonoGameLibrary/UI/GuiRenderer.cs`: the screen-pass draw system.

## Game integration (Factory)

- `FactoryGame` owns one `Gui`. Each update: `gui.Begin` → screen panels (HUD, the selected
  building's panel, the designer) → `gui.End`. Then world input runs, gated on `!gui.WantsMouse`.
- The existing toolbar swatches become buttons with hotkey labels. The window-title status line is
  replaced by an on-screen readout; the title keeps only fps and tick for debugging.

## Verification

- Headless: the atlas rasterises, every printable ASCII character maps to a glyph (lowercase through
  uppercase), and the measured width of a string equals the sum of its advances.
- Not verifiable headless: anything on screen. Needs a playtest.

## Later, not now

Text entry (design names), scroll views (long design lists), a second font size, layout
auto-sizing, gamepad navigation.
