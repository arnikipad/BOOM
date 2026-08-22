using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.Media;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.IO;
using System.Reflection;

namespace BOOM
{
    public partial class Form1 : Form
    {
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool ShowCursor(bool bShow);

        private Timer displayTimer;
        private PictureBox pictureBox;
        private SoundPlayer player;
        private int transparencyStep = 25;
        private int currentTransparency = 0;

        private Timer colorTimer;
        private Timer toggleTimer;
        private float hue = 0f;
        private int colorSpeed = 2;
        private Bitmap originalImage;
        private Bitmap coloredImage;
        private bool isColored = false;
        private bool isNatural = true;

        public Form1()
        {
            InitializeComponent();
            InitializeCustomComponents();
        }

        private void InitializeCustomComponents()
        {
            base.FormBorderStyle = FormBorderStyle.None;
            base.TransparencyKey = Color.Black;
            this.BackColor = Color.Black;
            ShowCursor(false);

            this.originalImage = LoadImageFromResources();

            if (this.originalImage == null)
            {
                this.originalImage = CreateDefaultImage();
            }

            this.pictureBox = new PictureBox();
            this.pictureBox.Image = this.originalImage;
            this.pictureBox.SizeMode = PictureBoxSizeMode.StretchImage;
            this.pictureBox.Dock = DockStyle.Fill;
            base.Controls.Add(this.pictureBox);

            this.displayTimer = new Timer();
            this.displayTimer.Interval = 1000;
            this.displayTimer.Tick += this.DisplayTimer_Tick;

            this.player = LoadSoundFromResources();
            if (this.player != null)
            {
                try
                {
                    this.player.PlayLooping();
                }
                catch { }
            }

            base.StartPosition = FormStartPosition.CenterScreen;
            base.ShowInTaskbar = false;
            base.TopMost = true;
            base.Shown += this.Form1_Shown;

            this.colorTimer = new Timer();
            this.colorTimer.Interval = 50;
            this.colorTimer.Tick += this.ColorTimer_Tick;
            this.colorTimer.Start();

            this.toggleTimer = new Timer();
            this.toggleTimer.Interval = 2000;
            this.toggleTimer.Tick += this.ToggleTimer_Tick;
            this.toggleTimer.Start();
        }

        private Bitmap LoadImageFromResources()
        {
            try
            {
                Assembly assembly = Assembly.GetExecutingAssembly();
                string[] resources = assembly.GetManifestResourceNames();

                string[] imageExtensions = { ".png", ".jpg", ".jpeg", ".bmp", ".gif" };

                foreach (string resource in resources)
                {
                    string resourceLower = resource.ToLower();

                    foreach (string ext in imageExtensions)
                    {
                        if (resourceLower.EndsWith(ext))
                        {
                            using (Stream stream = assembly.GetManifestResourceStream(resource))
                            {
                                if (stream != null)
                                {
                                    return new Bitmap(stream);
                                }
                            }
                        }
                    }
                }

                string[] imageFiles = Directory.GetFiles(Application.StartupPath, "*.png");
                if (imageFiles.Length == 0)
                {
                    imageFiles = Directory.GetFiles(Application.StartupPath, "*.jpg");
                }
                if (imageFiles.Length == 0)
                {
                    imageFiles = Directory.GetFiles(Application.StartupPath, "*.jpeg");
                }
                if (imageFiles.Length > 0)
                {
                    return new Bitmap(imageFiles[0]);
                }
            }
            catch { }

            return null;
        }

        private SoundPlayer LoadSoundFromResources()
        {
            try
            {
                Assembly assembly = Assembly.GetExecutingAssembly();
                string[] resources = assembly.GetManifestResourceNames();

                foreach (string resource in resources)
                {
                    if (resource.ToLower().EndsWith(".wav"))
                    {
                        Stream stream = assembly.GetManifestResourceStream(resource);
                        if (stream != null)
                        {
                            return new SoundPlayer(stream);
                        }
                    }
                }

                string[] soundFiles = Directory.GetFiles(Application.StartupPath, "*.wav");
                if (soundFiles.Length > 0)
                {
                    return new SoundPlayer(soundFiles[0]);
                }
            }
            catch { }

            return null;
        }

        private Bitmap CreateDefaultImage()
        {
            Bitmap bmp = new Bitmap(400, 300);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.Black);
                using (Font font = new Font("Arial", 20, FontStyle.Bold))
                using (SolidBrush brush = new SolidBrush(Color.White))
                {
                    string text = "BOOM!";
                    SizeF textSize = g.MeasureString(text, font);
                    float x = (400 - textSize.Width) / 2;
                    float y = (300 - textSize.Height) / 2;
                    g.DrawString(text, font, brush, x, y);
                }
            }
            return bmp;
        }

        private void Form1_Shown(object sender, EventArgs e)
        {
            this.displayTimer.Start();
        }

        private void DisplayTimer_Tick(object sender, EventArgs e)
        {
            this.currentTransparency += this.transparencyStep;
            if (this.currentTransparency >= 255 || this.currentTransparency <= 0)
            {
                this.transparencyStep = -this.transparencyStep;
            }

            if (this.isNatural)
            {
                Bitmap finalImage = new Bitmap(this.originalImage);
                this.SetImageOpacity(finalImage, this.currentTransparency);
                this.pictureBox.Image = finalImage;
            }
            else
            {
                Bitmap finalImage = new Bitmap(this.coloredImage);
                this.SetImageOpacity(finalImage, this.currentTransparency);
                this.pictureBox.Image = finalImage;
            }
        }

        private void ColorTimer_Tick(object sender, EventArgs e)
        {
            if (this.isColored)
            {
                this.hue += this.colorSpeed;
                if (this.hue >= 360f)
                {
                    this.hue = 0f;
                }
                this.coloredImage = this.ApplyColorToImage(this.originalImage, this.hue);
                Bitmap finalImage = new Bitmap(this.coloredImage);
                this.SetImageOpacity(finalImage, this.currentTransparency);
                this.pictureBox.Image = finalImage;
                this.pictureBox.Invalidate();
            }
        }

        private void ToggleTimer_Tick(object sender, EventArgs e)
        {
            if (this.isNatural)
            {
                this.isNatural = false;
                this.isColored = true;
                this.coloredImage = this.ApplyColorToImage(this.originalImage, this.hue);
                Bitmap finalImage = new Bitmap(this.coloredImage);
                this.SetImageOpacity(finalImage, this.currentTransparency);
                this.pictureBox.Image = finalImage;
            }
            else
            {
                this.isNatural = true;
                this.isColored = false;
                Bitmap finalImage = new Bitmap(this.originalImage);
                this.SetImageOpacity(finalImage, this.currentTransparency);
                this.pictureBox.Image = finalImage;
            }
            this.pictureBox.Invalidate();
        }

        private Bitmap ApplyColorToImage(Bitmap sourceImage, float hue)
        {
            Bitmap result = new Bitmap(sourceImage.Width, sourceImage.Height);
            ColorMatrix colorMatrix = new ColorMatrix();

            float num = hue * 3.1415927f / 180f;
            float cos = (float)Math.Cos(num);
            float sin = (float)Math.Sin(num);

            colorMatrix.Matrix00 = 0.213f + 0.787f * cos - 0.213f * sin;
            colorMatrix.Matrix01 = 0.715f - 0.715f * cos - 0.715f * sin;
            colorMatrix.Matrix02 = 0.072f - 0.072f * cos + 0.928f * sin;
            colorMatrix.Matrix10 = 0.213f - 0.213f * cos + 0.143f * sin;
            colorMatrix.Matrix11 = 0.715f + 0.285f * cos + 0.14f * sin;
            colorMatrix.Matrix12 = 0.072f - 0.072f * cos - 0.283f * sin;
            colorMatrix.Matrix20 = 0.213f - 0.213f * cos - 0.787f * sin;
            colorMatrix.Matrix21 = 0.715f - 0.715f * cos + 0.715f * sin;
            colorMatrix.Matrix22 = 0.072f + 0.928f * cos + 0.072f * sin;
            colorMatrix.Matrix33 = 1f;

            ImageAttributes attributes = new ImageAttributes();
            attributes.SetColorMatrix(colorMatrix, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);

            using (Graphics g = Graphics.FromImage(result))
            {
                g.DrawImage(sourceImage, new Rectangle(0, 0, sourceImage.Width, sourceImage.Height), 0, 0, sourceImage.Width, sourceImage.Height, GraphicsUnit.Pixel, attributes);
            }

            return result;
        }

        private void SetImageOpacity(Bitmap bmp, int transparency)
        {
            if (transparency < 0)
            {
                transparency = 0;
            }
            if (transparency > 255)
            {
                transparency = 255;
            }

            ColorMatrix colorMatrix = new ColorMatrix();
            colorMatrix.Matrix33 = (float)transparency / 255f;

            ImageAttributes imageAttributes = new ImageAttributes();
            imageAttributes.SetColorMatrix(colorMatrix, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);

            using (Graphics graphics = Graphics.FromImage(bmp))
            {
                graphics.DrawImage(bmp, new Rectangle(0, 0, bmp.Width, bmp.Height), 0, 0, bmp.Width, bmp.Height, GraphicsUnit.Pixel, imageAttributes);
            }
        }

        private void Form1_Load(object sender, EventArgs e) { }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {

            }
            base.Dispose(disposing);
        }
    }
}