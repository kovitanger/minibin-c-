using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace RecycleBinApp
{
    [StructLayout(LayoutKind.Sequential)]
    public struct SHQUERYRBINFO
    {
        public uint cbSize;
        public long i64Size;
        public long i64NumItems;
    }

    class Program
    {
        [DllImport("shell32.dll")]
        static extern int SHEmptyRecycleBin(IntPtr hwnd, string pszRootPath, uint dwFlags);

        [DllImport("shell32.dll")]
        static extern int SHQueryRecycleBin(IntPtr hwnd, ref SHQUERYRBINFO pSHQueryRBInfo);

        static NotifyIcon trayIcon;
        static Icon emptyIcon, fullIcon;
        static bool lastRecycleBinState;

        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();

            // Инициализация NotifyIcon
            trayIcon = new NotifyIcon
            {
                Icon = LoadIcon("icons/minibin-kt-empty.ico"),
                Visible = true,
                ContextMenuStrip = CreateContextMenu()
            };
            emptyIcon = trayIcon.Icon;
            fullIcon = LoadIcon("icons/minibin-kt-full.ico");

            // Проверка состояния корзины при запуске
            CheckRecycleBinStateOnStart();

            // Запуск фонового потока для обновления состояния
            Thread updateThread = new Thread(UpdateRecycleBinState);
            updateThread.IsBackground = true;
            updateThread.Start();

            Application.Run();
        }

        static Icon LoadIcon(string path)
        {
            Bitmap bitmap = new Bitmap(path);
            return Icon.FromHandle(bitmap.GetHicon());
        }

        static ContextMenuStrip CreateContextMenu()
        {
            ContextMenuStrip menu = new ContextMenuStrip();
            menu.Items.Add("Open Recycle Bin", null, new EventHandler(OpenRecycleBin));
            menu.Items.Add("Сlean the trash can", null, new EventHandler((s, e) => { EmptyRecycleBin(); UpdateTrayIcon(); }));
            menu.Items.Add("Exit", null, new EventHandler((s, e) => Application.Exit()));
            return menu;
        }

        static void OpenRecycleBin(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("shell:RecycleBinFolder");
        }

        static void EmptyRecycleBin()
        {
            int result = SHEmptyRecycleBin(IntPtr.Zero, null, 0x01);
            ShowNotification("The trash can is cleared", "Error code: " + result);
        }

        static void UpdateRecycleBinState()
        {
            while (true)
            {
                UpdateTrayIcon();
                Thread.Sleep(3000);
            }
        }

        static void UpdateTrayIcon()
        {
            bool isEmpty = IsRecycleBinEmpty();
            if (isEmpty != lastRecycleBinState)
            {
                trayIcon.Icon = isEmpty ? emptyIcon : fullIcon;
                lastRecycleBinState = isEmpty;
            }
        }

        static void CheckRecycleBinStateOnStart()
        {
            bool isEmpty = IsRecycleBinEmpty();
            trayIcon.Icon = isEmpty ? emptyIcon : fullIcon;
            lastRecycleBinState = isEmpty;
        }

        static bool IsRecycleBinEmpty()
        {
            SHQUERYRBINFO rbInfo = new SHQUERYRBINFO();
            rbInfo.cbSize = (uint)Marshal.SizeOf(typeof(SHQUERYRBINFO));
            int result = SHQueryRecycleBin(IntPtr.Zero, ref rbInfo);
            return result == 0 && rbInfo.i64NumItems == 0;
        }

        static void ShowNotification(string title, string message)
        {
            trayIcon.ShowBalloonTip(3000, title, message, ToolTipIcon.Info);
        }
    }
}
