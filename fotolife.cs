using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

class CaptureFotolife : Form {
    static string HATENA_LOGIN_URL  = "http://www.hatena.ne.jp/login";
    static string FOTOLIFE_POST_URL = "http://f.hatena.ne.jp/{0}/up";

    string username;
    string password;

    Rectangle rectCapture;
    Point ptMouseDown;
    bool isMouseDown = false;

    CookieContainer cookieContainer;

    CaptureFotolife(string username, string password) {
        this.TopMost         = true;
        this.FormBorderStyle = FormBorderStyle.None;
        this.ShowInTaskbar   = false;
        this.cookieContainer = new CookieContainer();
        this.username = username;
        this.password = password;
    }

    Bitmap CaptureScreen() {
        if (rectCapture.Width == 0 || rectCapture.Height == 0) {
            return null;
        }

        Bitmap bmpCapture = new Bitmap(rectCapture.Width, rectCapture.Height);
        using (Graphics gCapture = Graphics.FromImage(bmpCapture)) {
            gCapture.CopyFromScreen(
                this.RectangleToScreen(rectCapture).Location, Point.Empty, rectCapture.Size
            );
        }
        return bmpCapture;
    }

    string SaveTemporary(Bitmap bitmap) {
        string fileTemp = Path.ChangeExtension(Path.GetTempFileName(), "png");
        bitmap.Save(fileTemp, System.Drawing.Imaging.ImageFormat.Png);
        return fileTemp;
    }

    string LoginHatena(string username, string password) {
        HttpWebRequest request = (HttpWebRequest)WebRequest.Create(HATENA_LOGIN_URL);
        request.Method = "POST";
        request.CookieContainer = this.cookieContainer;
        request.AllowAutoRedirect = false;

        using (Stream requestStream = request.GetRequestStream()) {
            byte[] requestContent = System.Text.Encoding.ASCII.GetBytes(
                String.Format("name={0}&password={1}&persistent=1", username, password)
            );
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

    bool UploadFotolife(string filepath) {
        const string BOUNDARY = "-----------------------------FOTOLIFEBOUNDARY";

        string rk = LoginHatena(username, password);
        byte[] md5hash = MD5.Create().ComputeHash(Encoding.ASCII.GetBytes(rk));
        string rkm = System.Convert.ToBase64String(md5hash).Replace("=", "");

        HttpWebRequest request = (HttpWebRequest)WebRequest.Create(String.Format(FOTOLIFE_POST_URL, username));
        request.Method = "POST";
        request.CookieContainer = this.cookieContainer;

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

            WebResponse res = request.GetResponse();
            StreamReader reader = new StreamReader(res.GetResponseStream());

            Regex regex = new Regex(@"check-(\d+)");
            Match match = regex.Match(reader.ReadToEnd());

            if (match.Success) {
                Process.Start(String.Format("http://f.hatena.ne.jp/{0}/{1}", username, match.Groups[1]));
            } else {
                throw new System.Exception("Upload failed");
            }

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

    static void Main(string[] args) {
        if (args.Length < 2) {
            MessageBox.Show("Usage: fotolife.exe username password", "CaptureFotolife", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        String username = args[0];
        String password = args[1];

        Application.Run(new CaptureFotolife(username, password));
    }
}
