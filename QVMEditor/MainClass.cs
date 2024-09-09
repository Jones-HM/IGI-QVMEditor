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
                int semaphoreCount = 5;
                Semaphore semaphore = new Semaphore(semaphoreCount, semaphoreCount, AppDomain.CurrentDomain.FriendlyName);
                var projAppName = AppDomain.CurrentDomain.FriendlyName;
                AppDomain.CurrentDomain.ProcessExit += new EventHandler(OnAppExit);

                if (semaphore.WaitOne(TimeSpan.Zero, true))
                {
                    if (args.Length > 0)
                    {
                        var fileName = args[0];
                        QVMEditorForm.openFileName = fileName;
                    }

                    Application.EnableVisualStyles();
                    Application.SetCompatibleTextRenderingDefault(false);
                    Application.Run(new QVMEditorForm());
                    semaphore.Release();
                }
                else
                {
                    QUtils.ShowError($"Only {semaphoreCount} instances of QVM Editor are allowed to run at the same time.");
                }
            }
            catch (Exception exception)
            {
                QUtils.ShowLogException("QVMEditor Main Exception: ", exception);
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