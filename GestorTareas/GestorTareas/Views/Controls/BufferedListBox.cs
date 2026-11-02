using System.ComponentModel;
using System.Drawing.Drawing2D;
using System.Globalization;

namespace TrayTasks.Views;

class BufferedListBox : ListBox
{
    public BufferedListBox() { DoubleBuffered = true; }

    public event EventHandler? ScrollPositionChanged;

    protected override void WndProc(ref Message m)
    {
        base.WndProc(ref m);
        if (m.Msg is 0x0115 or 0x020A or 0x0100) // WM_VSCROLL, WM_MOUSEWHEEL, WM_KEYDOWN
            ScrollPositionChanged?.Invoke(this, EventArgs.Empty);
    }
}

/// <summary>Barra vertical minimalista para la lista de tareas.</summary>
