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
                if (!File.Exists(file))
                {
                    QUtils.AddLog($"QCopy: File does not exist: {file}");
                    return false;
                }

                try
                {
                    // Use .NET method to copy files, this does not rely on system language
                    File.Copy(file, Path.Combine(copyPath, Path.GetFileName(file)), true);
                    QUtils.AddLog($"QCopy: Successfully copied file: {file} to {copyPath}");
                }
                catch (UnauthorizedAccessException ex)
                {
                    QUtils.AddLog($"QCopy: UnauthorizedAccessException while copying file: {file} to {copyPath}. Exception: {ex.Message}");
                    return false;
                }
                catch (IOException ex)
                {
                    QUtils.AddLog($"QCopy: IOException while copying file: {file} to {copyPath}. Exception: {ex.Message}");
                    return false;
                }
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
            else if (type == QTYPE.DECOMPILE)
            {
                src = src.Replace(QUtils.qvmFile, QUtils.qscFile);
                dest = dest.Replace(QUtils.qvmFile, QUtils.qscFile);
            }

            QUtils.AddLog($"XMove: Attempting to move from {src} to {dest}");

            // Remove quotes from source and destination paths
            if( src.StartsWith("\"") && src.EndsWith("\"")){
                src = src.Replace("\"", "");
                dest = dest.Replace("\"", "");
            }
            
            try
            {
                // Attempt to delete the destination file if it exists
                QUtils.FileIODelete(dest);

                QUtils.FileMove(src, dest);
                QUtils.AddLog($"XMove: File moved from '{src}' to '{dest}'");

                // Post-move verification
                if (File.Exists(dest) && !File.Exists(src))
                {
                    QUtils.AddLog($"XMove: Move verified successfully from {src} to {dest}");
                }
                else
                {
                    QUtils.AddLog($"XMove: Move verification failed from {src} to {dest}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                QUtils.AddLog($"XMove: Error during file move from {src} to {dest}. Exception: {ex.Message}");
                return false;
            }
            return true;
        }


        public bool QCompile(List<string> qscFiles, string outputPath)
        {
            try
            {
                if (!QCopy(qscFiles, QTYPE.COMPILE))
                {
                    foreach (var file in qscFiles)
                    {
                        QUtils.AddLog($"QDecompile: File in qscFiles list: {file}");
                    }
                    QUtils.ShowError("Error occurred while copying files");
                    QUtils.AddLog($"QDecompile: Error occurred while copying files {qscFiles} and path is {compilePath}");
                    return false;
                }

                string currentPath = Directory.GetCurrentDirectory();
                QUtils.logFilePath = Path.Combine(currentPath, QUtils.logFile);
                QUtils.AddLog("QCompile: Log file path is " + QUtils.logFilePath);

                QUtils.AddLog("QCompile: setting path to " + compilePath);
                QSetPath(compilePath);

                // Checking the QVM version before saving.
                string compileStart = "";
                if (QUtils.qvmVersion == nameof(QUtils.QVMVersion.v0_85))
                    compileStart = compileStartv5;
                else if (QUtils.qvmVersion == nameof(QUtils.QVMVersion.v0_87))
                    compileStart = compileStartv7;

                QUtils.AddLog(logMsg: "Compile command file: " + compileStart, logPath: QUtils.logFilePath);
                string shellOut = QUtils.ShellExec(compileStart);
                QUtils.AddLog(logMsg: "Compile output: " + shellOut, logPath: QUtils.logFilePath);
                if (shellOut.Contains("Error") || shellOut.Contains("importModule") || shellOut.Contains("ModuleNotFoundError") || shellOut.Contains("Converted: 0"))
                {
                    QUtils.ShowError("QCompiler: Error in compiling input files");
                    return false;
                }

                string srcPath = Path.Combine(Directory.GetCurrentDirectory(), "output", qscFiles[0]);
                string destPath = Path.Combine(outputPath, qscFiles[0]);

                // Ensure paths are properly quoted
                srcPath = $"\"{srcPath}\"";
                destPath = $"\"{destPath}\"";

                QUtils.AddLog(logMsg: "QCompile: Source path is " + srcPath, logPath: QUtils.logFilePath);
                QUtils.AddLog(logMsg: "QCompile: Destination path is " + destPath, logPath: QUtils.logFilePath);

                bool moveStatus = XMove(srcPath, destPath, QTYPE.COMPILE);
                QUtils.AddLog(logMsg: "QCompile: Move status is " + moveStatus, logPath: QUtils.logFilePath);

                if (!moveStatus)
                {
                    QUtils.ShowError("QCompiler: Error while moving data to Output path");
                    QUtils.AddLog(logMsg: "Compile: Error while moving data to Output path", logPath: QUtils.logFilePath);
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
                    foreach (var file in qvmFiles)
                    {
                        QUtils.AddLog($"QDecompile: File in qvmFiles list: {file}");
                    }
                    QUtils.ShowError("Error occurred while copying files");
                    QUtils.AddLog($"QDecompile: Error occurred while copying files {qvmFiles} and path is {decompilePath}");
                    return false;
                }

                string currentPath = Directory.GetCurrentDirectory();
                QUtils.logFilePath = Path.Combine(currentPath, QUtils.logFile);
                QUtils.AddLog("QDecompile: Log file path is " + QUtils.logFilePath);

                QUtils.AddLog("QDecompile: setting path to " + decompilePath);
                QSetPath(decompilePath);

                string shellOut = QUtils.ShellExec(decompileStart);
                QUtils.AddLog(logMsg: "Decompile output: " + shellOut, logPath: QUtils.logFilePath);
                if (shellOut.Contains("Error") || shellOut.Contains("importModule") || shellOut.Contains("ModuleNotFoundError") || shellOut.Contains("Converted: 0"))
                {
                    QUtils.ShowError("QCompiler: Error in decompiling input files");
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