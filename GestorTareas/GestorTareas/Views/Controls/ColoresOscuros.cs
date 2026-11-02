using System.ComponentModel;
using System.Drawing.Drawing2D;
using System.Globalization;

namespace TrayTasks.Views;

class ColoresOscuros : ProfessionalColorTable
{
    static readonly Color Bg = Ui.Tarjeta;
    static readonly Color Sel = Color.FromArgb(72, 72, 74);

    public override Color ToolStripDropDownBackground => Bg;
    public override Color ImageMarginGradientBegin => Bg;
    public override Color ImageMarginGradientMiddle => Bg;
    public override Color ImageMarginGradientEnd => Bg;
    public override Color MenuBorder => Ui.Separador;
    public override Color MenuItemBorder => Sel;
    public override Color MenuItemSelected => Sel;
    public override Color MenuItemSelectedGradientBegin => Sel;
    public override Color MenuItemSelectedGradientEnd => Sel;
}

// =====================================================================
//  Ventana principal + icono de bandeja
