using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Threading;
using MessageBox = System.Windows.Forms.MessageBox;

namespace Wabbajack_Automagic
{
    public partial class MainWindow : Window
    {
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        static extern bool SetCursorPos(int x, int y);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        static extern void mouse_event(int dwFlags, int dx, int dy, int dwData, int dwExtraInfo);

        private const int MOUSEEVENTF_LEFTDOWN = 0x02;
        private const int MOUSEEVENTF_LEFTUP = 0x04;

        private DispatcherTimer magicTimer;

        private Bitmap currentScreen, slowButton;
        private System.Drawing.Point? currentPoint;

        public MainWindow()
        {
            InitializeComponent();
            textOutput.CaretBrush = new SolidColorBrush(System.Windows.Media.Color.FromArgb(0, 0, 0, 0));
            slowButton = new Bitmap(".\\img\\slow download.png");
            delayInput.Text = "5";
            magicTimer = new System.Windows.Threading.DispatcherTimer();
            magicTimer.Tick += new EventHandler(magicTimer_Tick);
            magicTimer.Interval = new TimeSpan(0, 0, 5);
            statusLabel.Content = "INACTIVE";
            statusLabel.Foreground = System.Windows.Media.Brushes.Red;
        }

        private void ButtonStart_Click(object sender, RoutedEventArgs e)
        {
            if (delayInput.Text == "")
            {
                System.Windows.Forms.MessageBox.Show("Enter delay", "Error");
            }
            else
            {
                updateDelay(Int32.Parse(delayInput.Text));
            }

            magicTimer.Start();
            statusLabel.Content = "ACTIVE";
            statusLabel.Foreground = System.Windows.Media.Brushes.Green;
            outputToConsole("Automagic started.");
        }

        private void ButtonStop_Click(object sender, RoutedEventArgs e)
        {
            magicTimer.Stop();
            statusLabel.Content = "INACTIVE";
            statusLabel.Foreground = System.Windows.Media.Brushes.Red;
            outputToConsole("Automagic stopped.");
        }

        private void magicTimer_Tick(object sender, EventArgs e)
        {
            outputToConsole("Checking for button");
            SetCursorPos(0, 0);
            currentScreen = Screenshot();
            currentPoint = Find(currentScreen, slowButton);
            if (currentPoint.HasValue)
            {
                outputToConsole("Found button at: (" + currentPoint.Value.X + "," + currentPoint.Value.Y + ")");
                clickMouse(currentPoint.Value.X + (slowButton.Width / 2), currentPoint.Value.Y + (slowButton.Height / 2));
                wait(5000);
                SetCursorPos(0, 0);
            }
            else
            {
                outputToConsole("Couldn't find button");
            }
        }

        private void clearConsole()
        {
            textOutput.Text = "";
        }
        private void outputToConsole(string output)
        {
            textOutput.AppendText("\n" + output);
            textOutput.Focus();
            textOutput.CaretIndex = textOutput.Text.Length;
            textOutput.ScrollToEnd();
        }
        private static void clickMouse(int Xposition, int Yposition)
        {
            SetCursorPos(Xposition, Yposition);
            mouse_event(MOUSEEVENTF_LEFTDOWN, Xposition, Yposition, 0, 0);
            mouse_event(MOUSEEVENTF_LEFTUP, Xposition, Yposition, 0, 0);
        }
        public Bitmap Screenshot()
        {
            var screenShot = new Bitmap(Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height);

            using (var g = Graphics.FromImage(screenShot))
            {
                g.CopyFromScreen(0, 0, 0, 0, Screen.PrimaryScreen.Bounds.Size);
            }

            return screenShot;
        }

        public void outputBitmap(Bitmap image)
        {
            image.Save("test.png");
        }

        public System.Drawing.Point? Find(Bitmap haystack, Bitmap needle)
        {
            if (null == haystack || null == needle)
            {
                return null;
            }
            if (haystack.Width < needle.Width || haystack.Height < needle.Height)
            {
                return null;
            }

            var haystackArray = GetPixelArray(haystack);
            var needleArray = GetPixelArray(needle);

            foreach (var firstLineMatchPoint in FindMatch(haystackArray.Take(haystack.Height - needle.Height), needleArray[0]))
            {
                if (IsNeedlePresentAtLocation(haystackArray, needleArray, firstLineMatchPoint, 1))
                {
                    return firstLineMatchPoint;
                }
            }

            return null;
        }

        private int[][] GetPixelArray(Bitmap bitmap)
        {
            var result = new int[bitmap.Height][];
            var bitmapData = bitmap.LockBits(new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly,
                System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            for (int y = 0; y < bitmap.Height; ++y)
            {
                result[y] = new int[bitmap.Width];
                Marshal.Copy(bitmapData.Scan0 + y * bitmapData.Stride, result[y], 0, result[y].Length);
            }

            bitmap.UnlockBits(bitmapData);

            return result;
        }

        private IEnumerable<System.Drawing.Point> FindMatch(IEnumerable<int[]> haystackLines, int[] needleLine)
        {
            var y = 0;
            foreach (var haystackLine in haystackLines)
            {
                for (int x = 0, n = haystackLine.Length - needleLine.Length; x < n; ++x)
                {
                    if (ContainSameElements(haystackLine, x, needleLine, 0, needleLine.Length))
                    {
                        yield return new System.Drawing.Point(x, y);
                    }
                }
                y += 1;
            }
        }

        private bool ContainSameElements(int[] first, int firstStart, int[] second, int secondStart, int length)
        {
            for (int i = 0; i < length; ++i)
            {
                if (first[i + firstStart] != second[i + secondStart])
                {
                    return false;
                }
            }
            return true;
        }

        private bool IsNeedlePresentAtLocation(int[][] haystack, int[][] needle, System.Drawing.Point point, int alreadyVerified)
        {
            //we already know that "alreadyVerified" lines already match, so skip them
            for (int y = alreadyVerified; y < needle.Length; ++y)
            {
                if (!ContainSameElements(haystack[y + point.Y], point.X, needle[y], 0, needle.Length))
                {
                    return false;
                }
            }
            return true;
        }

        private void delayInput_KeyDown(object sender, KeyPressEventArgs e)
        {
            string st = "0123456789" + (char)8;
            if (st.IndexOf(e.KeyChar) == -1)
            {
                MessageBox.Show("please enter digits only");
                e.Handled = true;
            }
        }
        private void updateDelay(int delay)
        {
            magicTimer.Interval = new TimeSpan(0, 0, delay);
        }

        public void wait(int milliseconds)
        {
            var timer1 = new System.Windows.Forms.Timer();
            if (milliseconds == 0 || milliseconds < 0)
            {
                return;
            }

            // Console.WriteLine("start wait timer");
            timer1.Interval = milliseconds;
            timer1.Enabled = true;
            timer1.Start();

            timer1.Tick += (s, e) =>
            {
                timer1.Enabled = false;
                timer1.Stop();
                // Console.WriteLine("stop wait timer");
            };

            while (timer1.Enabled)
            {
                System.Windows.Forms.Application.DoEvents();
            }
        }
    }
}
