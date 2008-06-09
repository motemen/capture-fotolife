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
using System.Security.Cryptography;

class CaptureFotolife : Form {
    static string HATENA_LOGIN_URL  = "http://www.hatena.ne.jp/login";
    static string FOTOLIFE_POST_URL = "http://f.hatena.ne.jp/motemen/up";
    //static string FOTOLIFE_POST_URL = "http://flocal.hatena.ne.jp:3000/motemen/up";
    //static string FOTOLIFE_POST_URL = "http://localhost/";

    Rectangle rectCapture;
    Point ptMouseDown;
    bool isMouseDown = false;

    CookieContainer cookieContainer;

    CaptureFotolife() {
        this.TopMost         = true;
        this.FormBorderStyle = FormBorderStyle.None;
        this.ShowInTaskbar   = false;
    }

    Bitmap CaptureScreen() {
        Bitmap bmpCapture = new Bitmap(rectCapture.Width, rectCapture.Height);
        Graphics gCapture = Graphics.FromImage(bmpCapture);
        gCapture.CopyFromScreen(this.RectangleToScreen(rectCapture).Location, Point.Empty, rectCapture.Size);
        gCapture.Dispose();
        return bmpCapture;
    }

    String SaveTemporary(Bitmap bitmap) {
        String fileTemp = Path.ChangeExtension(Path.GetTempFileName(), "png");
        bitmap.Save(fileTemp, ImageFormat.Png);
        Console.WriteLine(fileTemp);
        return fileTemp;
    }

    bool LoginHatena(string username, string password) {
        this.cookieContainer = new CookieContainer();

        HttpWebRequest request = (HttpWebRequest)WebRequest.Create(HATENA_LOGIN_URL);
        request.Method = "POST";
        request.CookieContainer = this.cookieContainer;

        byte[] requestContent = Encoding.ASCII.GetBytes(
            String.Format("name={0}&password={1}", username, password)
        );
        using (Stream requestStream = request.GetRequestStream()) {
            requestStream.Write(requestContent, 0, requestContent.Length);
        }

        request.GetResponse();

        foreach (Cookie c in this.cookieContainer.GetCookies(new Uri("http://f.hatena.ne.jp"))) {
            Console.WriteLine(c);
        }

        return true;
    }

    void Write(Stream stream, string s) {
        byte[] bytes = Encoding.ASCII.GetBytes(s);
        stream.Write(bytes, 0, s.Length);
    }

    bool UploadFotolife(String filepath) {
        LoginHatena("motemen", "QgXnG6b7");

        string BOUNDARY = "-----------------------------FOTOLIFEBOUNDARY";

        string rk = "";
        foreach (Cookie c in this.cookieContainer.GetCookies(new Uri("http://f.hatena.ne.jp"))) {
            if (c.Name == "rk") {
                rk = c.Value;
                break;
            }
        }
        MD5 md5 = MD5.Create();
        byte[] md5hash = md5.ComputeHash(Encoding.ASCII.GetBytes(rk));
        string rkm = System.Convert.ToBase64String(md5hash).Replace("=", "");
        Console.WriteLine(rkm);

        HttpWebRequest request = (HttpWebRequest)WebRequest.Create(FOTOLIFE_POST_URL);
        request.Method = "POST";
        request.CookieContainer = this.cookieContainer;
        request.ContentType = String.Format("multipart/form-data; boundary={0}", BOUNDARY);

        using (Stream requestStream = request.GetRequestStream()) {
            using (StreamWriter requestStreamWriter = new StreamWriter(requestStream)) {
                Func<string, bool> WriteToStream = s => {
                    byte[] bytes = Encoding.ASCII.GetBytes(s);
                    requestStream.Write(bytes, 0, bytes.Length);
                    return true;
                };

                requestStreamWriter.Write(String.Format("--{0}\r\n", BOUNDARY));

                requestStreamWriter.Write(String.Format(@"Content-Disposition: form-data; name=""image1""; filename=""{0}""" + "\r\n", Path.GetFileName(filepath)));
                requestStreamWriter.Write("Content-Type: image/png\r\n");
                requestStreamWriter.Write("\r\n");
                requestStreamWriter.Flush();

                using (FileStream fileStream = new FileStream(filepath, FileMode.Open)) {
                    int fileSize = (int)fileStream.Length;
                    byte[] fileContent = new byte[fileSize];
                    fileStream.Read(fileContent, 0, fileSize);
                    requestStream.Write(fileContent, 0, fileContent.Length);
                    requestStreamWriter.Write("\r\n");
                }

                requestStreamWriter.Write(String.Format("--{0}\r\n", BOUNDARY));
                requestStreamWriter.Write(@"Content-Disposition: form-data; name=""mode""" + "\r\n");
                requestStreamWriter.Write("\r\n");
                requestStreamWriter.Write("enter\r\n");

                requestStreamWriter.Write(String.Format("--{0}\r\n", BOUNDARY));
                requestStreamWriter.Write(@"Content-Disposition: form-data; name=""rkm""" + "\r\n");
                requestStreamWriter.Write("\r\n");
                requestStreamWriter.Write(String.Format("{0}\r\n", rkm));

                requestStreamWriter.Write(String.Format("--{0}--\r\n", BOUNDARY));
        }

        request.GetResponse();

        return false;
        }
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

        ControlPaint.DrawReversibleFrame(this.RectangleToScreen(rectCapture), Color.Black, FrameStyle.Dashed);

        Bitmap bmpCapture   = CaptureScreen();
        String fileCaptured = SaveTemporary(bmpCapture);
        UploadFotolife(fileCaptured);
        //UploadFotolife(@"C:\tmp\hoge.hs");
        Application.Exit();

    }

    protected override void OnMouseMove(MouseEventArgs e) {
        if (!isMouseDown)
            return;

        ControlPaint.DrawReversibleFrame(this.RectangleToScreen(rectCapture), Color.Black, FrameStyle.Dashed);

        rectCapture.Width  = Math.Abs(e.X - ptMouseDown.X);
        rectCapture.Height = Math.Abs(e.Y - ptMouseDown.Y);
        rectCapture.X      = Math.Min(e.X, ptMouseDown.X);
        rectCapture.Y      = Math.Min(e.Y, ptMouseDown.Y);

        ControlPaint.DrawReversibleFrame(this.RectangleToScreen(rectCapture), Color.Black, FrameStyle.Dashed);
    }

    protected override void OnPaint(PaintEventArgs e) { }

    protected override void OnPaintBackground(PaintEventArgs e) { }

    static void Main() {
        Application.Run(new CaptureFotolife());
    }
}
