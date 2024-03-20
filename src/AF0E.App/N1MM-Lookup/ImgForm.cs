using gma.System.Windows;

namespace N1MMLookup;

public partial class ImgForm : Form
{
#pragma warning disable CA2213
    private Control _parent = null!;
#pragma warning restore CA2213
    private readonly UserActivityHook _actHook;
    private Size _imgSize;

    public ImgForm()
    {
        _actHook = new UserActivityHook();
        InitializeComponent();
    }

#pragma warning disable CA1054
    public void LoadImage(Control parent, string url, Size imgSize)
#pragma warning restore CA1054
    {
        _parent = parent;
        _imgSize = imgSize;
        if (string.Equals(picBoxBig.ImageLocation, url, StringComparison.OrdinalIgnoreCase))
            AfterImageLoaded();
        else
            picBoxBig.LoadAsync(url);
    }

    public void CloseImage()
    {
        Hide();
    }

    private void Center()
    {
        var screen = Screen.FromControl(_parent);

        var workingArea = screen.WorkingArea;
        Location = new Point
        {
            //X = Math.Max(workingArea.X, workingArea.X + (workingArea.Width - Width) / 2),
            //Y = Math.Max(workingArea.Y, workingArea.Y + (workingArea.Height - Height) / 2)
            X = Math.Max(workingArea.X, workingArea.X + (workingArea.Width - _imgSize.Width) / 2),
            Y = Math.Max(workingArea.Y, workingArea.Y + (workingArea.Height - _imgSize.Height) / 2)
        };
    }

    private void MouseMoved(object? sender, MouseEventArgs e)
    {
        if (e.Clicks <= 0) return;

        _actHook.OnMouseActivity -= MouseMoved;
        CloseImage();
    }

    private void KeyPressed(object? o, KeyPressEventArgs args)
    {
        if (args.KeyChar != 27) return;

        _actHook.KeyPress -= KeyPressed;
        CloseImage();
    }

    private void AfterImageLoaded()
    {
        _actHook.OnMouseActivity += MouseMoved;
        _actHook.KeyPress += KeyPressed;
        Show();
        Center();
    }

    private void picBoxBig_LoadCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
    {
        AfterImageLoaded();
    }
}
