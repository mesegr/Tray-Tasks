using System.ComponentModel;
using System.Drawing.Drawing2D;
using System.Globalization;

namespace TrayTasks.Views;

static class Ui
{
    public static int RadioEsquina { get; private set; } = 16;

    public static Color Fondo { get; private set; } = Color.FromArgb(28, 28, 30);
    public static Color Tarjeta { get; private set; } = Color.FromArgb(44, 44, 46);
    public static Color Hover { get; private set; } = Color.FromArgb(54, 54, 56);
    public static Color Separador { get; private set; } = Color.FromArgb(62, 62, 64);
    public static Color Gris { get; private set; } = Color.FromArgb(142, 142, 147);
    public static Color Azul { get; private set; } = Color.FromArgb(10, 132, 255);
    public static Color AzulClaro { get; private set; } = Color.FromArgb(64, 156, 255);
    public static Color Verde { get; private set; } = Color.FromArgb(48, 209, 88);
    public static Color Rojo { get; private set; } = Color.FromArgb(255, 69, 58);
    public static Color Naranja { get; private set; } = Color.FromArgb(255, 159, 10);
    public static Color Texto { get; private set; } = Color.White;

    public static void EstablecerRadioEsquina(int radio) => RadioEsquina = Math.Max(1, radio);

    public static void EstablecerTema(bool claro)
    {
        Fondo = claro ? Color.FromArgb(242, 242, 247) : Color.FromArgb(28, 28, 30);
        Tarjeta = claro ? Color.White : Color.FromArgb(44, 44, 46);
        Hover = claro ? Color.FromArgb(229, 229, 234) : Color.FromArgb(54, 54, 56);
        Separador = claro ? Color.FromArgb(209, 209, 214) : Color.FromArgb(62, 62, 64);
        Gris = claro ? Color.FromArgb(99, 99, 102) : Color.FromArgb(142, 142, 147);
        Texto = claro ? Color.FromArgb(28, 28, 30) : Color.White;
        Azul = Color.FromArgb(10, 132, 255);
        AzulClaro = Color.FromArgb(0, 122, 255);
        Verde = Color.FromArgb(48, 209, 88);
        Rojo = Color.FromArgb(255, 69, 58);
        Naranja = Color.FromArgb(255, 159, 10);
    }

    public static GraphicsPath RoundedRect(Rectangle r, int radius)
    {
        int d = Math.Min(radius * 2, Math.Min(r.Width, r.Height));
        var p = new GraphicsPath();
        p.AddArc(r.X, r.Y, d, d, 180, 90);
        p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
        p.CloseFigure();
        return p;
    }

    // Dibuja una flecha ‹ o ›
    public static void Chevron(Graphics g, float cx, float cy, float ancho, float alto, bool derecha, Color color, float grosor)
    {
        float dir = derecha ? -1f : 1f;
        using var pen = new Pen(color, grosor)
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round,
            LineJoin = LineJoin.Round
        };
        g.DrawLines(pen, new[]
        {
            new PointF(cx + dir * ancho, cy - alto),
            new PointF(cx - dir * ancho, cy),
            new PointF(cx + dir * ancho, cy + alto)
        });
    }
}

// Panel con fondo redondeado (las "tarjetas" de iOS)
