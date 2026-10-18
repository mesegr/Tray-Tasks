using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace TareasBandeja.Views;

internal sealed class AppTextBox : TextBox
{
    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        TextInputMargins.Apply(Handle);
    }
}

internal sealed class AppMaskedTextBox : MaskedTextBox
{
    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        TextInputMargins.Apply(Handle);
    }
}

internal static class TextInputMargins
{
    private const int EmSetMargins = 0x00D3;
    private const int EcLeftMargin = 0x0001;
    private const int EcRightMargin = 0x0002;
    private const int MargenIzquierdo = 10;
    private const int MargenDerecho = 8;

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr SendMessage(IntPtr hWnd, int message, IntPtr wParam, IntPtr lParam);

    public static void Apply(IntPtr handle)
    {
        int margins = (MargenDerecho << 16) | MargenIzquierdo;
        SendMessage(handle, EmSetMargins, new IntPtr(EcLeftMargin | EcRightMargin), new IntPtr(margins));
    }
}
