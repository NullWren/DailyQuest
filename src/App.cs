using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Threading;
using Microsoft.Win32;

namespace DailyQuest
{
    public static class Program
    {
        [STAThread] public static void Main(string[] args)
        {
            Storage storage = null;
            try
            {
                string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DailyQuest");
                if (args.Length == 2 && args[0] == "--data-dir") folder = Path.GetFullPath(args[1]);
                storage = new Storage(folder);
                var journal = storage.Load();
                var app = new Application();
                using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("DailyQuest.Theme.xaml")) app.Resources = (ResourceDictionary)XamlReader.Load(stream);
                app.DispatcherUnhandledException += delegate(object sender, DispatcherUnhandledExceptionEventArgs e)
                {
                    MessageBox.Show("The action could not be completed.\n\n" + e.Exception.Message, "WrenFeed", MessageBoxButton.OK, MessageBoxImage.Error);
                    e.Handled = true;
                };
                app.Run(new QuestWindow(storage, journal));
            }
            catch (Exception error) { MessageBox.Show(error.Message, "WrenFeed could not open", MessageBoxButton.OK, MessageBoxImage.Warning); }
            finally { if (storage != null) storage.Dispose(); }
        }
    }
    public static class UI
    {
        public static Brush Brush(string key) { return ThemeManager.Brush(key); }
        public static void Display(TextBlock text) { text.SetResourceReference(TextBlock.FontFamilyProperty, "DisplayFont"); }
        public static TextBlock Text(string text, double size, string color)
        {
            return new TextBlock { Text = text, FontSize = size, Foreground = Brush(color), TextWrapping = TextWrapping.Wrap };
        }
        public static Button Button(string text, Action action)
        {
            var b = new Button { Content = text }; AutomationProperties.SetName(b, text);
            b.Click += delegate { action(); }; return b;
        }
        public static void Primary(Button button) { button.Background = Brush("Accent"); button.Foreground = Brush("Canvas"); button.BorderBrush = Brush("Accent"); button.FontWeight = FontWeights.SemiBold; }
        public static StackPanel Stack() { return new StackPanel(); }
        public static TextBlock Label(string text) { var label = Text(text, 12, "Muted"); label.Margin = new Thickness(0, 16, 0, 7); return label; }
        public static TextBox Field(string text, string name, int max)
        {
            var field = new TextBox { Text = text ?? "", MaxLength = max }; AutomationProperties.SetName(field, name); return field;
        }
        public static Border Card(UIElement child)
        {
            var decoration = ThemeManager.Current.Decoration;
            var card = new Border { Background = Brush("Panel"), BorderBrush = Brush("Edge"), BorderThickness = decoration == "graffiti" ? new Thickness(3, 1, 1, 3) : decoration == "sketch" ? new Thickness(2, 1, 2, 2) : new Thickness(decoration == "filigree" || decoration == "manga" || decoration == "pixel" ? 2 : 1), CornerRadius = new CornerRadius(decoration == "none" ? 8 : decoration == "circuit" ? 4 : decoration == "manga" || decoration == "graffiti" ? 2 : decoration == "sketch" || decoration == "pixel" ? 0 : decoration == "chain" ? 6 : decoration == "whiskers" ? 14 : decoration == "flock" ? 12 : 3), Padding = new Thickness(14) };
            if (decoration == "none") { card.Child = child; return card; }
            var panel = new StackPanel();
            var trim = new DockPanel { Margin = new Thickness(0, -5, 0, 10) };
            if (decoration == "graffiti" || decoration == "sketch")
            {
                trim.Children.Add(decoration == "graffiti" ? GraffitiTrim() : SketchTrim());
                panel.Children.Add(trim); panel.Children.Add(child); card.Child = panel; return card;
            }
            string trimText = decoration == "circuit" ? "◇ ── ◇ ─────" : decoration == "manga" ? "╱╱  ✦  ╲╲" : decoration == "graffiti" ? "✦  ╱╱  ◇  ╲╲  ✦" : decoration == "flock" ? "✦ · ✦ · ✦" : decoration == "chain" ? "◇ · ◇ · ◇" : decoration == "whiskers" ? "✧  ·  ✧" : decoration == "pixel" ? "▪ ▪ ▪  ■" : "✦  ───  ✦  ───";
            var mark = Text(trimText, 11, "Accent"); mark.Opacity = 0.8; mark.HorizontalAlignment = HorizontalAlignment.Right;
            trim.Children.Add(mark); panel.Children.Add(trim); panel.Children.Add(child); card.Child = panel;
            return card;
        }
        public static Canvas GraffitiTrim()
        {
            var trim = new Canvas { Width = 112, Height = 17, HorizontalAlignment = HorizontalAlignment.Right, IsHitTestVisible = false };
            trim.Children.Add(new System.Windows.Shapes.Path { Data = Geometry.Parse("M 1,13 L 27,4 L 48,11 L 79,2 L 110,8 M 3,16 L 30,9 L 50,15"), Stroke = Brush("Ink"), StrokeThickness = 2.1, Opacity = 0.78, StrokeLineJoin = PenLineJoin.Bevel });
            trim.Children.Add(new System.Windows.Shapes.Path { Data = Geometry.Parse("M 13,14 L 38,6 L 58,13 M 69,10 L 98,3"), Stroke = new SolidColorBrush(Color.FromRgb(67, 96, 204)), StrokeThickness = 3.2, Opacity = 0.85 });
            trim.Children.Add(new System.Windows.Shapes.Path { Data = Geometry.Parse("M 30,2 L 22,15 M 54,1 L 48,12 M 88,4 L 82,15"), Stroke = Brush("Accent"), StrokeThickness = 2.4 });
            foreach (var dot in new[] { new Point(3, 2), new Point(7, 6), new Point(103, 14), new Point(107, 12) }) trim.Children.Add(new System.Windows.Shapes.Ellipse { Width = 2.5, Height = 2.5, Fill = Brush("Success"), Margin = new Thickness(dot.X, dot.Y, 0, 0) });
            return trim;
        }
        public static Canvas SketchTrim()
        {
            var trim = new Canvas { Width = 105, Height = 16, HorizontalAlignment = HorizontalAlignment.Right, IsHitTestVisible = false };
            trim.Children.Add(new System.Windows.Shapes.Path { Data = Geometry.Parse("M 2,3 L 103,4 M 1,8 L 101,10 M 3,12 L 83,13 M 10,1 L 1,14 M 23,1 L 13,15 M 37,2 L 27,15 M 50,2 L 40,15 M 64,2 L 54,15 M 77,2 L 67,15 M 91,2 L 81,15"), Stroke = Brush("Accent"), StrokeThickness = 1, Opacity = 0.72 });
            return trim;
        }
        public static DrawingBrush TexturedWall(string decoration, double opacity)
        {
            var background = CurrentColor("Canvas");
            var ink = CurrentColor("Ink");
            var accent = CurrentColor("Accent");
            bool graffiti = decoration == "graffiti";
            double wallWidth = graffiti ? 450 : 240, wallHeight = graffiti ? 720 : 240;
            var group = new DrawingGroup { ClipGeometry = new RectangleGeometry(new Rect(0, 0, wallWidth, wallHeight)) };
            group.Children.Add(new GeometryDrawing(new SolidColorBrush(background), null, new RectangleGeometry(new Rect(0, 0, wallWidth, wallHeight))));
            Func<Color, byte, Brush> wash = delegate(Color color, byte alpha) { return new SolidColorBrush(Color.FromArgb(alpha, color.R, color.G, color.B)); };
            if (graffiti)
            {
                group.Children.Add(new GeometryDrawing(null, new Pen(wash(ink, 57), 1.8), Geometry.Parse("M -9,28 L 187,4 M 2,38 L 136,15 M 329,15 L 455,43 M 316,25 L 446,54 M -4,284 L 94,247 M 343,301 L 456,260 M 35,423 L 183,398 M 299,454 L 451,421 M 13,664 L 196,613 M 277,706 L 454,652")));
                group.Children.Add(new GeometryDrawing(null, new Pen(wash(ink, 87), 4.2), Geometry.Parse("M -6,148 Q 39,83 59,137 Q 78,174 124,91 M 5,162 Q 40,102 57,148 L 131,105 M 294,119 Q 339,61 365,119 Q 382,147 442,80 M 285,133 Q 337,77 363,131 L 449,95 M 16,506 Q 61,451 98,495 Q 118,524 173,461 M 7,526 Q 56,467 95,510 L 180,479 M 226,600 Q 280,549 310,600 Q 339,631 447,554 M 232,614 Q 279,561 309,611 L 454,568")));
                group.Children.Add(new GeometryDrawing(null, new Pen(wash(accent, 66), 4.4), Geometry.Parse("M 19,182 L 68,165 L 84,178 M 354,171 L 428,151 M 38,542 L 96,526 M 265,646 L 329,624 M 183,329 L 222,315")));
                group.Children.Add(new GeometryDrawing(null, new Pen(wash(Color.FromRgb(67, 96, 204), 97), 3.8), Geometry.Parse("M 2,174 L 71,145 M 318,159 L 445,118 M 15,531 L 119,485 M 265,626 L 394,572 M 191,342 L 236,323")));
                group.Children.Add(new GeometryDrawing(null, new Pen(wash(Color.FromRgb(221, 73, 153), 85), 2.7), Geometry.Parse("M 110,117 L 158,98 M 116,127 L 177,102 M 288,150 L 332,128 M 176,482 L 221,461 M 347,617 L 411,591")));
            }
            else
            {
                group.Children.Add(new GeometryDrawing(null, new Pen(wash(ink, 30), 1), Geometry.Parse("M 0,0 L 57,57 M 0,13 L 46,59 M 0,27 L 32,59 M 180,185 L 239,244 M 194,181 L 242,229 M 210,183 L 244,217 M 4,225 L 28,204 M 6,231 L 37,201")));
                group.Children.Add(new GeometryDrawing(null, new Pen(wash(ink, 20), 1), Geometry.Parse("M 0,58 L 59,0 M 0,45 L 45,0 M 182,241 L 240,183 M 197,241 L 241,197")));
            }
            var specks = new GeometryGroup();
            for (int i = 0; i < (graffiti ? 100 : 25); i++) specks.Children.Add(new EllipseGeometry(new Point((i * 67 + 13) % (int)wallWidth, (i * 97 + 21) % (int)wallHeight), i % 6 == 0 ? 1.6 : 0.7, i % 6 == 0 ? 1.6 : 0.7));
            group.Children.Add(new GeometryDrawing(wash(ink, graffiti ? (byte)58 : (byte)25), null, specks));
            return graffiti
                ? new DrawingBrush(group) { TileMode = TileMode.None, ViewportUnits = BrushMappingMode.RelativeToBoundingBox, Viewport = new Rect(0, 0, 1, 1), ViewboxUnits = BrushMappingMode.Absolute, Viewbox = new Rect(0, 0, wallWidth, wallHeight), Stretch = Stretch.Fill, Opacity = opacity }
                : new DrawingBrush(group) { TileMode = TileMode.Tile, ViewportUnits = BrushMappingMode.Absolute, Viewport = new Rect(0, 0, wallWidth, wallHeight), ViewboxUnits = BrushMappingMode.Absolute, Viewbox = new Rect(0, 0, wallWidth, wallHeight), Stretch = Stretch.Fill, Opacity = opacity };
        }
        static Color CurrentColor(string key) { return ThemeManager.Current.Colors[key]; }
        public static Canvas CuteWren()
        {
            var bird = new Canvas { Width = 43, Height = 33, Margin = new Thickness(0, 0, 10, 0), ToolTip = "A little wren keeping watch" };
            Action<System.Windows.Shapes.Shape, double, double> add = delegate(System.Windows.Shapes.Shape shape, double left, double top) { Canvas.SetLeft(shape, left); Canvas.SetTop(shape, top); bird.Children.Add(shape); };
            add(new System.Windows.Shapes.Path { Data = Geometry.Parse("M 12,20 L 1,11 L 5,23 L 14,26 Z"), Fill = Brush("Accent") }, 0, 0);
            add(new System.Windows.Shapes.Path { Data = Geometry.Parse("M 20,25 L 18,30 M 27,24 L 29,30 M 9,30 L 36,30"), Stroke = Brush("Ink"), StrokeThickness = 1.4, StrokeStartLineCap = PenLineCap.Round, StrokeEndLineCap = PenLineCap.Round }, 0, 0);
            add(new System.Windows.Shapes.Ellipse { Width = 26, Height = 19, Fill = Brush("Accent") }, 10, 10);
            add(new System.Windows.Shapes.Ellipse { Width = 18, Height = 14, Fill = Brush("Panel") }, 15, 14);
            add(new System.Windows.Shapes.Ellipse { Width = 14, Height = 10, Fill = Brush("Success"), Opacity = 0.8 }, 11, 13);
            add(new System.Windows.Shapes.Ellipse { Width = 17, Height = 17, Fill = Brush("Accent") }, 23, 3);
            add(new System.Windows.Shapes.Path { Data = Geometry.Parse("M 36,11 L 43,14 L 36,16 Z"), Fill = Brush("Success") }, 0, 0);
            add(new System.Windows.Shapes.Ellipse { Width = 2.8, Height = 2.8, Fill = Brush("Ink") }, 33, 9);
            return bird;
        }
        public static Canvas ChainWren()
        {
            var bird = CuteWren(); bird.ToolTip = "A wren with a tiny G medallion";
            bird.Children.Add(new System.Windows.Shapes.Path { Data = Geometry.Parse("M 15,24 Q 23,32 32,23"), Stroke = Brush("Ink"), StrokeThickness = 1.5, Fill = Brushes.Transparent });
            var pendant = new System.Windows.Shapes.Ellipse { Width = 10, Height = 10, Fill = Brush("Accent"), Stroke = Brush("Ink"), StrokeThickness = 1 };
            Canvas.SetLeft(pendant, 19); Canvas.SetTop(pendant, 25); bird.Children.Add(pendant);
            var letter = Text("G", 8, "Canvas"); letter.FontWeight = FontWeights.Bold; Canvas.SetLeft(letter, 21); Canvas.SetTop(letter, 24); bird.Children.Add(letter);
            return bird;
        }
        public static Canvas Sphynx()
        {
            var cat = new Canvas { Width = 45, Height = 36, Margin = new Thickness(0, 0, 9, 0), ToolTip = "A small sphynx cat watching the quests" };
            cat.Children.Add(new System.Windows.Shapes.Path { Data = Geometry.Parse("M 6,24 L 5,2 L 15,12 Q 22,8 30,12 L 40,2 L 39,24 Q 34,34 23,34 Q 11,34 6,24 Z"), Fill = Brush("Accent"), Stroke = Brush("Ink"), StrokeThickness = 1.3, StrokeLineJoin = PenLineJoin.Round });
            cat.Children.Add(new System.Windows.Shapes.Path { Data = Geometry.Parse("M 9,7 L 11,17 L 16,14 Z M 36,7 L 34,17 L 29,14 Z"), Fill = Brush("Panel") });
            cat.Children.Add(new System.Windows.Shapes.Path { Data = Geometry.Parse("M 11,20 Q 16,16 20,21 M 27,21 Q 31,16 36,20 M 15,13 Q 22,10 30,13 M 17,27 L 11,27 M 29,27 L 35,27"), Stroke = Brush("Ink"), StrokeThickness = 1.2, Fill = Brushes.Transparent, StrokeStartLineCap = PenLineCap.Round, StrokeEndLineCap = PenLineCap.Round });
            cat.Children.Add(new System.Windows.Shapes.Path { Data = Geometry.Parse("M 20,25 L 26,25 L 23,28 Z"), Fill = Brush("Panel") });
            return cat;
        }
        public static Canvas MangaEye()
        {
            var eye = new Canvas { Width = 48, Height = 34, Margin = new Thickness(0, 0, 8, 0), ToolTip = "An original ink-and-speed-line emblem" };
            eye.Children.Add(new System.Windows.Shapes.Path { Data = Geometry.Parse("M 1,18 Q 22,-1 45,18 Q 22,36 1,18 Z"), Fill = Brush("Panel"), Stroke = Brush("Accent"), StrokeThickness = 2 });
            eye.Children.Add(new System.Windows.Shapes.Ellipse { Width = 17, Height = 17, Fill = Brush("Accent"), Margin = new Thickness(16, 9, 0, 0) });
            eye.Children.Add(new System.Windows.Shapes.Ellipse { Width = 7, Height = 7, Fill = Brush("Canvas"), Margin = new Thickness(21, 14, 0, 0) });
            eye.Children.Add(new System.Windows.Shapes.Path { Data = Geometry.Parse("M 0,4 L 12,8 M 39,6 L 48,2 M 2,31 L 14,27 M 37,28 L 47,32"), Stroke = Brush("Accent"), StrokeThickness = 1.4 });
            return eye;
        }
        public static Canvas PixelWren()
        {
            var bird = new Canvas { Width = 43, Height = 35, Margin = new Thickness(0, 0, 10, 0), ToolTip = "A tiny pixel wren" };
            Action<int, int, int, int, string> block = delegate(int x, int y, int w, int h, string color)
            {
                var pixel = new System.Windows.Shapes.Rectangle { Width = w, Height = h, Fill = Brush(color), SnapsToDevicePixels = true };
                Canvas.SetLeft(pixel, x); Canvas.SetTop(pixel, y); bird.Children.Add(pixel);
            };
            block(3, 20, 8, 4, "Accent"); block(8, 16, 22, 12, "Accent"); block(14, 12, 18, 12, "Accent"); block(23, 7, 13, 13, "Accent");
            block(34, 13, 8, 4, "Success"); block(28, 11, 4, 4, "Ink"); block(14, 21, 11, 5, "Success"); block(14, 28, 3, 5, "Ink"); block(26, 28, 3, 5, "Ink");
            return bird;
        }
        public static Canvas GraffitiWing()
        {
            var mark = new Canvas { Width = 49, Height = 36, Margin = new Thickness(0, 0, 7, 0), ToolTip = "An original inked graffiti-wing emblem" };
            mark.Children.Add(new System.Windows.Shapes.Path { Data = Geometry.Parse("M 3,29 Q 19,25 25,9 L 43,4 L 35,20 L 45,17 L 28,33 L 18,29 Z"), Fill = new SolidColorBrush(Color.FromRgb(221, 73, 153)) });
            mark.Children.Add(new System.Windows.Shapes.Path { Data = Geometry.Parse("M 2,24 Q 17,22 24,6 L 42,2 L 31,17 L 40,15 L 24,28 L 16,24 Z"), Fill = Brush("Accent"), Stroke = Brush("Ink"), StrokeThickness = 1.4, StrokeLineJoin = PenLineJoin.Bevel });
            mark.Children.Add(new System.Windows.Shapes.Path { Data = Geometry.Parse("M 6,22 L 23,20 M 15,17 L 33,12 M 25,25 L 36,20"), Stroke = new SolidColorBrush(Color.FromRgb(55, 83, 190)), StrokeThickness = 2.3, StrokeStartLineCap = PenLineCap.Square });
            mark.Children.Add(new System.Windows.Shapes.Path { Data = Geometry.Parse("M 1,9 L 4,11 L 2,15 M 43,24 L 47,25 M 39,31 L 45,33"), Stroke = Brush("Success"), StrokeThickness = 1.8 });
            mark.Children.Add(new System.Windows.Shapes.Ellipse { Width = 2, Height = 2, Fill = Brush("Ink"), Margin = new Thickness(12, 2, 0, 0) });
            mark.Children.Add(new System.Windows.Shapes.Ellipse { Width = 2.5, Height = 2.5, Fill = Brush("Ink"), Margin = new Thickness(45, 10, 0, 0) });
            return mark;
        }
        public static Canvas InkWren()
        {
            var bird = new Canvas { Width = 51, Height = 37, Margin = new Thickness(0, 0, 5, 0), ToolTip = "An original hand-inked wren emblem" };
            bird.Children.Add(new System.Windows.Shapes.Path { Data = Geometry.Parse("M 11,24 L 1,11 L 4,26 L 14,28 Z"), Fill = Brush("Panel"), Stroke = Brush("Accent"), StrokeThickness = 1.6 });
            bird.Children.Add(new System.Windows.Shapes.Path { Data = Geometry.Parse("M 10,22 Q 14,13 25,13 Q 32,9 37,14 Q 44,20 37,27 Q 24,33 12,27 Z"), Fill = Brush("Panel"), Stroke = Brush("Accent"), StrokeThickness = 1.8, StrokeLineJoin = PenLineJoin.Bevel });
            bird.Children.Add(new System.Windows.Shapes.Path { Data = Geometry.Parse("M 21,15 Q 25,3 35,5 Q 42,7 42,16 L 37,20 Q 31,16 21,15 Z"), Fill = Brush("Panel"), Stroke = Brush("Accent"), StrokeThickness = 1.7 });
            bird.Children.Add(new System.Windows.Shapes.Path { Data = Geometry.Parse("M 42,12 L 50,15 L 42,17 Z"), Fill = Brush("Accent") });
            bird.Children.Add(new System.Windows.Shapes.Path { Data = Geometry.Parse("M 16,21 L 30,17 M 15,25 L 34,20 M 19,28 L 35,23 M 18,19 L 23,26 M 23,17 L 28,26 M 28,16 L 33,25 M 18,33 L 21,28 M 29,33 L 31,28 M 11,34 L 40,34"), Stroke = Brush("Accent"), StrokeThickness = 1.1, StrokeStartLineCap = PenLineCap.Round });
            bird.Children.Add(new System.Windows.Shapes.Ellipse { Width = 3, Height = 3, Fill = Brush("Accent"), Margin = new Thickness(35, 10, 0, 0) });
            bird.Children.Add(new System.Windows.Shapes.Path { Data = Geometry.Parse("M 1,5 L 8,12 M 46,2 L 43,8 M 45,30 L 49,34"), Stroke = Brush("Edge"), StrokeThickness = 1.2 });
            return bird;
        }
        public static Canvas Flock()
        {
            var friends = new Canvas { Width = 113, Height = 36, Margin = new Thickness(0, 0, 5, 0), ToolTip = "The flock: G-Unit wren, red wren, and sphynx cat" };
            friends.Children.Add(new System.Windows.Shapes.Path { Data = Geometry.Parse("M 1,32 Q 55,35 111,32"), Stroke = Brush("Success"), StrokeThickness = 1.3, Opacity = 0.8 });
            var grant = ChainWren(); grant.RenderTransform = new ScaleTransform(0.76, 0.76); Canvas.SetLeft(grant, 0); Canvas.SetTop(grant, 3); friends.Children.Add(grant);
            var red = CuteWren();
            var redBrush = new SolidColorBrush(Color.FromRgb(210, 83, 88));
            foreach (var shape in red.Children.OfType<System.Windows.Shapes.Shape>()) if (Object.ReferenceEquals(shape.Fill, Brush("Accent"))) shape.Fill = redBrush;
            red.RenderTransform = new ScaleTransform(0.76, 0.76); Canvas.SetLeft(red, 35); Canvas.SetTop(red, 3); friends.Children.Add(red);
            var cat = Sphynx(); cat.RenderTransform = new ScaleTransform(0.76, 0.76); Canvas.SetLeft(cat, 72); Canvas.SetTop(cat, 1); friends.Children.Add(cat);
            return friends;
        }
        public static void SetupDialog(Window dialog, Window owner, string title, double width, double height)
        {
            dialog.SetResourceReference(FrameworkElement.StyleProperty, typeof(Window));
            dialog.Owner = owner; dialog.Title = title; dialog.Width = width; dialog.Height = Math.Min(height, SystemParameters.WorkArea.Height - 50);
            dialog.MinWidth = Math.Min(width, 380); dialog.MinHeight = 350; dialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            dialog.ShowInTaskbar = false; dialog.ResizeMode = ResizeMode.NoResize;
            dialog.StateChanged += delegate { if (dialog.WindowState == WindowState.Minimized) dialog.WindowState = WindowState.Normal; };
            dialog.PreviewKeyDown += delegate(object s, KeyEventArgs e) { if (e.Key == Key.Escape) dialog.Close(); };
        }
    }
    public sealed class QuestWindow : Window
    {
        readonly Storage storage;
        readonly ThemeManager themes;
        Journal journal;
        readonly DockPanel root = new DockPanel();
        readonly Border shell = new Border();
        readonly StackPanel cards = new StackPanel();
        readonly TextBlock heading = UI.Text("", 24, "Ink");
        readonly TextBlock summary = UI.Text("", 12, "Muted");
        readonly TextBlock footer = UI.Text("Saved on this device · resets at midnight", 11, "Muted");
        readonly TextBlock brand = UI.Text("◇  W R E N F E E D", 11, "Accent");
        readonly TextBlock headerOrnament = UI.Text("", 11, "Accent");
        readonly Canvas wrenBadge = new Canvas { Width = 52, Height = 36 };
        readonly Border fill = new Border { Height = 3, HorizontalAlignment = HorizontalAlignment.Left };
        readonly Border track = new Border { Height = 3 };
        readonly StackPanel dateArea = new StackPanel();
        readonly Button todayButton;
        readonly Button weeklyButton;
        readonly Button historyButton;
        readonly Button addButton;
        readonly DispatcherTimer clock = new DispatcherTimer();
        string activeDate;
        bool history;
        bool weekly;
        DateTime historyDate = DateTime.Today;
        double fraction;
        public QuestWindow(Storage storage, Journal journal)
        {
            SetResourceReference(FrameworkElement.StyleProperty, typeof(Window));
            this.storage = storage; this.journal = journal;
            themes = new ThemeManager(storage.Folder);
            ThemeManager.Apply(themes.Find(journal.Settings.Theme));
            Title = "WrenFeed";
            Width = Math.Max(350, Math.Min(journal.Settings.Width, SystemParameters.WorkArea.Width));
            Height = Math.Max(440, Math.Min(journal.Settings.Height, SystemParameters.WorkArea.Height - 35));
            MinWidth = 350; MinHeight = 440; WindowStartupLocation = WindowStartupLocation.CenterScreen;
            Topmost = journal.Settings.AlwaysOnTop;
            WindowStyle = WindowStyle.None; AllowsTransparency = true; Background = Brushes.Transparent;
            BuildChrome();
            ApplyWindowOpacity();
            root.Margin = new Thickness(14, 12, 14, 10);
            var top = UI.Stack(); DockPanel.SetDock(top, Dock.Top); root.Children.Add(top);
            var masthead = new DockPanel();
            var settings = UI.Button("⚙", OpenSettings); settings.FontSize = 17; settings.Width = 34; settings.Padding = new Thickness(0); settings.ToolTip = "Settings (Ctrl+,)"; AutomationProperties.SetName(settings, "Settings");
            DockPanel.SetDock(settings, Dock.Right); masthead.Children.Add(settings);
            DockPanel.SetDock(wrenBadge, Dock.Right); masthead.Children.Add(wrenBadge);
            brand.VerticalAlignment = VerticalAlignment.Center; masthead.Children.Add(brand); top.Children.Add(masthead);
            headerOrnament.Margin = new Thickness(0, 7, 0, 0); headerOrnament.TextAlignment = TextAlignment.Center; top.Children.Add(headerOrnament);
            UI.Display(heading); heading.Margin = new Thickness(0, 17, 0, 6); top.Children.Add(heading);
            RefreshThemeChrome();
            summary.Margin = new Thickness(0, 0, 0, 12); top.Children.Add(summary);
            track.Background = UI.Brush("Edge"); fill.Background = UI.Brush("Accent"); track.Child = fill; top.Children.Add(track); track.SizeChanged += delegate { fill.Width = Math.Max(0, track.ActualWidth * fraction); };
            var tabs = new Grid { Margin = new Thickness(0, 16, 0, 14) };
            tabs.ColumnDefinitions.Add(new ColumnDefinition()); tabs.ColumnDefinitions.Add(new ColumnDefinition()); tabs.ColumnDefinitions.Add(new ColumnDefinition()); tabs.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            todayButton = UI.Button("Today", delegate { history = false; weekly = false; Render(null); }); todayButton.Margin = new Thickness(0, 0, 6, 0);
            weeklyButton = UI.Button("Weekly", delegate { history = false; weekly = true; Render(null); }); weeklyButton.Margin = new Thickness(0, 0, 6, 0);
            historyButton = UI.Button("History", delegate { history = true; weekly = false; historyDate = DateTime.Today; Render(null); }); historyButton.Margin = new Thickness(0, 0, 10, 0);
            addButton = UI.Button("+ Game", delegate { EditGame(null); }); UI.Primary(addButton); addButton.ToolTip = "Add game (Ctrl+N)";
            Grid.SetColumn(weeklyButton, 1); Grid.SetColumn(historyButton, 2); Grid.SetColumn(addButton, 3); tabs.Children.Add(todayButton); tabs.Children.Add(weeklyButton); tabs.Children.Add(historyButton); tabs.Children.Add(addButton); top.Children.Add(tabs);
            top.Children.Add(dateArea);
            var bottom = new Border { BorderBrush = UI.Brush("Edge"), BorderThickness = new Thickness(0, 1, 0, 0), Padding = new Thickness(0, 12, 0, 0), Margin = new Thickness(0, 12, 0, 0), Child = footer };
            DockPanel.SetDock(bottom, Dock.Bottom); root.Children.Add(bottom);
            var scroll = new ScrollViewer { Content = cards, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled }; root.Children.Add(scroll);
            activeDate = Journal.DateKey(DateTime.Now); journal.Today(DateTime.Now); foreach (var game in journal.Games.Where(g => !g.Archived && g.DailyReset != "local")) journal.CurrentTimedDaily(game, DateTime.UtcNow); foreach (var game in journal.WeeklyGames.Where(g => !g.Archived)) journal.CurrentWeekly(game, DateTime.UtcNow); storage.Save(journal);
            clock.Interval = TimeSpan.FromSeconds(3); clock.Tick += delegate { RefreshDay(); }; clock.Start();
            Activated += delegate { RefreshDay(); };
            Closing += delegate
            {
                clock.Stop();
                try { if (WindowState == WindowState.Normal) { journal.Settings.Width = Width; journal.Settings.Height = Height; } storage.Save(journal); }
                catch (Exception e) { MessageBox.Show(this, "Could not save window preferences. Previously saved progress is still on disk.\n\n" + e.Message, "WrenFeed"); }
            };
            PreviewKeyDown += delegate(object sender, KeyEventArgs e)
            {
                if (Keyboard.Modifiers != ModifierKeys.Control) return;
                if (e.Key == Key.N) { EditGame(null); e.Handled = true; }
                if (e.Key == Key.H) { history = !history; if (history) weekly = false; Render(null); e.Handled = true; }
                if (e.Key == Key.OemComma) { OpenSettings(); e.Handled = true; }
            };
            Render(null);
            Loaded += delegate { if (storage.RecoveryNotice != null) MessageBox.Show(this, storage.RecoveryNotice, "Journal recovered", MessageBoxButton.OK, MessageBoxImage.Warning); };
        }
        void BuildChrome()
        {
            var frame = new Grid();
            for (int i = 0; i < 3; i++)
            {
                frame.ColumnDefinitions.Add(new ColumnDefinition { Width = i == 1 ? new GridLength(1, GridUnitType.Star) : new GridLength(6) });
                frame.RowDefinitions.Add(new RowDefinition { Height = i == 1 ? new GridLength(1, GridUnitType.Star) : new GridLength(6) });
            }
            shell.BorderBrush = UI.Brush("Edge"); shell.BorderThickness = new Thickness(1); shell.CornerRadius = new CornerRadius(8);
            Grid.SetRowSpan(shell, 3); Grid.SetColumnSpan(shell, 3); frame.Children.Add(shell);
            var body = new Grid(); body.RowDefinitions.Add(new RowDefinition { Height = new GridLength(33) }); body.RowDefinitions.Add(new RowDefinition());
            Grid.SetRow(body, 1); Grid.SetColumn(body, 1); frame.Children.Add(body);
            var titleBar = new DockPanel { Background = Brushes.Transparent, LastChildFill = true };
            AutomationProperties.SetName(titleBar, "Move WrenFeed window");
            titleBar.MouseLeftButtonDown += delegate(object sender, MouseButtonEventArgs e)
            {
                if (e.ClickCount == 2) return;
                if (e.LeftButton == MouseButtonState.Pressed) DragMove();
            };
            var close = UI.Button("×", delegate { Close(); }); close.Width = 31; close.MinHeight = 27; close.Padding = new Thickness(0); close.ToolTip = "Close WrenFeed"; AutomationProperties.SetName(close, "Close WrenFeed");
            DockPanel.SetDock(close, Dock.Right); titleBar.Children.Add(close);
            var minimize = UI.Button("−", delegate { WindowState = WindowState.Minimized; }); minimize.Width = 31; minimize.MinHeight = 27; minimize.Padding = new Thickness(0); minimize.ToolTip = "Minimize WrenFeed"; AutomationProperties.SetName(minimize, "Minimize WrenFeed");
            DockPanel.SetDock(minimize, Dock.Right); titleBar.Children.Add(minimize);
            var label = UI.Text("✦  WrenFeed", 12, "Accent"); label.VerticalAlignment = VerticalAlignment.Center; label.Margin = new Thickness(5, 0, 0, 0); titleBar.Children.Add(label);
            body.Children.Add(titleBar); Grid.SetRow(root, 1); body.Children.Add(root);
            AddResizeHandle(frame, 0, 0, -1, -1); AddResizeHandle(frame, 0, 1, 0, -1); AddResizeHandle(frame, 0, 2, 1, -1);
            AddResizeHandle(frame, 1, 0, -1, 0); AddResizeHandle(frame, 1, 2, 1, 0);
            AddResizeHandle(frame, 2, 0, -1, 1); AddResizeHandle(frame, 2, 1, 0, 1); AddResizeHandle(frame, 2, 2, 1, 1);
            Content = frame;
        }
        void AddResizeHandle(Grid frame, int row, int column, int horizontal, int vertical)
        {
            var thumb = new Thumb { Background = Brushes.Transparent, Cursor = horizontal == 0 ? Cursors.SizeNS : vertical == 0 ? Cursors.SizeWE : horizontal == vertical ? Cursors.SizeNWSE : Cursors.SizeNESW };
            AutomationProperties.SetName(thumb, "Resize WrenFeed window");
            thumb.DragDelta += delegate(object sender, DragDeltaEventArgs e)
            {
                if (WindowState != WindowState.Normal) return;
                if (horizontal != 0)
                {
                    double change = horizontal < 0 ? -e.HorizontalChange : e.HorizontalChange;
                    double width = Math.Max(MinWidth, Width + change);
                    if (horizontal < 0) Left -= width - Width;
                    Width = width;
                }
                if (vertical != 0)
                {
                    double change = vertical < 0 ? -e.VerticalChange : e.VerticalChange;
                    double height = Math.Max(MinHeight, Height + change);
                    if (vertical < 0) Top -= height - Height;
                    Height = height;
                }
            };
            Grid.SetRow(thumb, row); Grid.SetColumn(thumb, column); frame.Children.Add(thumb);
        }
        SolidColorBrush Surface(string key)
        {
            var brush = new SolidColorBrush(ThemeManager.Current.Colors[key]);
            if (journal.Settings.BackgroundOnlyOpacity) brush.Opacity = key == "Panel" ? Math.Pow(journal.Settings.WindowOpacity, 8) : journal.Settings.WindowOpacity;
            return brush;
        }
        Border QuestCard(UIElement content)
        {
            var card = UI.Card(content); card.Background = Surface("Panel"); return card;
        }
        void ApplyWindowOpacity()
        {
            Opacity = journal.Settings.BackgroundOnlyOpacity ? 1.0 : journal.Settings.WindowOpacity;
            shell.Background = ThemeManager.Current.Decoration == "graffiti" || ThemeManager.Current.Decoration == "sketch"
                ? (Brush)UI.TexturedWall(ThemeManager.Current.Decoration, journal.Settings.BackgroundOnlyOpacity ? journal.Settings.WindowOpacity : 1)
                : Surface("Canvas");
            foreach (var card in cards.Children.OfType<Border>()) card.Background = Surface("Panel");
            ApplyTextReadability();
        }
        static System.Collections.Generic.IEnumerable<T> Descendants<T>(DependencyObject parent) where T : DependencyObject
        {
            foreach (object child in LogicalTreeHelper.GetChildren(parent))
            {
                var element = child as DependencyObject;
                if (element == null) continue;
                var match = element as T; if (match != null) yield return match;
                foreach (var nested in Descendants<T>(element)) yield return nested;
            }
        }
        void ApplyTextReadability()
        {
            if (Content == null) return;
            bool translucent = journal.Settings.BackgroundOnlyOpacity && journal.Settings.WindowOpacity < 1;
            foreach (var line in Descendants<TextBlock>((DependencyObject)Content))
            {
                if (!translucent) { line.Effect = null; continue; }
                var ink = line.Foreground as SolidColorBrush;
                if (ink == null) continue;
                var color = ink.Color;
                bool dark = 0.2126 * color.R + 0.7152 * color.G + 0.0722 * color.B < 128;
                line.Effect = new DropShadowEffect { Color = dark ? Colors.White : Colors.Black, BlurRadius = 4, ShadowDepth = 0, Opacity = 0.95 };
            }
        }
        bool RefreshDay()
        {
            string date = Journal.DateKey(DateTime.Now);
            bool dailyChanged = date != activeDate;
            bool timedDailyChanged = journal.Games.Where(g => !g.Archived && g.DailyReset != "local").Any(g =>
            {
                var current = journal.DailyUtcRecords.LastOrDefault(r => r.GameId == g.Id);
                return current == null || (g.DailyReset == "utc" ? DateTime.Parse(current.CycleStartUtc).ToUniversalTime() != Journal.DailyCycleStart(g, DateTime.UtcNow) : current.Result.Done && DateTime.UtcNow >= Journal.NextDailyIntervalReset(g, current));
            });
            bool weeklyChanged = journal.WeeklyGames.Where(g => !g.Archived).Any(g =>
            {
                var current = journal.WeeklyRecords.LastOrDefault(r => r.GameId == g.Id);
                return current == null || (g.WeeklyReset == "fixed" && DateTime.Parse(current.CycleStartUtc).ToUniversalTime() != Journal.FixedCycleStart(g, DateTime.UtcNow)) || (g.WeeklyReset == "completion-weekday" && current.Result.Done && DateTime.UtcNow >= Journal.NextWeeklyReset(g, current));
            });
            if (!dailyChanged && !timedDailyChanged && !weeklyChanged) return false;
            if (Change(delegate { journal.Today(DateTime.Now); foreach (var game in journal.Games.Where(g => !g.Archived && g.DailyReset != "local")) journal.CurrentTimedDaily(game, DateTime.UtcNow); foreach (var game in journal.WeeklyGames.Where(g => !g.Archived)) journal.CurrentWeekly(game, DateTime.UtcNow); }, null)) { activeDate = date; footer.Text = "New quests are ready."; }
            return true;
        }
        bool Change(Action action, string flash)
        {
            var before = Storage.Encode(journal);
            try { action(); storage.Save(journal); Render(flash); return true; }
            catch (Exception e)
            {
                journal = Storage.Decode(before); Render(null);
                MessageBox.Show(this, "This change was not saved. Please try again.\n\n" + e.Message, "Could not save", MessageBoxButton.OK, MessageBoxImage.Warning); return false;
            }
        }
        void Render(string flash)
        {
            cards.Children.Clear(); dateArea.Children.Clear();
            todayButton.BorderBrush = UI.Brush(!history && !weekly ? "Accent" : "Edge"); weeklyButton.BorderBrush = UI.Brush(weekly ? "Accent" : "Edge"); historyButton.BorderBrush = UI.Brush(history ? "Accent" : "Edge");
            addButton.Visibility = history ? Visibility.Collapsed : Visibility.Visible;
            if (weekly) { RenderWeekly(flash); ApplyTextReadability(); return; }
            var day = journal.Days.FirstOrDefault(d => d.Date == Journal.DateKey(history ? historyDate : DateTime.Now));
            var entries = day == null ? new System.Collections.Generic.List<GameResult>() : day.Games.Where(g => history || !g.Archived).ToList();
            if (history)
            {
                entries.AddRange(journal.DailyUtcRecords.Where(r => r.Result != null && (r.Result.Done && r.Result.CompletedUtc != null ? DateTime.Parse(r.Result.CompletedUtc).ToLocalTime().Date : DateTime.Parse(r.CycleStartUtc).ToLocalTime().Date) == historyDate.Date).Select(r => r.Result));
                entries = entries.GroupBy(g => g.Id + "|" + (g.CompletedUtc ?? "open")).Select(g => g.First()).ToList();
            }
            else
            {
                entries = journal.Games.Where(g => !g.Archived).Select(g =>
                {
                    var record = journal.DailyUtcRecords.LastOrDefault(r => r.GameId == g.Id);
                    return g.DailyReset != "local" ? (record == null ? null : record.Result) : (day == null ? null : day.Games.FirstOrDefault(r => r.Id == g.Id));
                }).Where(r => r != null).ToList();
            }
            int done = entries.Count(g => g.Done);
            fraction = entries.Count == 0 ? 0 : (double)done / entries.Count; fill.Width = Math.Max(0, track.ActualWidth * fraction);
            fill.Background = UI.Brush(entries.Count > 0 && done == entries.Count ? "Success" : "Accent");
            heading.Text = history ? "The adventure so far" : "Your daily quests";
            summary.Text = (history ? historyDate : DateTime.Today).ToString("ddd, MMM d") + "   ·   " + done + " / " + entries.Count + " quests complete";
            footer.Text = "Saved on this device · each quest has its own reset";
            if (history)
            {
                var dateRow = new DockPanel { Margin = new Thickness(0, 0, 0, 12) };
                var prev = UI.Button("‹", delegate { historyDate = historyDate.AddDays(-1); Render(null); }); prev.ToolTip = "Previous day"; AutomationProperties.SetName(prev, "Previous day"); prev.Margin = new Thickness(0, 0, 8, 0);
                var next = UI.Button("›", delegate { historyDate = historyDate.AddDays(1); Render(null); }); next.IsEnabled = historyDate.Date < DateTime.Today; next.ToolTip = "Next day"; AutomationProperties.SetName(next, "Next day"); next.Margin = new Thickness(8, 0, 0, 0);
                DockPanel.SetDock(prev, Dock.Left); DockPanel.SetDock(next, Dock.Right); dateRow.Children.Add(prev); dateRow.Children.Add(next);
                var date = new DatePicker { SelectedDate = historyDate, DisplayDateEnd = DateTime.Today, VerticalContentAlignment = VerticalAlignment.Center, FontSize = 13, Foreground = UI.Brush("Ink"), Background = UI.Brush("Field"), BorderBrush = UI.Brush("Edge") };
                AutomationProperties.SetName(date, "History date");
                date.SelectedDateChanged += delegate { if (date.SelectedDate.HasValue) { historyDate = date.SelectedDate.Value.Date; Render(null); } };
                dateRow.Children.Add(date); dateArea.Children.Add(dateRow);
                var note = UI.Text("A saved snapshot of this day · read only", 11, "Muted"); note.Margin = new Thickness(0, 0, 0, 12); dateArea.Children.Add(note);
            }
            if (entries.Count == 0)
            {
                var empty = UI.Stack(); empty.Margin = new Thickness(10, 30, 10, 30);
                var symbol = UI.Text("◇", 38, "Accent"); symbol.HorizontalAlignment = HorizontalAlignment.Center; empty.Children.Add(symbol);
                var title = UI.Text(history ? "No quests recorded" : "Every adventure starts small", 18, "Ink"); UI.Display(title); title.TextAlignment = TextAlignment.Center; title.Margin = new Thickness(0, 14, 0, 10); empty.Children.Add(title);
                var copy = UI.Text(history ? "There is no saved checklist for this date. Days when the app was closed are not marked as completed." : "Add a game and the little daily tasks worth coming back for.", 13, "Muted"); copy.TextAlignment = TextAlignment.Center; empty.Children.Add(copy);
                if (!history) { var add = UI.Button("+ Add your first game", delegate { EditGame(null); }); UI.Primary(add); add.Margin = new Thickness(0, 22, 0, 0); add.HorizontalAlignment = HorizontalAlignment.Center; empty.Children.Add(add); }
                cards.Children.Add(QuestCard(empty));
            }
            RenderQuestGroups(entries, flash);
            if (!history && entries.Count > 0 && done == entries.Count)
            {
                var finished = UI.Text("✦  Daily quests complete. Well played.", 12, "Success"); finished.Margin = new Thickness(0, 10, 0, 8); finished.TextAlignment = TextAlignment.Center; cards.Children.Add(finished);
            }
            ApplyTextReadability();
        }
        void RenderWeekly(string flash)
        {
            var entries = journal.WeeklyGames.Where(g => !g.Archived).Select(g => new { Game = g, Record = journal.WeeklyRecords.LastOrDefault(r => r.GameId == g.Id) }).Where(x => x.Record != null).ToList();
            int done = entries.Count(x => x.Record.Result.Done);
            fraction = entries.Count == 0 ? 0 : (double)done / entries.Count; fill.Width = Math.Max(0, track.ActualWidth * fraction);
            fill.Background = UI.Brush(entries.Count > 0 && done == entries.Count ? "Success" : "Accent");
            heading.Text = "Your weekly quests";
            summary.Text = done + " / " + entries.Count + " quests complete";
            footer.Text = "Saved on this device · weekly resets use UTC";
            addButton.Content = "+ Game";
            if (entries.Count == 0)
            {
                var empty = UI.Stack(); empty.Margin = new Thickness(10, 30, 10, 30);
                var title = UI.Text("A new weekly adventure", 18, "Ink"); UI.Display(title); title.TextAlignment = TextAlignment.Center; empty.Children.Add(title);
                var copy = UI.Text("Add a weekly game and choose when its quest resets.", 13, "Muted"); copy.Margin = new Thickness(0, 10, 0, 16); copy.TextAlignment = TextAlignment.Center; empty.Children.Add(copy);
                var add = UI.Button("+ Add a weekly game", delegate { EditGame(null); }); UI.Primary(add); add.HorizontalAlignment = HorizontalAlignment.Center; empty.Children.Add(add);
                cards.Children.Add(QuestCard(empty));
            }
            RenderQuestGroups(entries.Select(e => e.Record.Result).ToList(), flash);
            var past = journal.WeeklyRecords.Where(r => r.Result.Done && !entries.Any(e => e.Record.Id == r.Id)).OrderByDescending(r => r.Result.CompletedUtc).Take(8).ToList();
            if (past.Count > 0)
            {
                var title = UI.Text("PREVIOUS WEEKLY QUESTS", 11, "Accent"); title.Margin = new Thickness(0, 16, 0, 10); cards.Children.Add(title);
                foreach (var record in past)
                {
                    var at = DateTime.Parse(record.Result.CompletedUtc, null, System.Globalization.DateTimeStyles.RoundtripKind).ToLocalTime();
                    var note = UI.Text("✓  " + record.Result.Name + "  ·  " + at.ToString("MMM d, yyyy"), 12, "Muted"); note.Margin = new Thickness(3, 0, 0, 9); cards.Children.Add(note);
                }
            }
        }
        void RenderQuestGroups(System.Collections.Generic.List<GameResult> entries, string flash)
        {
            var shown = new System.Collections.Generic.HashSet<GameResult>();
            foreach (var entry in entries)
            {
                if (!shown.Add(entry)) continue;
                string group = (entry.GroupName ?? "").Trim();
                if (group.Length == 0) { cards.Children.Add(GameCard(entry, flash == entry.Id)); continue; }
                var members = entries.Where(e => String.Equals((e.GroupName ?? "").Trim(), group, StringComparison.OrdinalIgnoreCase)).ToList();
                foreach (var member in members) shown.Add(member);
                var content = UI.Stack();
                var header = new DockPanel { Margin = new Thickness(0, 0, 0, 9) };
                if (!history)
                {
                    var add = UI.Button("+ Quest", delegate { EditGame(null, group); }); add.FontSize = 11; add.Padding = new Thickness(7, 2, 7, 2); add.ToolTip = "Add a quest to " + group;
                    DockPanel.SetDock(add, Dock.Right); header.Children.Add(add);
                }
                var title = UI.Text(group, 17, "Accent"); title.FontWeight = FontWeights.SemiBold; header.Children.Add(title); content.Children.Add(header);
                for (int i = 0; i < members.Count; i++)
                {
                    if (i > 0) content.Children.Add(new Border { BorderBrush = UI.Brush("Edge"), BorderThickness = new Thickness(0, 1, 0, 0), Margin = new Thickness(0, 11, 0, 11) });
                    content.Children.Add(GameRow(members[i]));
                }
                var card = QuestCard(content); card.Margin = new Thickness(0, 0, 0, 10);
                if (members.All(m => m.Done)) card.BorderBrush = CompletedBorder();
                if (members.Any(m => m.Id == flash)) Flash(card);
                cards.Children.Add(card);
            }
        }
        void Flash(Border card)
        {
            if (!journal.Settings.Animate) return;
            var brush = new SolidColorBrush(((SolidColorBrush)card.BorderBrush).Color); card.BorderBrush = brush;
            brush.BeginAnimation(SolidColorBrush.ColorProperty, new ColorAnimation(((SolidColorBrush)UI.Brush("Accent")).Color, brush.Color, TimeSpan.FromMilliseconds(650)));
        }
        Border GameCard(GameResult game, bool flash)
        {
            var card = QuestCard(GameRow(game)); card.Margin = new Thickness(0, 0, 0, 10);
            if (game.Done) card.BorderBrush = CompletedBorder();
            if (flash) Flash(card);
            return card;
        }
        static SolidColorBrush CompletedBorder()
        {
            return ThemeManager.Current.Decoration == "none" ? new SolidColorBrush(Color.FromRgb(64, 89, 78)) : new SolidColorBrush(ThemeManager.Current.Colors["Success"]);
        }
        void RefreshThemeChrome()
        {
            brand.Text = ThemeManager.Current.Mark + "  W R E N F E E D";
            wrenBadge.Children.Clear();
            Canvas mascot = null;
            switch (ThemeManager.Current.Mascot)
            {
                case "wren": mascot = UI.CuteWren(); break;
                case "chain-wren": mascot = UI.ChainWren(); break;
                case "sphynx": mascot = UI.Sphynx(); break;
                case "pixel-wren": mascot = UI.PixelWren(); break;
                case "manga-eye": mascot = UI.MangaEye(); break;
                case "graffiti-wing": mascot = UI.GraffitiWing(); break;
                case "ink-wren": mascot = UI.InkWren(); break;
                case "flock": mascot = UI.Flock(); break;
            }
            wrenBadge.Width = ThemeManager.Current.Mascot == "flock" ? 119 : 52;
            if (mascot != null) wrenBadge.Children.Add(mascot);
            wrenBadge.Visibility = mascot == null ? Visibility.Collapsed : Visibility.Visible;
            headerOrnament.Visibility = ThemeManager.Current.Decoration == "none" ? Visibility.Collapsed : Visibility.Visible;
            headerOrnament.Text = ThemeManager.Current.Decoration == "circuit" ? "── ◇ ────── ◇ ──" : ThemeManager.Current.Decoration == "manga" ? "╱╱   ✦   ╲╲   ╱╱   ✦   ╲╲" : ThemeManager.Current.Decoration == "graffiti" ? "╱╱╱  DAILY // WEEKLY  ╱╱╱" : ThemeManager.Current.Decoration == "sketch" ? "////  INK THE NEXT QUEST  ////" : ThemeManager.Current.Decoration == "flock" ? "✧  TOGETHER, ONE QUEST AT A TIME  ✧" : ThemeManager.Current.Decoration == "chain" ? "◇ ─ ◇ ─ ◇ ─ ◇" : ThemeManager.Current.Decoration == "whiskers" ? "✧   ᵕ   ✧   ᵕ   ✧" : ThemeManager.Current.Decoration == "pixel" ? "▪ ▪ ■ ▪ ▪ ■ ▪ ▪" : "✦  ───  ◇  ───  ✦";
            headerOrnament.FontWeight = ThemeManager.Current.Decoration == "graffiti" || ThemeManager.Current.Decoration == "sketch" ? FontWeights.Bold : FontWeights.Normal;
        }
        StackPanel GameRow(GameResult game)
        {
            var content = UI.Stack(); var header = new DockPanel();
            if (!history)
            {
                var menuButton = UI.Button("···", delegate { }); menuButton.Padding = new Thickness(5, 0, 5, 0); menuButton.MinHeight = 26; menuButton.Width = 29; menuButton.FontSize = 17; menuButton.ToolTip = "Edit or reorder " + game.Name; AutomationProperties.SetName(menuButton, "Options for " + game.Name);
                var menu = new ContextMenu();
                Action<string, Action> item = delegate(string label, Action action) { var mi = new MenuItem { Header = label }; mi.Click += delegate { action(); }; menu.Items.Add(mi); };
                item("Edit quest", delegate { EditGame((weekly ? journal.WeeklyGames : journal.Games).First(g => g.Id == game.Id)); });
                item("Move up", delegate { Change(delegate { if (weekly) journal.MoveWeekly(game.Id, -1); else journal.Move(game.Id, -1); }, null); });
                item("Move down", delegate { Change(delegate { if (weekly) journal.MoveWeekly(game.Id, 1); else journal.Move(game.Id, 1); }, null); });
                menu.Items.Add(new Separator());
                item("Archive quest", delegate
                {
                    if (MessageBox.Show(this, "Remove “" + game.Name + "” from your " + (weekly ? "weekly" : "daily") + " list?\n\nIts history will stay saved. You can restore it in Settings.", "Archive quest", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                        Change(delegate { if (weekly) journal.ArchiveWeekly(game.Id, DateTime.UtcNow); else journal.Archive(game.Id, DateTime.Now); }, null);
                });
                menuButton.Click += delegate { menu.PlacementTarget = menuButton; menu.IsOpen = true; };
                DockPanel.SetDock(menuButton, Dock.Right); header.Children.Add(menuButton);
            }
            var name = UI.Text(game.Name, 16, game.Done ? "Success" : "Ink"); name.FontWeight = FontWeights.SemiBold;
            var check = new CheckBox { Content = name, IsChecked = game.Done, IsHitTestVisible = !history, Focusable = !history };
            AutomationProperties.SetName(check, "Complete " + game.Name);
            check.Click += delegate
            {
                if (RefreshDay()) return;
                bool done = check.IsChecked == true;
                if (Change(delegate { if (weekly) journal.ToggleWeeklyGame(game.Id, done, DateTime.UtcNow); else journal.ToggleGame(game.Id, done, DateTime.Now, DateTime.UtcNow); }, done ? game.Id : null)) footer.Text = done ? "Quest complete. Nicely done." : "Quest reopened.";
            };
            header.Children.Add(check); content.Children.Add(header);
            if (!String.IsNullOrWhiteSpace(game.Description)) { var desc = UI.Text(game.Description, 12, "Muted"); desc.Margin = new Thickness(29, 4, 0, 3); content.Children.Add(desc); }
            if (game.Lines.Count > 0)
            {
                var subs = UI.Stack();
                foreach (var line in game.Lines)
                {
                    if (line.Checkable)
                    {
                        var lineCheck = new CheckBox { Content = UI.Text(line.Text, 12, line.Done ? "Success" : "Muted"), IsChecked = line.Done, Margin = new Thickness(0, 2, 0, 2), IsHitTestVisible = !history, Focusable = !history };
                        AutomationProperties.SetName(lineCheck, game.Name + ": " + line.Text);
                        string lineId = line.Id;
                        lineCheck.Click += delegate
                        {
                            if (RefreshDay()) return;
                            Change(delegate { if (weekly) journal.ToggleWeeklyLine(game.Id, lineId, lineCheck.IsChecked == true, DateTime.UtcNow); else journal.ToggleLine(game.Id, lineId, lineCheck.IsChecked == true, DateTime.Now, DateTime.UtcNow); }, game.Id);
                        };
                        subs.Children.Add(lineCheck);
                    }
                    else { var note = UI.Text("·  " + line.Text, 12, "Muted"); note.Margin = new Thickness(0, 6, 0, 6); subs.Children.Add(note); }
                }
                content.Children.Add(new Border { Margin = new Thickness(29, 10, 0, 0), BorderThickness = new Thickness(1, 0, 0, 0), BorderBrush = UI.Brush("Edge"), Padding = new Thickness(12, 0, 0, 0), Child = subs });
            }
            if (game.Archived) { var archived = UI.Text("Archived quest", 10, "Muted"); archived.Margin = new Thickness(29, 8, 0, 0); content.Children.Add(archived); }
            if (!history)
            {
                var template = (weekly ? journal.WeeklyGames : journal.Games).First(g => g.Id == game.Id);
                string rule = weekly ? template.WeeklyReset == "completion-weekday" ? "7 days after completion · 00:00 UTC" : "Every " + ((DayOfWeek)template.FixedWeekdayUtc) + " · " + template.FixedHourUtc.ToString("00") + ":" + template.FixedMinuteUtc.ToString("00") + " UTC" : template.DailyReset == "utc" ? "Daily · " + template.DailyHourUtc.ToString("00") + ":" + template.DailyMinuteUtc.ToString("00") + " UTC" : template.DailyReset == "after-completion" ? template.DailyIntervalHours + " hours after completion" : "Daily · local midnight";
                var detail = UI.Text(rule, 11, "Accent"); detail.Margin = new Thickness(29, 8, 0, 0); content.Children.Add(detail);
                if (!weekly && template.DailyReset == "after-completion" && game.Done)
                {
                    var record = journal.DailyUtcRecords.Last(r => r.GameId == game.Id);
                    var reset = Journal.NextDailyIntervalReset(template, record).ToLocalTime();
                    var next = UI.Text("Ready again: " + reset.ToString("ddd, MMM d · h:mm tt") + " local", 11, "Muted"); next.Margin = new Thickness(29, 4, 0, 0); content.Children.Add(next);
                }
                if (weekly && game.Done)
                {
                    var record = journal.WeeklyRecords.Last(r => r.GameId == game.Id);
                    var reset = Journal.NextWeeklyReset(template, record).ToLocalTime();
                    var next = UI.Text("Next quest: " + reset.ToString("ddd, MMM d · h:mm tt") + " local", 11, "Muted"); next.Margin = new Thickness(29, 4, 0, 0); content.Children.Add(next);
                }
            }
            if (history && game.Done && !String.IsNullOrEmpty(game.CompletedUtc))
            {
                var at = DateTime.Parse(game.CompletedUtc).ToLocalTime();
                var when = UI.Text("Completed " + at.ToString("h:mm tt") + " local", 10, "Muted"); when.Margin = new Thickness(29, 8, 0, 0); content.Children.Add(when);
            }
            return content;
        }
        void EditGame(Game game)
        {
            EditGame(game, null);
        }
        void EditGame(Game game, string group)
        {
            var editor = new GameEditor(this, game, weekly, group);
            if (editor.ShowDialog() == true)
            {
                history = false;
                Change(delegate { if (weekly) journal.SaveWeeklyGame(editor.Result, DateTime.UtcNow); else journal.SaveGame(editor.Result, DateTime.Now, DateTime.UtcNow); }, null);
            }
        }
        void OpenSettings()
        {
            var dialog = new Window(); UI.SetupDialog(dialog, this, "WrenFeed · Settings", 450, 580);
            var stack = UI.Stack(); stack.Margin = new Thickness(24);
            var title = UI.Text("Make yourself at home", 22, "Ink"); UI.Display(title); stack.Children.Add(title);
            var intro = UI.Text("A quiet little companion for your daily games.", 12, "Muted"); intro.Margin = new Thickness(0, 8, 0, 20); stack.Children.Add(intro);
            var pin = new CheckBox { Content = "Keep this window on top", IsChecked = journal.Settings.AlwaysOnTop, Margin = new Thickness(0, 0, 0, 10) };
            pin.Click += delegate { if (Change(delegate { journal.Settings.AlwaysOnTop = pin.IsChecked == true; }, null)) Topmost = journal.Settings.AlwaysOnTop; else pin.IsChecked = journal.Settings.AlwaysOnTop; };
            var animate = new CheckBox { Content = "Subtle completion animations", IsChecked = journal.Settings.Animate };
            animate.Click += delegate { if (!Change(delegate { journal.Settings.Animate = animate.IsChecked == true; }, null)) animate.IsChecked = journal.Settings.Animate; };
            stack.Children.Add(pin); stack.Children.Add(animate);
            stack.Children.Add(UI.Label("WINDOW VISIBILITY"));
            var wholeWindow = new RadioButton { Content = "Fade everything", GroupName = "OpacityMode", IsChecked = !journal.Settings.BackgroundOnlyOpacity, Foreground = UI.Brush("Ink"), Margin = new Thickness(0, 0, 0, 5) };
            var backgroundOnly = new RadioButton { Content = "Fade backgrounds only · keep text clear", GroupName = "OpacityMode", IsChecked = journal.Settings.BackgroundOnlyOpacity, Foreground = UI.Brush("Ink"), Margin = new Thickness(0, 0, 0, 7) };
            AutomationProperties.SetName(wholeWindow, "Fade whole window"); AutomationProperties.SetName(backgroundOnly, "Fade backgrounds only");
            stack.Children.Add(wholeWindow); stack.Children.Add(backgroundOnly);
            bool resettingMode = false;
            Action<bool> chooseMode = delegate(bool background)
            {
                if (resettingMode || journal.Settings.BackgroundOnlyOpacity == background) return;
                if (Change(delegate { journal.Settings.BackgroundOnlyOpacity = background; }, null)) ApplyWindowOpacity();
                else { resettingMode = true; wholeWindow.IsChecked = !journal.Settings.BackgroundOnlyOpacity; backgroundOnly.IsChecked = journal.Settings.BackgroundOnlyOpacity; resettingMode = false; }
            };
            wholeWindow.Checked += delegate { chooseMode(false); };
            backgroundOnly.Checked += delegate { chooseMode(true); };
            var opacityLabel = UI.Text("Window visibility · " + Math.Round(journal.Settings.WindowOpacity * 100) + "%", 12, "Muted"); stack.Children.Add(opacityLabel);
            var opacitySlider = new Slider { Minimum = 20, Maximum = 100, Value = Math.Round(journal.Settings.WindowOpacity * 100), TickFrequency = 5, IsSnapToTickEnabled = true, SmallChange = 5, LargeChange = 10, Margin = new Thickness(0, 5, 0, 0) };
            AutomationProperties.SetName(opacitySlider, "Window opacity"); stack.Children.Add(opacitySlider);
            var opacityHint = UI.Text("At 100% the window is solid. Backgrounds only keeps text clear and reduces the extra tint on quest cards; small buttons stay solid so they remain easy to use.", 11, "Muted"); opacityHint.Margin = new Thickness(0, 4, 0, 2); stack.Children.Add(opacityHint);
            bool resettingOpacity = false;
            opacitySlider.ValueChanged += delegate
            {
                if (resettingOpacity) return;
                double value = opacitySlider.Value / 100.0;
                if (Math.Abs(value - journal.Settings.WindowOpacity) < 0.001) return;
                if (Change(delegate { journal.Settings.WindowOpacity = value; }, null)) { ApplyWindowOpacity(); opacityLabel.Text = "Window visibility · " + Math.Round(value * 100) + "%"; }
                else { resettingOpacity = true; opacitySlider.Value = journal.Settings.WindowOpacity * 100; resettingOpacity = false; }
            };
            stack.Children.Add(UI.Label("APPEARANCE"));
            var themeList = new ComboBox { MinHeight = 33 }; AutomationProperties.SetName(themeList, "Theme"); stack.Children.Add(themeList);
            var themeInfo = UI.Text("", 11, "Muted"); themeInfo.Margin = new Thickness(0, 6, 0, 5); stack.Children.Add(themeInfo);
            bool fillingThemes = false;
            Action fillThemes = delegate
            {
                fillingThemes = true; themeList.Items.Clear();
                var available = new System.Collections.Generic.List<ThemeSpec> { ThemeManager.Default }; available.AddRange(themes.Installed());
                foreach (var option in available) themeList.Items.Add(new ComboBoxItem { Content = option.Name, Tag = option });
                themeList.SelectedIndex = available.FindIndex(t => t.Id == journal.Settings.Theme);
                themeInfo.Text = available.Any(t => t.Id == journal.Settings.Theme) ? ((ThemeSpec)((ComboBoxItem)themeList.SelectedItem).Tag).Description : "The selected theme file is missing. Default is showing until you install it again or choose another theme.";
                fillingThemes = false;
            };
            fillThemes();
            themeList.SelectionChanged += delegate
            {
                if (fillingThemes || themeList.SelectedItem == null) return;
                var selected = (ThemeSpec)((ComboBoxItem)themeList.SelectedItem).Tag;
                if (selected.Id == journal.Settings.Theme) return;
                if (Change(delegate { journal.Settings.Theme = selected.Id; }, null))
                {
                    ThemeManager.Apply(selected); RefreshThemeChrome(); Render(null); ApplyWindowOpacity();
                    themeInfo.Text = selected.Description;
                }
                else fillThemes();
            };
            var installTheme = UI.Button("Install theme file…", delegate
            {
                var picker = new OpenFileDialog { Filter = "WrenFeed theme (*.json)|*.json", CheckFileExists = true, Title = "Choose a WrenFeed theme" };
                var suppliedThemes = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "themes");
                if (Directory.Exists(suppliedThemes)) picker.InitialDirectory = suppliedThemes;
                if (picker.ShowDialog(dialog) != true) return;
                try
                {
                    var candidate = ThemeManager.Parse(File.ReadAllBytes(picker.FileName));
                    var destination = Path.Combine(themes.Folder, candidate.Id + ".json");
                    if (File.Exists(destination) && !String.Equals(Path.GetFullPath(picker.FileName), Path.GetFullPath(destination), StringComparison.OrdinalIgnoreCase) && MessageBox.Show(dialog, "Replace the installed “" + candidate.Name + "” theme file?", "Replace theme", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
                    var installed = themes.Install(picker.FileName);
                    fillThemes();
                    if (installed.Id == journal.Settings.Theme) { ThemeManager.Apply(installed); RefreshThemeChrome(); Render(null); ApplyWindowOpacity(); themeInfo.Text = installed.Description; }
                    foreach (ComboBoxItem item in themeList.Items) if (((ThemeSpec)item.Tag).Id == installed.Id) { themeList.SelectedItem = item; break; }
                }
                catch (Exception error) { MessageBox.Show(dialog, error.Message, "Theme install failed", MessageBoxButton.OK, MessageBoxImage.Warning); }
            });
            installTheme.Margin = new Thickness(0, 5, 0, 0); installTheme.HorizontalAlignment = HorizontalAlignment.Left; stack.Children.Add(installTheme);
            var themeLocation = UI.Text("Optional theme files stay in " + themes.Folder, 10, "Muted"); themeLocation.TextWrapping = TextWrapping.Wrap; themeLocation.Margin = new Thickness(0, 6, 0, 0); stack.Children.Add(themeLocation);
            stack.Children.Add(UI.Label("DAILY RESET")); stack.Children.Add(UI.Text("Each quest can reset at local midnight, a fixed UTC time, or a chosen number of hours after completion. Progress saves immediately, and earlier completions remain in History.", 12, "Muted"));
            stack.Children.Add(UI.Label("WEEKLY RESET")); stack.Children.Add(UI.Text("Choose a fixed UTC day and time, or the same UTC weekday seven days after completion. Weekly completion history appears on the Weekly tab.", 12, "Muted"));
            stack.Children.Add(UI.Label("YOUR DATA")); stack.Children.Add(UI.Text("Your journal stays on this PC. Only the optional leaderboard upload uses the internet. A previous-save backup is kept automatically.", 12, "Muted"));
            var buttons = new WrapPanel { Margin = new Thickness(0, 12, 0, 0) };
            var leaderboard = UI.Button("Leaderboard upload…", delegate { OpenLeaderboard(dialog); }); leaderboard.Margin = new Thickness(0, 0, 8, 8); buttons.Children.Add(leaderboard);
            var questImport = UI.Button("Import quests", delegate { ImportQuests(dialog); }); questImport.Margin = new Thickness(0, 0, 8, 8); buttons.Children.Add(questImport);
            var export = UI.Button("Export backup", delegate { Export(dialog); }); export.Margin = new Thickness(0, 0, 8, 8); buttons.Children.Add(export);
            var import = UI.Button("Restore backup", delegate { Import(dialog); }); import.Margin = new Thickness(0, 0, 0, 8); buttons.Children.Add(import); stack.Children.Add(buttons);
            var location = UI.Text(storage.DataPath, 10, "Muted"); location.TextWrapping = TextWrapping.Wrap; stack.Children.Add(location);
            stack.Children.Add(UI.Label("ARCHIVED GAMES"));
            var archived = UI.Stack(); stack.Children.Add(archived);
            Action renderArchived = null;
            renderArchived = delegate
            {
                archived.Children.Clear();
                foreach (var game in journal.Games.Where(g => g.Archived).ToList())
                {
                    var row = new DockPanel { Margin = new Thickness(0, 4, 0, 4) }; string id = game.Id;
                    var restore = UI.Button("Restore", delegate { if (Change(delegate { journal.Restore(id, DateTime.Now, DateTime.UtcNow); }, null)) renderArchived(); }); DockPanel.SetDock(restore, Dock.Right); row.Children.Add(restore);
                    var label = UI.Text(game.Name, 12, "Ink"); label.Margin = new Thickness(0, 5, 8, 0); row.Children.Add(label); archived.Children.Add(row);
                }
                foreach (var game in journal.WeeklyGames.Where(g => g.Archived).ToList())
                {
                    var row = new DockPanel { Margin = new Thickness(0, 4, 0, 4) }; string id = game.Id;
                    var restore = UI.Button("Restore", delegate { if (Change(delegate { journal.RestoreWeekly(id, DateTime.UtcNow); }, null)) renderArchived(); }); DockPanel.SetDock(restore, Dock.Right); row.Children.Add(restore);
                    var label = UI.Text(game.Name + " · Weekly", 12, "Ink"); label.Margin = new Thickness(0, 5, 8, 0); row.Children.Add(label); archived.Children.Add(row);
                }
                if (archived.Children.Count == 0) archived.Children.Add(UI.Text("No archived games.", 12, "Muted"));
            }; renderArchived();
            var close = UI.Button("Done", delegate { dialog.Close(); }); close.HorizontalAlignment = HorizontalAlignment.Right; close.Margin = new Thickness(0, 22, 0, 0); UI.Primary(close); stack.Children.Add(close);
            dialog.Content = new ScrollViewer { Content = stack, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            dialog.ShowDialog();
        }
        void OpenLeaderboard(Window owner)
        {
            var snapshot = LeaderboardSnapshot.Create(journal);
            var dialog = new Window(); UI.SetupDialog(dialog, owner, "WrenFeed · Leaderboard upload", 480, 570);
            var stack = UI.Stack(); stack.Margin = new Thickness(22);
            var title = UI.Text("Share your tally", 22, "Ink"); UI.Display(title); stack.Children.Add(title);
            var intro = UI.Text("Your checklist remains local. Press Upload only when you want to share the nickname and game totals shown below. No dates, descriptions, subtask details, or journal file are sent.", 12, "Muted"); intro.Margin = new Thickness(0, 8, 0, 6); stack.Children.Add(intro);
            LeaderboardIdentity saved = new LeaderboardIdentity();
            try { saved = LeaderboardIdentity.Load(storage.Folder); }
            catch (Exception) { /* A missing or unreadable local invite can be entered again. */ }
            stack.Children.Add(UI.Label("PUBLIC NICKNAME"));
            var nickname = UI.Field(saved.nickname ?? "", "Leaderboard nickname", 32); stack.Children.Add(nickname);
            stack.Children.Add(UI.Label("PRIVATE INVITE CODE"));
            var code = new PasswordBox { Password = saved.inviteCode ?? "", MinHeight = 32 }; AutomationProperties.SetName(code, "Leaderboard invite code"); stack.Children.Add(code);
            var codeHint = UI.Text("Each person gets a separate code. WrenFeed saves it encrypted for your Windows account after a successful upload.", 11, "Muted"); codeHint.Margin = new Thickness(0, 5, 0, 0); stack.Children.Add(codeHint);
            stack.Children.Add(UI.Label("WHAT WILL BE SHARED"));
            var summary = UI.Text(snapshot.DailyTotal + " daily · " + snapshot.WeeklyTotal + " weekly completions across " + snapshot.games.Count + " games", 13, "Ink"); summary.FontWeight = FontWeights.SemiBold; stack.Children.Add(summary);
            var rows = UI.Stack(); rows.Margin = new Thickness(0, 7, 0, 2);
            foreach (var game in snapshot.games)
            {
                var row = UI.Text(game.name + "  ·  " + game.daily + " daily  /  " + game.weekly + " weekly", 11, "Muted"); row.Margin = new Thickness(0, 2, 0, 2); rows.Children.Add(row);
            }
            if (snapshot.games.Count == 0) rows.Children.Add(UI.Text("No completed quests are recorded yet.", 11, "Muted"));
            var preview = new ScrollViewer { Content = rows, MaxHeight = 160, VerticalScrollBarVisibility = ScrollBarVisibility.Auto }; stack.Children.Add(preview);
            var status = UI.Text("Uploads happen only when you press the button below.", 11, "Muted"); status.Margin = new Thickness(0, 12, 0, 8); stack.Children.Add(status);
            var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            var cancel = UI.Button("Close", delegate { dialog.Close(); }); cancel.Margin = new Thickness(0, 0, 8, 0); buttons.Children.Add(cancel);
            Button upload = null;
            upload = UI.Button("Upload totals", async delegate
            {
                var name = nickname.Text.Trim(); var invite = code.Password.Trim();
                if (name.Length < 1 || invite.Length < 24) { status.Text = "Enter a nickname and your private invite code."; return; }
                upload.IsEnabled = false; status.Text = "Sending the totals shown above…";
                try
                {
                    await LeaderboardUpload.SendAsync(name, invite, snapshot);
                    try { new LeaderboardIdentity { nickname = name, inviteCode = invite }.Save(storage.Folder); }
                    catch (Exception) { status.Text = "Uploaded, but this PC could not remember your invite code. Enter it again next time."; return; }
                    status.Text = "Uploaded. The leaderboard should show your latest totals shortly.";
                }
                catch (Exception error) { status.Text = error.Message; }
                finally { upload.IsEnabled = true; }
            }); UI.Primary(upload); upload.IsEnabled = snapshot.DailyTotal + snapshot.WeeklyTotal > 0; buttons.Children.Add(upload); stack.Children.Add(buttons);
            dialog.Content = new ScrollViewer { Content = stack, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            dialog.ShowDialog();
        }
        void Export(Window owner)
        {
            var picker = new SaveFileDialog { Filter = "WrenFeed backup (*.json)|*.json", FileName = "WrenFeed-" + DateTime.Today.ToString("yyyy-MM-dd") + ".json", OverwritePrompt = true };
            if (picker.ShowDialog(owner) != true) return;
            try
            {
                if (IsLiveFile(picker.FileName)) throw new IOException("Choose a location outside the live journal files.");
                File.WriteAllBytes(picker.FileName, Storage.Encode(journal)); MessageBox.Show(owner, "Your games, settings, and history have been exported.", "Backup exported");
            } catch (Exception e) { MessageBox.Show(owner, e.Message, "Export failed"); }
        }
        bool IsLiveFile(string path)
        {
            return String.Equals(Path.GetFullPath(path), storage.DataPath, StringComparison.OrdinalIgnoreCase) || String.Equals(Path.GetFullPath(path), storage.BackupPath, StringComparison.OrdinalIgnoreCase);
        }
        void ImportQuests(Window owner)
        {
            var picker = new OpenFileDialog { Filter = "WrenFeed quest list (*.json)|*.json", CheckFileExists = true, Title = "Choose a WrenFeed quest list" };
            if (picker.ShowDialog(owner) != true) return;
            try
            {
                var plan = QuestImportPlan.Create(journal, QuestImport.Parse(File.ReadAllBytes(picker.FileName)));
                var preview = new Window(); UI.SetupDialog(preview, owner, "WrenFeed · Import quests", 510, 560);
                var layout = new DockPanel { Margin = new Thickness(22) }; preview.Content = layout;
                var summaryText = UI.Text(plan.ToAdd.Count + " quests to add · " + plan.Skipped.Count + " duplicates to skip", 17, "Ink");
                summaryText.Margin = new Thickness(0, 0, 0, 12); DockPanel.SetDock(summaryText, Dock.Top); layout.Children.Add(summaryText);
                var actions = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 14, 0, 0) };
                var cancel = UI.Button("Cancel", delegate { preview.DialogResult = false; }); cancel.IsCancel = true; cancel.Margin = new Thickness(0, 0, 8, 0); actions.Children.Add(cancel);
                var add = UI.Button("Add quests", delegate { preview.DialogResult = true; }); add.IsEnabled = plan.ToAdd.Count > 0; UI.Primary(add); actions.Children.Add(add);
                DockPanel.SetDock(actions, Dock.Bottom); layout.Children.Add(actions);
                var details = UI.Stack();
                foreach (var entry in plan.ToAdd)
                {
                    var label = UI.Text((entry.Weekly ? "Weekly" : "Daily") + " · " + (String.IsNullOrEmpty(entry.Game.GroupName) ? "Ungrouped" : entry.Game.GroupName) + " · " + entry.Game.Name, 13, "Ink");
                    label.Margin = new Thickness(0, 0, 0, 9); details.Children.Add(label);
                }
                foreach (var entry in plan.Skipped)
                {
                    var label = UI.Text("Skip duplicate · " + (entry.Weekly ? "Weekly" : "Daily") + " · " + (String.IsNullOrEmpty(entry.Game.GroupName) ? "Ungrouped" : entry.Game.GroupName) + " · " + entry.Game.Name, 12, "Muted");
                    label.Margin = new Thickness(0, 0, 0, 9); details.Children.Add(label);
                }
                layout.Children.Add(new ScrollViewer { Content = details, VerticalScrollBarVisibility = ScrollBarVisibility.Auto });
                if (preview.ShowDialog() != true) return;
                if (Change(delegate { plan.Apply(journal, DateTime.Now, DateTime.UtcNow); }, null))
                {
                    history = false;
                    weekly = plan.ToAdd.All(q => q.Weekly);
                    Render(null);
                    owner.Close();
                    MessageBox.Show(this, "Added " + plan.ToAdd.Count + " quests. Skipped " + plan.Skipped.Count + " duplicates.", "Quests imported");
                }
            }
            catch (Exception e) { MessageBox.Show(owner, e.Message, "Quest import failed", MessageBoxButton.OK, MessageBoxImage.Warning); }
        }
        void Import(Window owner)
        {
            var picker = new OpenFileDialog { Filter = "WrenFeed backup (*.json)|*.json", CheckFileExists = true };
            if (picker.ShowDialog(owner) != true) return;
            try
            {
                var restored = Storage.Decode(File.ReadAllBytes(picker.FileName));
                if (MessageBox.Show(owner, "Replace your current journal with this backup?\n\n" + restored.Games.Count(g => !g.Archived) + " daily games · " + restored.WeeklyGames.Count(g => !g.Archived) + " weekly games · " + restored.Days.Count + " recorded days\n\nA separate copy of your current journal will be kept in the data folder.", "Restore backup", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
                File.WriteAllBytes(Path.Combine(storage.Folder, "before-restore-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-ffff") + ".json"), Storage.Encode(journal));
                if (Change(delegate { journal = restored; journal.Today(DateTime.Now); foreach (var game in journal.Games.Where(g => !g.Archived && g.DailyReset != "local")) journal.CurrentTimedDaily(game, DateTime.UtcNow); foreach (var game in journal.WeeklyGames.Where(g => !g.Archived)) journal.CurrentWeekly(game, DateTime.UtcNow); }, null)) { Topmost = journal.Settings.AlwaysOnTop; ApplyWindowOpacity(); owner.Close(); }
            } catch (Exception e) { MessageBox.Show(owner, e.Message, "Restore failed"); }
        }
    }
    public sealed class GameEditor : Window
    {
        public Game Result;
        readonly string gameId;
        readonly TextBox name;
        readonly TextBox groupName;
        readonly TextBox description;
        readonly bool isWeekly;
        ComboBox resetRule;
        ComboBox resetDay;
        TextBox resetTime;
        StackPanel fixedOptions;
        ComboBox dailyResetRule;
        TextBox dailyResetTime;
        StackPanel dailyUtcOptions;
        TextBox dailyIntervalHours;
        StackPanel dailyIntervalOptions;
        readonly StackPanel rows = UI.Stack();
        readonly System.Collections.Generic.List<LineEditor> lines = new System.Collections.Generic.List<LineEditor>();
        public GameEditor(Window owner, Game game) : this(owner, game, false) { }
        public GameEditor(Window owner, Game game, bool weekly) : this(owner, game, weekly, null) { }
        public GameEditor(Window owner, Game game, bool weekly, string group)
        {
            isWeekly = weekly;
            gameId = game == null ? Guid.NewGuid().ToString("N") : game.Id;
            UI.SetupDialog(this, owner, game == null ? "WrenFeed · Add quest" : "WrenFeed · Edit quest", 510, weekly ? 760 : 730);
            var root = new DockPanel { Margin = new Thickness(24) }; Content = root;
            var actions = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 18, 0, 0) };
            var cancel = UI.Button("Cancel", delegate { DialogResult = false; }); cancel.Margin = new Thickness(0, 0, 8, 0); cancel.IsCancel = true; actions.Children.Add(cancel);
            var save = UI.Button("Save quest", Save); UI.Primary(save); save.IsDefault = true; actions.Children.Add(save); DockPanel.SetDock(actions, Dock.Bottom); root.Children.Add(actions);
            var stack = UI.Stack();
            var title = UI.Text(game == null ? (weekly ? "A new weekly quest" : "A new daily quest") : "Refine your quest", 23, "Ink"); UI.Display(title); stack.Children.Add(title);
            stack.Children.Add(UI.Label("QUEST NAME")); name = UI.Field(game == null ? "" : game.Name, "Quest name", 80); stack.Children.Add(name);
            stack.Children.Add(UI.Label("GAME GROUP  ·  OPTIONAL")); groupName = UI.Field(game == null ? group : game.GroupName, "Game group", 80); stack.Children.Add(groupName);
            var groupHint = UI.Text("Use the same group for related quests, such as OSRS. Each quest keeps its own reset and history.", 11, "Muted"); groupHint.Margin = new Thickness(0, 5, 0, 0); stack.Children.Add(groupHint);
            stack.Children.Add(UI.Label("SHORT DESCRIPTION  ·  OPTIONAL")); description = UI.Field(game == null ? "" : game.Description, "Short description", 220); description.TextWrapping = TextWrapping.Wrap; description.MinHeight = 60; description.AcceptsReturn = true; stack.Children.Add(description);
            if (!weekly)
            {
                stack.Children.Add(UI.Label("DAILY RESET RULE"));
                dailyResetRule = new ComboBox { MinHeight = 33 }; AutomationProperties.SetName(dailyResetRule, "Daily reset rule");
                dailyResetRule.Items.Add("Local midnight"); dailyResetRule.Items.Add("Fixed UTC time"); dailyResetRule.Items.Add("Hours after completion");
                dailyResetRule.SelectedIndex = game != null && game.DailyReset == "after-completion" ? 2 : game != null && game.DailyReset == "utc" ? 1 : 0; stack.Children.Add(dailyResetRule);
                dailyUtcOptions = UI.Stack(); dailyUtcOptions.Children.Add(UI.Label("RESET TIME (HH:MM UTC)"));
                dailyResetTime = UI.Field(game == null ? "00:00" : game.DailyHourUtc.ToString("00") + ":" + game.DailyMinuteUtc.ToString("00"), "Daily reset time UTC", 5); dailyUtcOptions.Children.Add(dailyResetTime);
                stack.Children.Add(dailyUtcOptions);
                dailyIntervalOptions = UI.Stack(); dailyIntervalOptions.Children.Add(UI.Label("HOURS AFTER COMPLETION"));
                dailyIntervalHours = UI.Field(game == null || game.DailyIntervalHours == 0 ? "24" : game.DailyIntervalHours.ToString(), "Hours after completion", 5); dailyIntervalOptions.Children.Add(dailyIntervalHours);
                var intervalHint = UI.Text("Complete the quest when you start the timer. For Forma, use 23 hours if you check it after starting the next build.", 11, "Muted"); intervalHint.Margin = new Thickness(0, 7, 0, 0); dailyIntervalOptions.Children.Add(intervalHint);
                stack.Children.Add(dailyIntervalOptions);
                dailyResetRule.SelectionChanged += delegate { dailyUtcOptions.Visibility = dailyResetRule.SelectedIndex == 1 ? Visibility.Visible : Visibility.Collapsed; dailyIntervalOptions.Visibility = dailyResetRule.SelectedIndex == 2 ? Visibility.Visible : Visibility.Collapsed; };
                dailyUtcOptions.Visibility = dailyResetRule.SelectedIndex == 1 ? Visibility.Visible : Visibility.Collapsed;
                dailyIntervalOptions.Visibility = dailyResetRule.SelectedIndex == 2 ? Visibility.Visible : Visibility.Collapsed;
            }
            if (weekly)
            {
                stack.Children.Add(UI.Label("WEEKLY RESET RULE"));
                resetRule = new ComboBox { MinHeight = 33 }; AutomationProperties.SetName(resetRule, "Weekly reset rule");
                resetRule.Items.Add("Fixed UTC day and time"); resetRule.Items.Add("Seven days after completion (UTC weekday)");
                resetRule.SelectedIndex = game != null && game.WeeklyReset == "completion-weekday" ? 1 : 0; stack.Children.Add(resetRule);
                fixedOptions = UI.Stack();
                fixedOptions.Children.Add(UI.Label("RESET DAY (UTC)")); resetDay = new ComboBox { MinHeight = 33 }; AutomationProperties.SetName(resetDay, "Fixed reset day UTC");
                foreach (DayOfWeek day in Enum.GetValues(typeof(DayOfWeek))) resetDay.Items.Add(day.ToString());
                resetDay.SelectedIndex = game == null ? 0 : game.FixedWeekdayUtc; fixedOptions.Children.Add(resetDay);
                fixedOptions.Children.Add(UI.Label("RESET TIME (HH:MM UTC)")); resetTime = UI.Field(game == null ? "00:00" : game.FixedHourUtc.ToString("00") + ":" + game.FixedMinuteUtc.ToString("00"), "Fixed reset time UTC", 5); fixedOptions.Children.Add(resetTime);
                stack.Children.Add(fixedOptions);
                var ruleHint = UI.Text("For Tears of Guthix, choose “Seven days after completion.” It becomes available at 00:00 UTC on the same weekday next week.", 11, "Muted"); ruleHint.Margin = new Thickness(0, 8, 0, 0); stack.Children.Add(ruleHint);
                resetRule.SelectionChanged += delegate { fixedOptions.Visibility = resetRule.SelectedIndex == 0 ? Visibility.Visible : Visibility.Collapsed; };
                fixedOptions.Visibility = resetRule.SelectedIndex == 0 ? Visibility.Visible : Visibility.Collapsed;
            }
            stack.Children.Add(UI.Label("ADDITIONAL LINES  ·  OPTIONAL"));
            var info = UI.Text("Use a line for a note or a task. Enable “Checkbox” for tasks you want to check off individually.", 12, "Muted"); info.Margin = new Thickness(0, 0, 0, 12); stack.Children.Add(info); stack.Children.Add(rows);
            if (game != null) foreach (var line in game.Lines) AddLine(line);
            var add = UI.Button("+ Add line", delegate { var line = AddLine(null); line.Text.Focus(); }); add.HorizontalAlignment = HorizontalAlignment.Left; add.Margin = new Thickness(0, 8, 0, 0); stack.Children.Add(add);
            var hint = UI.Text("Completing every checkable task completes the game. Checking the game completes all its tasks.", 11, "Muted"); hint.Margin = new Thickness(0, 20, 0, 0); stack.Children.Add(hint);
            root.Children.Add(new ScrollViewer { Content = stack, VerticalScrollBarVisibility = ScrollBarVisibility.Auto });
            Loaded += delegate { name.Focus(); name.SelectAll(); };
        }
        LineEditor AddLine(QuestLine line)
        {
            var editor = new LineEditor { Id = line == null ? Guid.NewGuid().ToString("N") : line.Id };
            var row = UI.Stack(); row.Margin = new Thickness(0, 0, 0, 10);
            editor.Text = UI.Field(line == null ? "" : line.Text, "Line text", 160); editor.Text.TextWrapping = TextWrapping.Wrap; row.Children.Add(editor.Text);
            var options = new DockPanel { Margin = new Thickness(0, 4, 0, 0) };
            var remove = UI.Button("Remove", delegate { rows.Children.Remove(row); lines.Remove(editor); }); remove.FontSize = 11; remove.Padding = new Thickness(8, 2, 8, 2); remove.MinHeight = 27; DockPanel.SetDock(remove, Dock.Right); options.Children.Add(remove);
            editor.Checkable = new CheckBox { Content = UI.Text("Checkbox", 11, "Muted"), IsChecked = line != null && line.Checkable }; options.Children.Add(editor.Checkable); row.Children.Add(options);
            rows.Children.Add(row); lines.Add(editor); return editor;
        }
        void Save()
        {
            if (String.IsNullOrWhiteSpace(name.Text)) { MessageBox.Show(this, "Give this quest a name first.", "Quest name required"); name.Focus(); return; }
            int hour = 0, minute = 0;
            if (isWeekly && resetRule.SelectedIndex == 0)
            {
                var parts = resetTime.Text.Split(':');
                if (parts.Length != 2 || parts[0].Length != 2 || parts[1].Length != 2 || !int.TryParse(parts[0], out hour) || !int.TryParse(parts[1], out minute) || hour > 23 || minute > 59 || hour < 0 || minute < 0)
                { MessageBox.Show(this, "Enter a UTC reset time in 24-hour HH:MM format, such as 00:00 or 18:30.", "Invalid reset time"); resetTime.Focus(); return; }
            }
            int dailyHour = 0, dailyMinute = 0;
            if (!isWeekly && dailyResetRule.SelectedIndex == 1)
            {
                var parts = dailyResetTime.Text.Split(':');
                if (parts.Length != 2 || parts[0].Length != 2 || parts[1].Length != 2 || !int.TryParse(parts[0], out dailyHour) || !int.TryParse(parts[1], out dailyMinute) || dailyHour > 23 || dailyMinute > 59 || dailyHour < 0 || dailyMinute < 0)
                { MessageBox.Show(this, "Enter a UTC reset time in 24-hour HH:MM format.", "Invalid reset time"); dailyResetTime.Focus(); return; }
            }
            int intervalHours = 24;
            if (!isWeekly && dailyResetRule.SelectedIndex == 2 && (!int.TryParse(dailyIntervalHours.Text.Trim(), out intervalHours) || intervalHours < 1 || intervalHours > 8760))
            { MessageBox.Show(this, "Enter a whole number of hours from 1 to 8760.", "Invalid reset interval"); dailyIntervalHours.Focus(); return; }
            Result = new Game { Id = gameId, Name = name.Text.Trim(), GroupName = groupName.Text.Trim(), Description = description.Text.Trim(), Lines = lines.Where(l => !String.IsNullOrWhiteSpace(l.Text.Text)).Select(l => new QuestLine { Id = l.Id, Text = l.Text.Text.Trim(), Checkable = l.Checkable.IsChecked == true }).ToList(), DailyReset = !isWeekly && dailyResetRule.SelectedIndex == 2 ? "after-completion" : !isWeekly && dailyResetRule.SelectedIndex == 1 ? "utc" : "local", DailyHourUtc = dailyHour, DailyMinuteUtc = dailyMinute, DailyIntervalHours = intervalHours, WeeklyReset = isWeekly && resetRule.SelectedIndex == 1 ? "completion-weekday" : "fixed", FixedWeekdayUtc = isWeekly ? resetDay.SelectedIndex : 0, FixedHourUtc = hour, FixedMinuteUtc = minute };
            DialogResult = true;
        }
        sealed class LineEditor { public string Id; public TextBox Text; public CheckBox Checkable; }
    }
}
