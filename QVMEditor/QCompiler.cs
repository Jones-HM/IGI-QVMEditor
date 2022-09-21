using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace QVM_Editor
{
    internal class QCompiler
    {
        enum QTYPE
        {
            COMPILE,
            DECOMPILE,
        };
        private static string qpath;

        private string compileStart = "compile.bat";
        private string decompileStart = "decompile.bat";
        internal static string compilePath = QUtils.qvmEditorQEdPath + "\\" + QUtils.qCompiler + @"\Compile";
        private string compileInputPath = QUtils.qvmEditorQEdPath + "\\" + QUtils.qCompiler + @"\Compile\input";
        internal static string decompilePath = QUtils.qvmEditorQEdPath + "\\" + QUtils.qCompiler + @"\Decompile";
        private string decompileInputPath = QUtils.qvmEditorQEdPath + "\\" + QUtils.qCompiler + @"\Decompile\input";
        private string copyNoneErr = "0 File(s) copied";
        private string moveNoneErr = "0 File(s) moved";
        private static string qappPath;

        internal QCompiler()
        {
            qappPath = Directory.GetCurrentDirectory();
            qpath = QUtils.appdataPath;
        }

        private static QCompiler GetQCompiler()
        {
            bool compilerExist = false;
            QUtils.appdataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            QUtils.qvmEditorQEdPath = QUtils.appdataPath + Path.DirectorySeparatorChar + QUtils.qEditor;
            compilerExist = CheckQCompilerExist();
            return compilerExist ? new QCompiler() : null;
        }

        internal static bool CheckQCompilerExist()
        {
            var qCompilerPath = QUtils.qvmEditorQEdPath + "\\" + QUtils.qCompiler;
            bool exist = Directory.Exists(qCompilerPath);
            if (!exist)
            {
                QUtils.ShowError("QCompiler tool not found in system", "Compiler Error.");
                return false;
            }
            return true;
        }


        private void QSetPath(string path)
        {
            //path = qpath + path;
            Directory.SetCurrentDirectory(path);
        }

        private string QGetAbsPath(string dirName)
        {
            return qpath + dirName;
        }

        private bool QCopy(List<string> files, QTYPE type)
        {
            bool status = true;
            string copyPath = (type == (int)QTYPE.COMPILE) ? (compileInputPath) : (decompileInputPath);
            foreach (var file in files)
            {
                string copyFile = "copy \"" + file + "\" \"" + copyPath + "\"";
                var shellOut = QUtils.ShellExec(copyFile);

                //Check for error in copy.
                if (shellOut.Contains(copyNoneErr))
                {
                    status = false;
                    break;
                }
            }
            return status;
        }

        private bool XCopy(string src, string dest)
        {
            bool status = true;
            string xcopyCmd = "xcopy " + src + dest + " /s /e /h /D";

            var shellOut = QUtils.ShellExec(xcopyCmd);

            //Check for error in copy.
            if (shellOut.Contains(copyNoneErr))
                status = false;
            return status;
        }

        private bool XMove(string src, string dest, QTYPE qtype)
        {
            bool status = true;
            string filter = "*.";
            
            if (qtype == QTYPE.COMPILE)
            {
                src = src.Replace(QUtils.qscFile, QUtils.qvmFile);
                dest = dest.Replace(QUtils.qscFile, QUtils.qvmFile);
            }

            else if (qtype == QTYPE.DECOMPILE)
            {
                src = src.Replace(QUtils.qvmFile, QUtils.qscFile);
                dest = dest.Replace(QUtils.qvmFile, QUtils.qscFile);
            }

            //string xmoveCmd = "for /r \"" + src + "\" %x in (" + filter + ") do move /y \"%x\" \"" + dest + "\"";
            string xmoveCmd = "move /y \"" + src + "\" \"" + dest + "\"";
            var shellOut = QUtils.ShellExec(xmoveCmd);

            //Check for error in move.
            if (shellOut.Contains(moveNoneErr))
                status = false;
            return status;
        }

        //Compiler  - QConv Tools.
        public bool QCompile(List<string> qscFiles, string outputPath)
        {
            bool status = true;
            try
            {
                status = QCopy(qscFiles, QTYPE.COMPILE);

                if (!status)
                    QUtils.ShowError("Error occurred while copying files");

                //Change directory to compile directory.
                QSetPath(compilePath);

                //Start compile command.
                string shellOut = QUtils.ShellExec(compileStart);
                if (shellOut.Contains("Error") || shellOut.Contains("importModule") || shellOut.Contains("ModuleNotFoundError") || shellOut.Contains("Converted: 0"))
                {
                    QUtils.ShowError("QCompiler: Error in compiling input files");
                    return false;
                }

                var currDir = Directory.GetCurrentDirectory();
                if (Directory.Exists(currDir))
                {
                    bool moveStatus = XMove("output" + "\\" + qscFiles[0], outputPath + "\\" + qscFiles[0], QTYPE.COMPILE);
                    if (!moveStatus)
                        QUtils.ShowError("Error while moving data to Output path");
                }
                else
                {
                    QUtils.ShowError("Path '" + currDir + "' does not exist!");
                }

            }
            catch (Exception ex)
            {
                QUtils.ShowLogException(MethodBase.GetCurrentMethod().Name, ex);
            }
            Directory.SetCurrentDirectory(qappPath);
            return status;
        }

        internal static bool CompileFile(string qscFile, string qscPath)
        {
            bool status = false;
            try
            {
                if (!String.IsNullOrEmpty(qscFile))
                {
                    var qcompiler = GetQCompiler();
                    status = qcompiler.QCompile(new List<string>() { qscFile }, qscPath);
                }
            }
            catch (Exception ex)
            {
                QUtils.LogException(MethodBase.GetCurrentMethod().Name, ex);
            }
            return status;
        }

        internal static bool CompileData(string qscData, string gamePath, bool appendData = false, string qscFile = "objects.qsc", bool restartLevel = false, bool savePos = true)
        {
            bool status = false;
            try
            {
                if (!String.IsNullOrEmpty(qscData))
                {
                    QUtils.SaveFile(qscData, appendData, qscFile);
                    var qcompiler = GetQCompiler();
                    status = qcompiler.QCompile(new List<string>() { qscFile }, gamePath);
                }
            }

            catch (Exception ex)
            {
                QUtils.LogException(MethodBase.GetCurrentMethod().Name, ex);
            }
            return status;
        }

        //Decompiler  - QConv Tools.
        internal static bool DecompileFile(string qvmFile, string qvmPath)
        {
            bool status = false;
            try
            {
                if (!String.IsNullOrEmpty(qvmFile))
                {
                    var qcompiler = GetQCompiler();
                    status = qcompiler.QDecompile(new List<string>() { qvmFile }, qvmPath);
                }
            }
            catch (Exception ex)
            {
                QUtils.LogException(MethodBase.GetCurrentMethod().Name, ex);
            }
            return status;
        }

        internal static bool DecompileData(string qvmFile, string gamePath, bool appendData = false, bool restartLevel = false, bool savePos = true)
        {
            bool status = false;
            try
            {
                if (!String.IsNullOrEmpty(qvmFile))
                {
                    QUtils.SaveFile(qvmFile, appendData);
                    var qcompiler = GetQCompiler();
                    status = qcompiler.QDecompile(new List<string>() { QUtils.objectsQvm }, gamePath);
                }
            }

            catch (Exception ex)
            {
                QUtils.LogException(MethodBase.GetCurrentMethod().Name, ex);
            }
            return status;
        }

        public bool QDecompile(List<string> qvmFiles, string outputPath)
        {
            bool status = true;
            try
            {
                status = QCopy(qvmFiles, QTYPE.DECOMPILE);

                if (!status)
                    QUtils.ShowError("Error occurred while copying files");

                //Change directory to compile directory.
                QSetPath(decompilePath);

                //Start compile command.
                string shellOut = QUtils.ShellExec(decompileStart);
                if (shellOut.Contains("Error") || shellOut.Contains("importModule") || shellOut.Contains("ModuleNotFoundError") || shellOut.Contains("Converted: 0"))
                {
                    QUtils.ShowError("Error in decompiling input files");
                    return false;
                }

                var currDir = Directory.GetCurrentDirectory();
                if (Directory.Exists(currDir))
                {
                    bool moveStatus = XMove("output", outputPath, QTYPE.DECOMPILE);
                    if (!moveStatus)
                        QUtils.ShowError("Error while moving data to Output path");
                }
                else
                {
                    QUtils.ShowError("Path '" + currDir + "' does not exist!");
                }

            }
            catch (Exception ex)
            {
                QUtils.ShowLogException(MethodBase.GetCurrentMethod().Name, ex);
            }
            Directory.SetCurrentDirectory(qappPath);
            return status;
        }

        internal static bool Compile(string qscFile, string gamePath, int _ignore)
        {
            bool status = CompileFile(qscFile, gamePath);
            return status;
        }

        internal static bool Compile(string qscData, string gamePath, bool appendData = false, string qscFile = "objects.qsc", bool restartLevel = false, bool savePos = true)
        {
            bool status = CompileData(qscData, gamePath, appendData, qscFile, restartLevel, savePos);
            return status;
        }

        internal static bool Decompile(string qscFile, string gamePath, int _ignore)
        {
            bool status = DecompileFile(qscFile, gamePath);
            return status;
        }

        internal static bool Decompile(string qscData, string gamePath, bool appendData = false, bool restartLevel = false, bool savePos = true)
        {
            bool status = DecompileData(qscData, gamePath, appendData, restartLevel, savePos);
            return status;
        }
    }
}
