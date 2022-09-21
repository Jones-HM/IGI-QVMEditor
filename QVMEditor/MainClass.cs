using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace QVM_Editor
{
    static class MainClass
    {
        [STAThread]
        static void Main(string[] args)
        {
            try
            {
                bool instanceCount = false;
                Mutex mutex = null;
                var projAppName = AppDomain.CurrentDomain.FriendlyName;
                AppDomain.CurrentDomain.ProcessExit += new EventHandler(OnAppExit);

                mutex = new Mutex(true, projAppName, out instanceCount);
                if (instanceCount)
                {
                    if (args.Length > 0)
                    {
                        var fileName = args[0];
                        QVMEditorForm.openFileName = fileName;
                    }

                    Application.EnableVisualStyles();
                    Application.SetCompatibleTextRenderingDefault(false);
                    Application.Run(new QVMEditorForm());
                    mutex.ReleaseMutex();
                }
                else
                {
                    QUtils.ShowError("QVM Editor is already running");
                }
            }
            catch (Exception ex)
            {
                QUtils.ShowLogException("QVMEditor", ex);
            }
        }

        private static void OnAppExit(object sender, EventArgs e)
        {
            var directoryInfo = new DirectoryInfo(QUtils.appOutPath);

            foreach (FileInfo file in directoryInfo.GetFiles())
            {
                file.Delete();
            }
        }
    }
}
