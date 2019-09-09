using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RuntimeUnityEditor.UI
{
    public static class Export
    {
        private static bool guiInitialized = false;
        private static Thread guiThread;

        static Export()
        {
            Application.ThreadException += ApplicationOnThreadException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomainOnUnhandledException;
        }

        private static void CurrentDomainOnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            MessageBox.Show(e.ExceptionObject.ToString(), "Unhandled domain exception!", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        private static void ApplicationOnThreadException(object sender, ThreadExceptionEventArgs e)
        {
            MessageBox.Show(e.Exception.ToString(), "Unhandled application exception!", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        [DllExport(CallingConvention.StdCall)]
        public static void ShowGUI()
        {
            if (!guiInitialized)
                StartGUI();
            else
                GUIMain.form.Invoke((Action)(() => { GUIMain.form.Visible = true; }));
        }

        private static void StartGUI()
        {
            guiThread = new Thread(GUIMain.Run);
            guiThread.Start();
            guiInitialized = true;
        }
    }

    internal static class GUIMain
    {
        public static Form1 form;

        [STAThread]
        public static void Run()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            form = new Form1();
            Application.Run(form);
        }
    }
}
