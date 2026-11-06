using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary.Input;
using System;
using System.Collections.Generic;

namespace MonoGameLibrary.UI
{
    public enum GuiCommandKind : byte
    {
        Fill,
        Outline,
        Text,
    }

    /// <summary>One recorded draw: a filled rectangle, a 1-pixel outline, or a line of text at <see cref="Rect"/>'s corner.</summary>
    public readonly record struct GuiCommand(GuiCommandKind Kind, Rectangle Rect, Color Colour, string? Text = null);

    /// <summary>What happened to one interactive rectangle this frame.</summary>
    public readonly record struct GuiInteraction(bool Hovered, bool Held, bool Clicked, bool RightClicked);

    public sealed class GuiStyle
    {
        public Color PanelBackground = new(18, 22, 30, 232);
        public Color PanelBorder = new(70, 82, 104);
        public Color TitleBackground = new(34, 42, 58);
        public Color Button = new(46, 54, 70);
        public Color ButtonHovered = new(66, 78, 100);
        public Color ButtonHeld = new(90, 108, 140);
        public Color ButtonDisabled = new(32, 36, 44);
        public Color Selected = new(52, 96, 150);
        public Color Text = new(226, 230, 238);
        public Color TextDim = new(132, 142, 158);
        public Color Accent = new(110, 196, 255);
        public Color Good = new(110, 220, 120);
        public Color Warning = new(240, 184, 64);
        public Color Bad = new(232, 84, 72);
        public int Padding = 6;
        public int RowGap = 4;
    }

    /// <summary>
    /// An immediate-mode UI, after Dear ImGui: a widget is a function call that draws itself and
    /// reports what happened (<c>if (gui.Button("Enter")) …</c>). There's no widget tree and no
    /// event wiring - the UI is re-declared from game state every frame.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Frame shape:</b> <see cref="Begin"/> → panels and widgets → <see cref="End"/>, all during
    /// Update, before the game's own world input. Widgets record <see cref="Commands"/>, which
    /// <see cref="GuiRenderer"/> draws in the screen pass. <see cref="WantsMouse"/> is set by End from
    /// this frame's panels, so world input that runs afterwards can skip picking under the UI.
    /// </para>
    /// <para>
    /// <b>Clicks</b> fire on release over the widget the press started on (ImGui's "active id"),
    /// so dragging off a button cancels it, and a press that started on the UI keeps the mouse
    /// captured until release - a drag can't start on a panel and paint buildings in the world.
    /// </para>
    /// <para>
    /// <b>Ids</b> are the label hashed with the enclosing panel's title; append <c>##suffix</c> to a
    /// label to tell identical labels apart (the suffix is not drawn).
    /// </para>
    /// </remarks>
    public sealed class Gui(SpriteFont font)
    {
        private readonly List<GuiCommand> commands = [];
        private readonly List<Rectangle> blockers = [];
        private readonly Stack<Layout> layouts = new();

        private Point mouse;
        private bool leftDown;
        private bool leftPressed;
        private bool leftReleased;
        private bool rightPressed;
        private int activeId;
        private bool pressBeganOnGui;
        private string? tooltip;
        private bool lastWidgetHovered;

        public SpriteFont Font { get; } = font;
        public GuiStyle Style { get; } = new();
        public Rectangle Viewport { get; private set; }
        public int LineHeight => Font.LineSpacing;

        /// <summary>The standard widget row height: one line of text plus padding.</summary>
        public int RowHeight => Font.LineSpacing + Style.Padding;

        /// <summary>True when the cursor is over the UI, or a press that began on it is still held. World input should skip the mouse.</summary>
        public bool WantsMouse { get; private set; }

        public IReadOnlyList<GuiCommand> Commands => commands;

        public void Begin(MouseInfo mouseInfo, Rectangle viewport)
        {
            commands.Clear();
            blockers.Clear();
            layouts.Clear();
            tooltip = null;
            lastWidgetHovered = false;
            Viewport = viewport;

            mouse = mouseInfo.Position;
            leftDown = mouseInfo.IsButtonDown(MouseButton.Left);
            leftPressed = mouseInfo.WasButtonJustPressed(MouseButton.Left);
            leftReleased = mouseInfo.WasButtonJustReleased(MouseButton.Left);
            rightPressed = mouseInfo.WasButtonJustPressed(MouseButton.Right);
        }

        public void End()
        {
            if (layouts.Count != 0)
            {
                throw new InvalidOperationException("Gui.End with a panel still open: every BeginPanel needs an EndPanel.");
            }

            if (tooltip is not null)
            {
                var size = Font.MeasureString(tooltip).ToPoint();
                var rect = new Rectangle(mouse.X + 16, mouse.Y + 16, size.X + Style.Padding * 2, size.Y + Style.Padding);
                // Keep it on screen.
                rect.X = Math.Min(rect.X, Viewport.Right - rect.Width);
                rect.Y = Math.Min(rect.Y, Viewport.Bottom - rect.Height);
                Fill(rect, Style.PanelBackground);
                Outline(rect, Style.Accent);
                Text(new Point(rect.X + Style.Padding, rect.Y + Style.Padding / 2), tooltip, Style.Text);
            }

            var overGui = false;
            foreach (var blocker in blockers)
            {
                overGui |= blocker.Contains(mouse);
            }

            if (leftPressed)
            {
                pressBeganOnGui = overGui;
            }

            WantsMouse = overGui || pressBeganOnGui || activeId != 0;

            if (!leftDown)
            {
                activeId = 0;
                pressBeganOnGui = false;
            }
        }

        // ------------------------------------------------------------------ panels and layout

        /// <summary>Opens a panel: a background box (with a title bar if titled) whose contents lay out top to bottom.</summary>
        public void BeginPanel(Rectangle rect, string? title = null)
        {
            BlockMouse(rect);
            Fill(rect, Style.PanelBackground);
            Outline(rect, Style.PanelBorder);
            var top = rect.Y + Style.Padding;
            if (title is not null)
            {
                var bar = new Rectangle(rect.X, rect.Y, rect.Width, RowHeight);
                Fill(bar, Style.TitleBackground);
                Text(new Point(rect.X + Style.Padding, rect.Y + Style.Padding / 2), title, Style.Accent);
                top = bar.Bottom + Style.Padding;
            }

            var content = new Rectangle(rect.X + Style.Padding, top, rect.Width - Style.Padding * 2, rect.Bottom - top - Style.Padding);
            layouts.Push(new Layout(content, title ?? string.Empty));
        }

        public void EndPanel()
        {
            if (layouts.Count == 0)
            {
                throw new InvalidOperationException("EndPanel without BeginPanel.");
            }

            layouts.Pop();
        }

        /// <summary>Marks a screen area as UI for mouse capture without drawing anything.</summary>
        public void BlockMouse(Rectangle rect) => blockers.Add(rect);

        /// <summary>Takes the next row of the open panel. Inside <see cref="BeginColumns"/>, takes the next column cell instead.</summary>
        public Rectangle NextRow(int? height = null)
        {
            var layout = CurrentLayout();
            var rowHeight = height ?? RowHeight;

            if (layout.ColumnsRemaining > 0)
            {
                var cell = new Rectangle(
                    layout.ColumnRow.X + layout.ColumnIndex * (layout.ColumnWidth + Style.RowGap),
                    layout.ColumnRow.Y, layout.ColumnWidth, layout.ColumnRow.Height);
                layout.ColumnIndex++;
                layout.ColumnsRemaining--;
                return cell;
            }

            var row = new Rectangle(layout.Content.X, layout.CursorY, layout.Content.Width, rowHeight);
            layout.CursorY += rowHeight + Style.RowGap;
            return row;
        }

        /// <summary>The next <paramref name="count"/> widgets share one row, split evenly.</summary>
        public void BeginColumns(int count, int? height = null)
        {
            if (count < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            var row = NextRow(height);
            var layout = CurrentLayout();
            layout.ColumnRow = row;
            layout.ColumnIndex = 0;
            layout.ColumnsRemaining = count;
            layout.ColumnWidth = (row.Width - Style.RowGap * (count - 1)) / count;
        }

        /// <summary>Space left in the open panel below the cursor.</summary>
        public Rectangle Remaining()
        {
            var layout = CurrentLayout();
            return new Rectangle(layout.Content.X, layout.CursorY, layout.Content.Width, Math.Max(0, layout.Content.Bottom - layout.CursorY));
        }

        public void Space(int pixels) => CurrentLayout().CursorY += pixels;

        public void Separator()
        {
            var row = NextRow(1);
            Fill(row, Style.PanelBorder);
        }

        // ------------------------------------------------------------------ widgets

        public void Label(string text, Color? colour = null)
        {
            var row = NextRow();
            Text(new Point(row.X, row.Y + Style.Padding / 2), text, colour ?? Style.Text);
            lastWidgetHovered = row.Contains(mouse);
        }

        /// <summary>A label on the left and a value right-aligned in the same row: stats tables.</summary>
        public void Value(string label, string value, Color? valueColour = null)
        {
            var row = NextRow();
            var y = row.Y + Style.Padding / 2;
            Text(new Point(row.X, y), label, Style.TextDim);
            var width = (int)Font.MeasureString(value).X;
            Text(new Point(row.Right - width, y), value, valueColour ?? Style.Text);
            lastWidgetHovered = row.Contains(mouse);
        }

        /// <summary>Word-wrapped text across the panel's width.</summary>
        public void Paragraph(string text, Color? colour = null)
        {
            var width = CurrentLayout().Content.Width;
            var line = string.Empty;
            foreach (var word in text.Split(' '))
            {
                var candidate = line.Length == 0 ? word : line + " " + word;
                if (line.Length > 0 && Font.MeasureString(candidate).X > width)
                {
                    Label(line, colour);
                    line = word;
                }
                else
                {
                    line = candidate;
                }
            }

            if (line.Length > 0)
            {
                Label(line, colour);
            }
        }

        public bool Button(string label, bool enabled = true) => ButtonAt(NextRow(), label, enabled);

        public bool ButtonAt(Rectangle rect, string label, bool enabled = true, bool selected = false)
        {
            var interaction = enabled ? Interact(rect, label) : default;
            var colour = !enabled ? Style.ButtonDisabled
                : interaction.Held ? Style.ButtonHeld
                : selected ? Style.Selected
                : interaction.Hovered ? Style.ButtonHovered
                : Style.Button;
            Fill(rect, colour);
            if (selected)
            {
                Outline(rect, Style.Accent);
            }

            var shown = Visible(label);
            var size = Font.MeasureString(shown);
            Text(new Point(rect.X + (rect.Width - (int)size.X) / 2, rect.Y + (rect.Height - (int)size.Y) / 2 + 1), shown, enabled ? Style.Text : Style.TextDim);
            lastWidgetHovered = rect.Contains(mouse);
            return interaction.Clicked;
        }

        /// <summary>A button that stays lit while selected: list rows, mode pickers.</summary>
        public bool Selectable(string label, bool selected, bool enabled = true) => ButtonAt(NextRow(), label, enabled, selected);

        /// <summary>A checkbox. Returns true on the frame it changes.</summary>
        public bool Toggle(string label, ref bool value)
        {
            var row = NextRow();
            var interaction = Interact(row, label);
            var box = new Rectangle(row.X, row.Y + (row.Height - LineHeight) / 2, LineHeight, LineHeight);
            Fill(box, interaction.Hovered ? Style.ButtonHovered : Style.Button);
            Outline(box, Style.PanelBorder);
            if (value)
            {
                Fill(new Rectangle(box.X + 3, box.Y + 3, box.Width - 6, box.Height - 6), Style.Accent);
            }

            Text(new Point(box.Right + Style.Padding, row.Y + Style.Padding / 2), Visible(label), Style.Text);
            lastWidgetHovered = interaction.Hovered;
            if (interaction.Clicked)
            {
                value = !value;
                return true;
            }

            return false;
        }

        /// <summary>A bar filled to <paramref name="fraction"/> (clamped to 0..1), with optional centred text.</summary>
        public void ProgressBar(float fraction, Color fill, string? text = null)
        {
            var row = NextRow();
            ProgressBarAt(row, fraction, fill, text);
            lastWidgetHovered = row.Contains(mouse);
        }

        public void ProgressBarAt(Rectangle rect, float fraction, Color fill, string? text = null)
        {
            fraction = float.IsFinite(fraction) ? Math.Clamp(fraction, 0f, 1f) : 0f;
            Fill(rect, Style.Button);
            Fill(new Rectangle(rect.X, rect.Y, (int)(rect.Width * fraction), rect.Height), fill);
            Outline(rect, Style.PanelBorder);
            if (text is not null)
            {
                var size = Font.MeasureString(text);
                Text(new Point(rect.X + (rect.Width - (int)size.X) / 2, rect.Y + (rect.Height - (int)size.Y) / 2 + 1), text, Style.Text);
            }
        }

        /// <summary>Shows <paramref name="text"/> near the cursor if the previous widget is hovered.</summary>
        public void Tooltip(string text)
        {
            if (lastWidgetHovered)
            {
                tooltip = text;
            }
        }

        /// <summary>Shows a tooltip now, whatever is hovered - for things the game hit-tests itself.</summary>
        public void ShowTooltip(string text) => tooltip = text;

        // ------------------------------------------------------------------ raw interaction and drawing

        /// <summary>Hit-tests a rectangle as a widget with id <paramref name="id"/>: the building block for custom widgets (grids, swatches).</summary>
        public GuiInteraction Interact(Rectangle rect, string id)
        {
            var key = HashCode.Combine(layouts.Count > 0 ? layouts.Peek().Scope : string.Empty, id);
            if (key == 0)
            {
                key = 1;
            }

            var hovered = rect.Contains(mouse);
            if (hovered && leftPressed)
            {
                activeId = key;
            }

            var held = activeId == key && leftDown;
            var clicked = leftReleased && activeId == key && hovered;
            return new GuiInteraction(hovered, held, clicked, hovered && rightPressed);
        }

        /// <summary>Whether the cursor is in <paramref name="rect"/>, with no click handling.</summary>
        public bool IsHovered(Rectangle rect) => rect.Contains(mouse);

        public Point MousePosition => mouse;
        public bool LeftDown => leftDown;

        public void Fill(Rectangle rect, Color colour) => commands.Add(new GuiCommand(GuiCommandKind.Fill, rect, colour));

        public void Outline(Rectangle rect, Color colour) => commands.Add(new GuiCommand(GuiCommandKind.Outline, rect, colour));

        public void Text(Point position, string text, Color colour) =>
            commands.Add(new GuiCommand(GuiCommandKind.Text, new Rectangle(position, Point.Zero), colour, text));

        public int MeasureWidth(string text) => (int)Font.MeasureString(text).X;

        private static string Visible(string label)
        {
            var hidden = label.IndexOf("##", StringComparison.Ordinal);
            return hidden < 0 ? label : label[..hidden];
        }

        private Layout CurrentLayout() =>
            layouts.Count > 0 ? layouts.Peek() : throw new InvalidOperationException("Layout widgets need an open panel (BeginPanel).");

        private sealed class Layout(Rectangle content, string scope)
        {
            public Rectangle Content = content;
            public string Scope = scope;
            public int CursorY = content.Y;
            public Rectangle ColumnRow;
            public int ColumnIndex;
            public int ColumnsRemaining;
            public int ColumnWidth;
        }
    }
}
