using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace QVM_Editor
{
    internal class QCompiler
    {
        private enum QTYPE { COMPILE, DECOMPILE }

        private static readonly string appPath = Directory.GetCurrentDirectory();
        private const string compileStartv5 = "compile_v5.bat";
        private const string compileStartv7 = "compile_v7.bat";
        private const string decompileStart = "decompile.bat";
        internal static readonly string compilePath = Path.Combine(QUtils.qvmEditorQEdPath, QUtils.qCompiler, "Compile");
        internal static readonly string decompilePath = Path.Combine(QUtils.qvmEditorQEdPath, QUtils.qCompiler, "Decompile");

        private static string GetInputPath(QTYPE type) => Path.Combine(type == QTYPE.COMPILE ? compilePath : decompilePath, "input");

        private static void QSetPath(string path) => Directory.SetCurrentDirectory(path);

        private static bool QCopy(List<string> files, QTYPE type)
        {
            string copyPath = GetInputPath(type);
            foreach (var file in files)
            {
                if (!File.Exists(file) || !QUtils.ShellExec($"copy \"{file}\" \"{copyPath}\"").Contains("1 file(s) copied"))
                    return false;
            }
            return true;
        }

        private static bool XMove(string src, string dest, QTYPE type)
        {
            if (type == QTYPE.COMPILE)
            {
                src = src.Replace(QUtils.qscFile, QUtils.qvmFile);
                dest = dest.Replace(QUtils.qscFile, QUtils.qvmFile);
            }
            else
            {
                src = src.Replace(QUtils.qvmFile, QUtils.qscFile);
                dest = dest.Replace(QUtils.qvmFile, QUtils.qscFile);
            }

            return QUtils.ShellExec($"move /y \"{src}\" \"{dest}\"").Contains("1 file(s) moved");
        }

        public bool QCompile(List<string> qscFiles, string outputPath)
        {
            try
            {
                if (!QCopy(qscFiles, QTYPE.COMPILE))
                {
                    QUtils.ShowError("QCompiler: Error occurred while copying files");
                    return false;
                }

                QSetPath(compilePath);

                // Checking the QVM version before saving.
                string compileStart = "";
                if (QUtils.qvmVersion == nameof(QUtils.QVMVersion.v0_85))
                    compileStart = compileStartv5;
                else if (QUtils.qvmVersion == nameof(QUtils.QVMVersion.v0_87))
                    compileStart = compileStartv7;

                string shellOut = QUtils.ShellExec(compileStart);
                if (shellOut.Contains("Error") || shellOut.Contains("importModule") || shellOut.Contains("ModuleNotFoundError") || shellOut.Contains("Converted: 0"))
                {
                    QUtils.ShowError("QCompiler: Error in compiling input files");
                    return false;
                }

                string srcPath = Path.Combine(Directory.GetCurrentDirectory(), "output", qscFiles[0]);
                string destPath = Path.Combine(outputPath, qscFiles[0]);

                if (!XMove(srcPath, destPath, QTYPE.COMPILE))
                {
                    QUtils.ShowError("QCompiler: Error while moving data to Output path");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                QUtils.ShowLogException(MethodBase.GetCurrentMethod().Name, ex);
                return false;
            }
            finally
            {
                Directory.SetCurrentDirectory(appPath);
            }
        }

        public bool QDecompile(List<string> qvmFiles, string outputPath)
        {
            try
            {
                if (!QCopy(qvmFiles, QTYPE.DECOMPILE))
                {
                    QUtils.ShowError("Error occurred while copying files");
                    return false;
                }

                QSetPath(decompilePath);

                string shellOut = QUtils.ShellExec(decompileStart);
                if (shellOut.Contains("Error") || shellOut.Contains("importModule") || shellOut.Contains("ModuleNotFoundError") || shellOut.Contains("Converted: 0"))
                {
                    QUtils.ShowError("QCompiler: Error in decompiling input files");
                    return false;
                }

                string srcPath = Path.Combine(Directory.GetCurrentDirectory(), "output", qvmFiles[0]);
                string destPath = Path.Combine(outputPath, qvmFiles[0]);

                if (!XMove(srcPath, destPath, QTYPE.DECOMPILE))
                {
                    QUtils.ShowError("QCompiler: Error while moving data to Output path");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                QUtils.ShowLogException(MethodBase.GetCurrentMethod().Name, ex);
                return false;
            }
            finally
            {
                Directory.SetCurrentDirectory(appPath);
            }
        }

        internal static bool CompileFile(string qscFile, string qscPath)
        {
            return !string.IsNullOrEmpty(qscFile) && new QCompiler().QCompile(new List<string> { qscFile }, qscPath);
        }

        internal static bool DecompileFile(string qvmFile, string qvmPath)
        {
            return !string.IsNullOrEmpty(qvmFile) && new QCompiler().QDecompile(new List<string> { qvmFile }, qvmPath);
        }

        internal static bool CompileData(string qscData, string gamePath, bool appendData = false, string qscFile = "objects.qsc", bool restartLevel = false, bool savePos = true)
        {
            if (string.IsNullOrEmpty(qscData)) return false;
            QUtils.SaveFile(qscData, appendData, qscFile);
            return new QCompiler().QCompile(new List<string> { qscFile }, gamePath);
        }

        internal static bool DecompileData(string qvmFile, string gamePath, bool appendData = false, bool restartLevel = false, bool savePos = true)
        {
            if (string.IsNullOrEmpty(qvmFile)) return false;
            QUtils.SaveFile(qvmFile, appendData);
            return new QCompiler().QDecompile(new List<string> { QUtils.objectsQvm }, gamePath);
        }

        // These methods are kept for backward compatibility
        internal static bool Compile(string qscFile, string gamePath, int _ignore) => CompileFile(qscFile, gamePath);
        internal static bool Compile(string qscData, string gamePath, bool appendData = false, string qscFile = "objects.qsc", bool restartLevel = false, bool savePos = true)
            => CompileData(qscData, gamePath, appendData, qscFile, restartLevel, savePos);
        internal static bool Decompile(string qscFile, string gamePath, int _ignore) => DecompileFile(qscFile, gamePath);
        internal static bool Decompile(string qscData, string gamePath, bool appendData = false, bool restartLevel = false, bool savePos = true)
            => DecompileData(qscData, gamePath, appendData, restartLevel, savePos);
    }
}