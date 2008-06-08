using System;
using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
//using System.Runtime.InteropServices;
using System.IO;
using System.Net;

class CaptureFotolife : Form {
    static string FOTOLIFE_POST_URL = "http://f.hatena.ne.jp/{0}/up";

    Rectangle rectCapture;
    Point ptMouseDown;
    bool isMouseDown = false;

    CaptureFotolife() {
        this.TopMost         = true;
        this.FormBorderStyle = FormBorderStyle.None;
        this.ShowInTaskbar   = false;
    }

    Bitmap CaptureScreen() {
        Bitmap bmpCapture = new Bitmap(rectCapture.Width, rectCapture.Height);
        Graphics gCapture = Graphics.FromImage(bmpCapture);
        gCapture.CopyFromScreen(rectCapture.Location, Point.Empty, rectCapture.Size);
        gCapture.Dispose();
        return bmpCapture;
    }

    String SaveTemporary(Bitmap bitmap) {
        String fileTemp = Path.ChangeExtension(Path.GetTempFileName(), "png");
        bitmap.Save(fileTemp, ImageFormat.Png);
        Console.WriteLine(fileTemp);
        return fileTemp;
    }

    bool UploadFotolife(String filepath) {
        WebClient client = new WebClient();
        //client.UploadValues(FOTOLIFE_POST_URL, nameValueCollection);
        return false;
    }

    protected override CreateParams CreateParams {
        get {
            Rectangle screenRect = SystemInformation.VirtualScreen;

            CreateParams createParams = base.CreateParams;
            createParams.X      = screenRect.Left;
            createParams.Y      = screenRect.Top;
            createParams.Width  = screenRect.Width;
            createParams.Height = screenRect.Height;

            return createParams;
        }
    }

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

        Bitmap bmpCapture   = CaptureScreen();
        String fileCaptured = SaveTemporary(bmpCapture);
        //UploadFotolife(fileCaptured);
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

    protected override void OnPaint(PaintEventArgs e) { }

    protected override void OnPaintBackground(PaintEventArgs e) { }

    static void Main() {
        Application.Run(new CaptureFotolife());
    }
}
