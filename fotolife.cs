using System;
using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

class HelloClass : Form {
    HelloClass() {
        this.TopMost = true;
        this.FormBorderStyle = FormBorderStyle.None;
        this.ShowInTaskbar = false;
    }

    private Bitmap CaptureScreen() {
        Bitmap bmpCapture = new Bitmap(rectCapture.Width, rectCapture.Height);
        Graphics gCapture = Graphics.FromImage(bmpCapture);
        gCapture.CopyFromScreen(rectCapture.Location, Point.Empty, rectCapture.Size);
        gCapture.Dispose();
        return bmpCapture;
    }

    protected override CreateParams CreateParams {
        get {
            Rectangle screenRect = SystemInformation.VirtualScreen;

            CreateParams createParams = base.CreateParams;
            createParams.X = screenRect.Left;
            createParams.Y = screenRect.Top;
            createParams.Width = screenRect.Width;
            createParams.Height = screenRect.Height;

            return createParams;
        }
    }

    private Rectangle rectCapture;
    private Point ptMouseDown;
    private bool isMouseDown = false;

    protected override void OnMouseDown(MouseEventArgs e) {
        isMouseDown = true;
        rectCapture.X = e.X;
        rectCapture.Y = e.Y;
        ptMouseDown.X = e.X;
        ptMouseDown.Y = e.Y;
    }

    protected override void OnMouseUp(MouseEventArgs e) {
        if (!isMouseDown)
            return;

        ControlPaint.DrawReversibleFrame(rectCapture, Color.Black, FrameStyle.Dashed);

        Bitmap bmpCapture = CaptureScreen();
        bmpCapture.Save("C:\\tmp\\hoge.bmp");
        //UploadFotolife();
        Application.Exit();

    }

    protected override void OnMouseMove(MouseEventArgs e) {
        if (!isMouseDown)
            return;

        ControlPaint.DrawReversibleFrame(rectCapture, Color.Black, FrameStyle.Dashed);

        rectCapture.Width  = Math.Abs(e.X - ptMouseDown.X);
        rectCapture.Height = Math.Abs(e.Y - ptMouseDown.Y);
        rectCapture.X      = Math.Min(e.X, ptMouseDown.X);
        rectCapture.Y      = Math.Min(e.Y, ptMouseDown.Y);

        ControlPaint.DrawReversibleFrame(rectCapture, Color.Black, FrameStyle.Dashed);
    }

    protected override void OnPaint(PaintEventArgs e) {
    }

    protected override void OnPaintBackground(PaintEventArgs e) {
    }

    static void Main() {
        Application.Run(new HelloClass());
    }
}
