using System;
using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
//using System.Runtime.InteropServices;
using System.IO;
using System.Net;
using System.Collections.Specialized;
using System.Text;

class CaptureFotolife : Form {
    //static string FOTOLIFE_POST_URL = "http://f.hatena.ne.jp/{0}/up";
    static string FOTOLIFE_POST_URL = "http://localhost/";

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
        string BOUNDARY = "-----------------------------FOTOLIFEBOUNDARY";

        HttpWebRequest request = (HttpWebRequest)WebRequest.Create(FOTOLIFE_POST_URL);
        request.Method = "POST";
        request.ContentType = String.Format("multipart/form-data; boundary={0}", BOUNDARY);

        StringBuilder contentBuilder = new StringBuilder();
        contentBuilder.Append("--").Append(BOUNDARY).Append("\r\n");
        contentBuilder.Append(String.Format(@"Content-Disposition: form-data; name=""file""; filename=""{0}""", Path.GetFileName(filepath)))
                      .Append("\r\n");
        contentBuilder.Append("Content-Type: image/png\r\n");
        contentBuilder.Append("\r\n");
        using (StreamReader fileReader = new StreamReader(filepath, Encoding.ASCII)) {
            contentBuilder.Append(fileReader.ReadToEnd());
            contentBuilder.Append("\r\n");
        }
        contentBuilder.Append("--").Append(BOUNDARY).Append("--").Append("\r\n");
        byte[] requestContent = Encoding.ASCII.GetBytes(contentBuilder.ToString());
        Console.WriteLine(contentBuilder);

        request.ContentLength = requestContent.Length;
        request.ProtocolVersion = HttpVersion.Version10;

        using (Stream requestStream = request.GetRequestStream()) {
            requestStream.Write(requestContent, 0, requestContent.Length);
        }

        try {
            using (WebResponse response = request.GetResponse()) {
                foreach (string key in response.Headers.AllKeys) {
                  Console.WriteLine("[{0}]\n\t{1}", key, response.Headers[key]);
                }
            }
        } catch (Exception e) {
            Console.WriteLine(e);
        }

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
        UploadFotolife(@"C:\tmp\hoge.hs");
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
