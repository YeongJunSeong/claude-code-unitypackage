using UnityEngine;
using UnityEngine.UIElements;

namespace ClaudeCode.Editor.UI
{
    public enum IconType
    {
        Refresh,
        Folder,
        File,
        Script,
        Prefab,
        Material,
        Image,
        GameObject,
        Scene,
        Error,
        Info,
        Search,
        Plus,
        Close,
        Copy,
        Send,
        Edit,
        Lock,
        Account,
        Settings,
        ChevronDown,
        Check,
        BookOpen
    }

    /// <summary>
    /// Lucide-inspired vector icons drawn at runtime via Painter2D.
    /// All icons live in a 24-unit virtual viewBox with 2-unit stroke,
    /// round line caps/joins. No external assets required.
    /// </summary>
    public static class VectorIcons
    {
        static readonly Color DefaultStroke = new Color(0.86f, 0.86f, 0.88f);
        const float V = 24f;
        const float StrokeUnits = 2f;

        public static VisualElement Make(IconType type, int size = 16, Color? color = null)
        {
            var ve = new VisualElement();
            ve.style.width = size;
            ve.style.height = size;
            ve.style.flexShrink = 0;
            ve.pickingMode = PickingMode.Ignore;
            var c = color ?? DefaultStroke;
            ve.generateVisualContent += mgc =>
            {
                var p = mgc.painter2D;
                p.strokeColor = c;
                p.fillColor = c;
                p.lineWidth = StrokeUnits * size / V;
                p.lineCap = LineCap.Round;
                p.lineJoin = LineJoin.Round;
                Draw(p, type, size);
            };
            return ve;
        }

        static Vector2 Pt(float x, float y, float s) => new Vector2(x / V * s, y / V * s);

        static void Draw(Painter2D p, IconType t, float s)
        {
            switch (t)
            {
                case IconType.Refresh:     Refresh(p, s); break;
                case IconType.Folder:      Folder(p, s); break;
                case IconType.File:        File(p, s); break;
                case IconType.Script:      Script(p, s); break;
                case IconType.Prefab:      Box(p, s); break;
                case IconType.Material:    Palette(p, s); break;
                case IconType.Image:       Picture(p, s); break;
                case IconType.GameObject:  Box(p, s); break;
                case IconType.Scene:       Film(p, s); break;
                case IconType.Error:       AlertTriangle(p, s); break;
                case IconType.Info:        InfoCircle(p, s); break;
                case IconType.Search:      Search(p, s); break;
                case IconType.Plus:        Plus(p, s); break;
                case IconType.Close:       Close(p, s); break;
                case IconType.Copy:        Copy(p, s); break;
                case IconType.Send:        Send(p, s); break;
                case IconType.Edit:        Pencil(p, s); break;
                case IconType.Lock:        Lock(p, s); break;
                case IconType.Account:     UserCircle(p, s); break;
                case IconType.Settings:    Gear(p, s); break;
                case IconType.ChevronDown: ChevronDown(p, s); break;
                case IconType.Check:       Check(p, s); break;
                case IconType.BookOpen:    BookOpen(p, s); break;
            }
        }

        // ---- Lucide refresh-cw ----
        static void Refresh(Painter2D p, float s)
        {
            float r = 9f * s / V;
            var c = Pt(12, 12, s);

            // Top-right arc
            p.BeginPath();
            p.Arc(c, r, Angle.Degrees(-60), Angle.Degrees(180), ArcDirection.Clockwise);
            p.Stroke();

            // Top-right arrow head: 21,3 → 21,9 → 15,9
            p.BeginPath();
            p.MoveTo(Pt(21, 3, s));
            p.LineTo(Pt(21, 9, s));
            p.LineTo(Pt(15, 9, s));
            p.Stroke();

            // Bottom-left arc
            p.BeginPath();
            p.Arc(c, r, Angle.Degrees(120), Angle.Degrees(0), ArcDirection.Clockwise);
            p.Stroke();

            // Bottom-left arrow head: 3,21 → 3,15 → 9,15
            p.BeginPath();
            p.MoveTo(Pt(3, 21, s));
            p.LineTo(Pt(3, 15, s));
            p.LineTo(Pt(9, 15, s));
            p.Stroke();
        }

        // ---- Lucide folder ----
        static void Folder(Painter2D p, float s)
        {
            // M4 20h16a2 2 0 0 0 2-2V8a2 2 0 0 0-2-2h-7.93a2 2 0 0 1-1.66-.9l-.82-1.2A2 2 0 0 0 7.93 3H4a2 2 0 0 0-2 2v13c0 1.1.9 2 2 2Z
            p.BeginPath();
            p.MoveTo(Pt(2, 7, s));
            p.LineTo(Pt(2, 19, s));
            p.LineTo(Pt(22, 19, s));
            p.LineTo(Pt(22, 9, s));
            p.LineTo(Pt(12, 9, s));
            p.LineTo(Pt(10, 5, s));
            p.LineTo(Pt(2, 5, s));
            p.LineTo(Pt(2, 7, s));
            p.ClosePath();
            p.Stroke();
        }

        // ---- Lucide file ----
        static void File(Painter2D p, float s)
        {
            // Rect outline with corner fold
            p.BeginPath();
            p.MoveTo(Pt(14, 3, s));
            p.LineTo(Pt(6, 3, s));
            p.LineTo(Pt(6, 21, s));
            p.LineTo(Pt(20, 21, s));
            p.LineTo(Pt(20, 9, s));
            p.LineTo(Pt(14, 3, s));
            p.ClosePath();
            p.Stroke();

            // Fold
            p.BeginPath();
            p.MoveTo(Pt(14, 3, s));
            p.LineTo(Pt(14, 9, s));
            p.LineTo(Pt(20, 9, s));
            p.Stroke();
        }

        // ---- Lucide code (angle brackets) ----
        static void Script(Painter2D p, float s)
        {
            // <  16 18 → 22 12 → 16 6  >
            p.BeginPath();
            p.MoveTo(Pt(16, 18, s));
            p.LineTo(Pt(22, 12, s));
            p.LineTo(Pt(16, 6, s));
            p.Stroke();

            p.BeginPath();
            p.MoveTo(Pt(8, 6, s));
            p.LineTo(Pt(2, 12, s));
            p.LineTo(Pt(8, 18, s));
            p.Stroke();
        }

        // ---- Lucide box (cube) ----
        static void Box(Painter2D p, float s)
        {
            // Outer diamond: top (12,2) → right (22,7) → bottom (22,17) → bottombottom (12,22) → left (2,17) → topleft (2,7) → top
            p.BeginPath();
            p.MoveTo(Pt(12, 2, s));
            p.LineTo(Pt(22, 7, s));
            p.LineTo(Pt(22, 17, s));
            p.LineTo(Pt(12, 22, s));
            p.LineTo(Pt(2, 17, s));
            p.LineTo(Pt(2, 7, s));
            p.LineTo(Pt(12, 2, s));
            p.ClosePath();
            p.Stroke();

            // Inner Y: top point down + diagonals
            p.BeginPath();
            p.MoveTo(Pt(2, 7, s));
            p.LineTo(Pt(12, 12, s));
            p.LineTo(Pt(22, 7, s));
            p.Stroke();

            p.BeginPath();
            p.MoveTo(Pt(12, 12, s));
            p.LineTo(Pt(12, 22, s));
            p.Stroke();
        }

        // ---- Lucide palette (simplified circle + dots) ----
        static void Palette(Painter2D p, float s)
        {
            // Outer circle
            p.BeginPath();
            p.Arc(Pt(12, 12, s), 9f * s / V, Angle.Degrees(0), Angle.Degrees(360));
            p.Stroke();

            // Three dot accents
            float r = 1.2f * s / V;
            FillDot(p, Pt(8, 8, s), r);
            FillDot(p, Pt(16, 8, s), r);
            FillDot(p, Pt(8, 14, s), r);
        }

        static void FillDot(Painter2D p, Vector2 center, float r)
        {
            p.BeginPath();
            p.Arc(center, r, Angle.Degrees(0), Angle.Degrees(360));
            p.Fill();
        }

        // ---- Lucide image ----
        static void Picture(Painter2D p, float s)
        {
            // Outer rect
            p.BeginPath();
            p.MoveTo(Pt(3, 3, s));
            p.LineTo(Pt(21, 3, s));
            p.LineTo(Pt(21, 21, s));
            p.LineTo(Pt(3, 21, s));
            p.ClosePath();
            p.Stroke();

            // Small circle (sun)
            p.BeginPath();
            p.Arc(Pt(8.5f, 8.5f, s), 1.5f * s / V, Angle.Degrees(0), Angle.Degrees(360));
            p.Stroke();

            // Mountain
            p.BeginPath();
            p.MoveTo(Pt(21, 15, s));
            p.LineTo(Pt(16, 10, s));
            p.LineTo(Pt(5, 21, s));
            p.Stroke();
        }

        // ---- Lucide film ----
        static void Film(Painter2D p, float s)
        {
            // Outer rect
            p.BeginPath();
            p.MoveTo(Pt(3, 3, s));
            p.LineTo(Pt(21, 3, s));
            p.LineTo(Pt(21, 21, s));
            p.LineTo(Pt(3, 21, s));
            p.ClosePath();
            p.Stroke();

            // Vertical lines (sprocket sides)
            Line(p, Pt(7, 3, s), Pt(7, 21, s));
            Line(p, Pt(17, 3, s), Pt(17, 21, s));

            // Horizontal middle
            Line(p, Pt(3, 12, s), Pt(21, 12, s));
        }

        // ---- Lucide alert-triangle ----
        static void AlertTriangle(Painter2D p, float s)
        {
            p.BeginPath();
            p.MoveTo(Pt(12, 3, s));
            p.LineTo(Pt(22, 20, s));
            p.LineTo(Pt(2, 20, s));
            p.ClosePath();
            p.Stroke();

            // Exclamation line
            Line(p, Pt(12, 10, s), Pt(12, 14, s));
            // Dot
            FillDot(p, Pt(12, 17, s), 1.0f * s / V);
        }

        // ---- Lucide info ----
        static void InfoCircle(Painter2D p, float s)
        {
            p.BeginPath();
            p.Arc(Pt(12, 12, s), 9f * s / V, Angle.Degrees(0), Angle.Degrees(360));
            p.Stroke();

            // i body
            Line(p, Pt(12, 11, s), Pt(12, 16, s));
            // i dot
            FillDot(p, Pt(12, 8, s), 1.0f * s / V);
        }

        // ---- Lucide search ----
        static void Search(Painter2D p, float s)
        {
            p.BeginPath();
            p.Arc(Pt(11, 11, s), 7f * s / V, Angle.Degrees(0), Angle.Degrees(360));
            p.Stroke();

            Line(p, Pt(16, 16, s), Pt(21, 21, s));
        }

        // ---- Plus ----
        static void Plus(Painter2D p, float s)
        {
            Line(p, Pt(5, 12, s), Pt(19, 12, s));
            Line(p, Pt(12, 5, s), Pt(12, 19, s));
        }

        // ---- X ----
        static void Close(Painter2D p, float s)
        {
            Line(p, Pt(6, 6, s), Pt(18, 18, s));
            Line(p, Pt(18, 6, s), Pt(6, 18, s));
        }

        // ---- Lucide copy (two overlapping rects) ----
        static void Copy(Painter2D p, float s)
        {
            // Front rect 8,8 → 20,20
            p.BeginPath();
            p.MoveTo(Pt(9, 9, s));
            p.LineTo(Pt(20, 9, s));
            p.LineTo(Pt(20, 20, s));
            p.LineTo(Pt(9, 20, s));
            p.ClosePath();
            p.Stroke();

            // Back L-shape
            p.BeginPath();
            p.MoveTo(Pt(5, 15, s));
            p.LineTo(Pt(4, 15, s));
            p.LineTo(Pt(4, 4, s));
            p.LineTo(Pt(15, 4, s));
            p.LineTo(Pt(15, 5, s));
            p.Stroke();
        }

        // ---- Lucide send (paper plane) ----
        static void Send(Painter2D p, float s)
        {
            // Outer triangle
            p.BeginPath();
            p.MoveTo(Pt(22, 2, s));
            p.LineTo(Pt(15, 22, s));
            p.LineTo(Pt(11, 13, s));
            p.LineTo(Pt(2, 9, s));
            p.ClosePath();
            p.Stroke();

            // Inner line
            Line(p, Pt(22, 2, s), Pt(11, 13, s));
        }

        // ---- Lucide pencil ----
        static void Pencil(Painter2D p, float s)
        {
            p.BeginPath();
            p.MoveTo(Pt(17, 3, s));
            p.LineTo(Pt(21, 7, s));
            p.LineTo(Pt(8, 20, s));
            p.LineTo(Pt(3, 21, s));
            p.LineTo(Pt(4, 16, s));
            p.ClosePath();
            p.Stroke();

            // Tip line
            Line(p, Pt(14, 6, s), Pt(18, 10, s));
        }

        // ---- Lucide lock ----
        static void Lock(Painter2D p, float s)
        {
            // Body
            p.BeginPath();
            p.MoveTo(Pt(4, 11, s));
            p.LineTo(Pt(20, 11, s));
            p.LineTo(Pt(20, 21, s));
            p.LineTo(Pt(4, 21, s));
            p.ClosePath();
            p.Stroke();

            // Shackle arc above body
            p.BeginPath();
            p.Arc(Pt(12, 11, s), 5f * s / V, Angle.Degrees(180), Angle.Degrees(360));
            p.Stroke();
        }

        // ---- Lucide circle-user ----
        static void UserCircle(Painter2D p, float s)
        {
            // Outer circle
            p.BeginPath();
            p.Arc(Pt(12, 12, s), 9f * s / V, Angle.Degrees(0), Angle.Degrees(360));
            p.Stroke();

            // Head
            p.BeginPath();
            p.Arc(Pt(12, 10, s), 3f * s / V, Angle.Degrees(0), Angle.Degrees(360));
            p.Stroke();

            // Shoulders arc
            p.BeginPath();
            p.Arc(Pt(12, 22, s), 7f * s / V, Angle.Degrees(200), Angle.Degrees(340));
            p.Stroke();
        }

        // ---- Lucide settings (simplified gear) ----
        static void Gear(Painter2D p, float s)
        {
            // Inner circle
            p.BeginPath();
            p.Arc(Pt(12, 12, s), 3f * s / V, Angle.Degrees(0), Angle.Degrees(360));
            p.Stroke();

            // 8 spokes (radial line teeth)
            for (int i = 0; i < 8; i++)
            {
                float a = i * 45f;
                var inner = PolarPt(12, 12, 5.5f, a, s);
                var outer = PolarPt(12, 12, 10f, a, s);
                Line(p, inner, outer);
            }
        }

        static Vector2 PolarPt(float cx, float cy, float r, float degrees, float s)
        {
            float rad = degrees * Mathf.Deg2Rad;
            return Pt(cx + r * Mathf.Cos(rad), cy + r * Mathf.Sin(rad), s);
        }

        // ---- Chevron down ----
        static void ChevronDown(Painter2D p, float s)
        {
            p.BeginPath();
            p.MoveTo(Pt(6, 9, s));
            p.LineTo(Pt(12, 15, s));
            p.LineTo(Pt(18, 9, s));
            p.Stroke();
        }

        // ---- Lucide check ----
        static void Check(Painter2D p, float s)
        {
            p.BeginPath();
            p.MoveTo(Pt(4, 12, s));
            p.LineTo(Pt(9, 17, s));
            p.LineTo(Pt(20, 6, s));
            p.Stroke();
        }

        // ---- Lucide book-open (for CLAUDE.md) ----
        static void BookOpen(Painter2D p, float s)
        {
            // Left half
            p.BeginPath();
            p.MoveTo(Pt(2, 4, s));
            p.LineTo(Pt(8, 4, s));
            p.LineTo(Pt(12, 7, s));
            p.LineTo(Pt(12, 20, s));
            p.LineTo(Pt(8, 18, s));
            p.LineTo(Pt(2, 18, s));
            p.ClosePath();
            p.Stroke();

            // Right half
            p.BeginPath();
            p.MoveTo(Pt(22, 4, s));
            p.LineTo(Pt(16, 4, s));
            p.LineTo(Pt(12, 7, s));
            p.LineTo(Pt(12, 20, s));
            p.LineTo(Pt(16, 18, s));
            p.LineTo(Pt(22, 18, s));
            p.ClosePath();
            p.Stroke();
        }

        // ---- Helpers ----
        static void Line(Painter2D p, Vector2 a, Vector2 b)
        {
            p.BeginPath();
            p.MoveTo(a);
            p.LineTo(b);
            p.Stroke();
        }
    }
}
