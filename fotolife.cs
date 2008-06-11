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
using System.Xml.Serialization;

class CaptureFotolife : Form {
    static string HATENA_LOGIN_URL  = "http://www.hatena.ne.jp/login";
    static string FOTOLIFE_POST_URL = "http://f.hatena.ne.jp/{0}/up";

    Rectangle rectCapture;
    Point ptMouseDown;
    bool isMouseDown = false;

    CookieContainer cookieContainer;

    CaptureFotolife() {
        this.TopMost         = true;
        this.FormBorderStyle = FormBorderStyle.None;
        this.ShowInTaskbar   = false;
        this.cookieContainer = new CookieContainer();
    }

    Bitmap CaptureScreen() {
        if (rectCapture.Width == 0 || rectCapture.Height == 0) {
            return null;
        }

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

    string LoginHatena(string username, string password) {
        Console.WriteLine("LoginHatena");

        HttpWebRequest request = (HttpWebRequest)WebRequest.Create(HATENA_LOGIN_URL);
        request.Method = "POST";
        request.CookieContainer = this.cookieContainer;
        request.AllowAutoRedirect = false;

        byte[] requestContent = Encoding.ASCII.GetBytes(
            String.Format("name={0}&password={1}&persistent=1", username, password)
        );
        using (Stream requestStream = request.GetRequestStream()) {
            requestStream.Write(requestContent, 0, requestContent.Length);
        }

        request.GetResponse();

        foreach (Cookie c in this.cookieContainer.GetCookies(new Uri("http://f.hatena.ne.jp"))) {
            if (c.Name == "rk") {
                return c.Value;
            }
        }

        throw new System.Exception("Could not login");
    }

    string RestoreRK() {
        Console.WriteLine("RestoreRK");
        try {
            using (StreamReader reader = new StreamReader("rk")) {
                String line = reader.ReadLine();
                Console.WriteLine(line);
                return line;
            }
        } catch {
            return null;
        }
    }

    void StoreRK(string rk) {
        Console.WriteLine("StoreRK");
        using (StreamWriter writer = new StreamWriter("rk")) {
            writer.WriteLine(rk);
        }
    }

    bool UploadFotolife(String filepath) {
        string username, password;
        using (StreamReader reader = new StreamReader("login")) {
            username = reader.ReadLine();
            password = reader.ReadLine();
        }
        string rk = RestoreRK();

        if (rk == null) {
            rk = LoginHatena(username, password);
            StoreRK(rk);
        } else {
            this.cookieContainer.SetCookies(new Uri("http://f.hatena.ne.jp"), String.Format("rk={0}", rk));
        }

        Console.WriteLine("Logged in: rk=[{0}]", rk);

        const string BOUNDARY = "-----------------------------FOTOLIFEBOUNDARY";

        MD5 md5 = MD5.Create();
        byte[] md5hash = md5.ComputeHash(Encoding.ASCII.GetBytes(rk));
        string rkm = System.Convert.ToBase64String(md5hash).Replace("=", "");
        Console.WriteLine(rkm);

        HttpWebRequest request = (HttpWebRequest)WebRequest.Create(String.Format(FOTOLIFE_POST_URL, username));
        request.Method = "POST";
        request.CookieContainer = this.cookieContainer;
        request.AllowAutoRedirect = false;

        request.ContentType = String.Format("multipart/form-data; boundary={0}", BOUNDARY);

        using (Stream requestStream = request.GetRequestStream()) {
            using (StreamWriter requestStreamWriter = new StreamWriter(requestStream)) {
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
        if (e.Button == MouseButtons.Right) {
            Application.Exit();
        }

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

        this.Visible = false;

        Bitmap bmpCapture = CaptureScreen();
        if (bmpCapture != null) {
            String fileCaptured = SaveTemporary(bmpCapture);
            UploadFotolife(fileCaptured);
        }
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
