using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using FileIO = Microsoft.VisualBasic.FileIO;
using FileSystem = Microsoft.VisualBasic.FileIO.FileSystem;
using System.Collections.Generic;
using System.Drawing;

namespace QVM_Editor
{
    internal class QUtils
    {
        private static string logFile;
        internal const string appVersion = "0.2",qvmFile = ".qvm",qscFile = ".qsc", CAPTION_CONFIG_ERR = "Config - Error", CAPTION_FATAL_SYS_ERR = "Fatal sytem - Error", CAPTION_APP_ERR = "Application - Error", CAPTION_COMPILER_ERR = "Compiler - Error", EDITOR_LEVEL_ERR = "EDITOR ERROR";
        internal static bool logEnabled = false;
        internal static string appdataPath, qvmEditorQEdPath, objectsModelsFile, editorAppName, qfilesPath = @"\QFiles", qEditor = "QEditor", qconv = "QConv", qCompiler = "QCompiler", qCompilerPath, tempPathFile,tempPathFileName = "TempPath.txt",
         igiQsc = "IGI_QSC", igiQvm = "IGI_QVM", cfgGamePathEx = @"\missions\location0\level", weaponsDirPath = @"\weapons";
        internal static string keywordsFile = "keywords.txt", objectsQsc = "objects.qsc", objectsQvm = "objects.qvm";

        public static string appOutPath { get; internal set; }
        internal static Dictionary<string, string> gameObjectsInfo = new Dictionary<string, string>();
        internal static string keywords;
        internal static string masterobjList;

        internal class FOpenIO
        {
            string fileName;
            string fileData;
            long fileLength;
            float fileSize;
            public FOpenIO() { FileName = FileData = null; FileLength = 0; FileSize = 0; }

            public FOpenIO(string fileName, string fileData, long fileLength, float fileSize)
            {
                this.FileName = fileName;
                this.FileData = fileData;
                this.FileLength = fileLength;
                this.FileSize = fileSize;
            }

            public string FileName { get => fileName; set => fileName = value; }
            public string FileData { get => fileData; set => fileData = value; }
            public long FileLength { get => fileLength; set => fileLength = value; }
            public float FileSize { get => fileSize; set => fileSize = value; }
        }

        private static void MoveQEditorDir(string destPath)
        {
            var currentDirectory = Directory.GetCurrentDirectory();
            var qEdCurrPath = currentDirectory + "\\" + "QEditor";
            var qCompilerCurrPath = qEdCurrPath + "\\" + "QCompiler";
            var keywordFilePath = qEdCurrPath + "\\" + keywordsFile;

            if (Directory.Exists(qEdCurrPath) && Directory.Exists(qCompilerCurrPath) && !Directory.Exists(qvmEditorQEdPath))
            {
                DirectoryMove(qEdCurrPath, destPath + "\\" + "QEditor");
            }
            if (File.Exists(keywordFilePath))
            {
                QUtils.FileIOMove(keywordFilePath, qvmEditorQEdPath + "\\" + keywordsFile);
            }
        }

        private static void InitGameObjectsInfo()
        {
            gameObjectsInfo.Add("Building", "This is used to add building to level." + "\n" + "Usage: Task_New(Task Id,Building,Position X,Y,Z,Orientation Alpha,Beta,Gamma,Model Id);");
            gameObjectsInfo.Add("EditRigidObj", "This is used to add 3D rigid object to level." + "\n" + "Usage: Task_New(Task Id,EditRigidObj,Position X,Y,Z,Orientation Alpha,Beta,Gamma,Model Id,Angular Effect (RGB),Ambient Effect (RGB));");
            gameObjectsInfo.Add("SplineObjWaypoint", "These are 3D spline objects like Tunnel/Roads.");
            gameObjectsInfo.Add("SplineObj", "3D spline objects like Tunnel/Roads.");
            gameObjectsInfo.Add("Static", "These are static objects which do not render/change their properties during runtime.");
            gameObjectsInfo.Add("Dynamic", "These are Dynamic objects which render/change their properties during runtime.");
            gameObjectsInfo.Add("HeightMap", "Light map or height map defines properties of Terrain");
            gameObjectsInfo.Add("TerrainLightMap", "Light map or height map defines properties of Terrain");
            gameObjectsInfo.Add("LightmapInfo", "Light map defines properties of 3D objects.");
            gameObjectsInfo.Add("Container", "Container contains multiple QTasks like Army of soldiers or Group of Trees for Decorations.");
            gameObjectsInfo.Add("SCamera", "Security Control Camera.");
            gameObjectsInfo.Add("EditVariable", "This is used to create variable in script.");
            gameObjectsInfo.Add("AreaActivate", "This is used to Activate certain areas like for marking Level area.");
            gameObjectsInfo.Add("ExplodeObject", "This is used to created animation for exploded objects like PC/Terminal.");
            gameObjectsInfo.Add("StatusMessage", "This is used to show status messages on screen for information.");
            gameObjectsInfo.Add("ConditionalContainer", "This is used to create containers on condition and to enable/disable some army at some conditions.");
            gameObjectsInfo.Add("AnimTask", "This is used to create Animations for tasks.");
            gameObjectsInfo.Add("HumanSoldier", "This is used to add new Soldier to level." + "\n" + "Usage: Task_New(Task Id,HumanSoldier,Position X,Y,Z,Gamma, Model Id,Team Id,Bone Hierarchy,Stand Animation);");
            gameObjectsInfo.Add("HumanAI", "This is used to add new AI to level" + "\n" + "Usage: Task_New(Task Id,HumanAI,AI Type, Graph ID)");
            gameObjectsInfo.Add("HumanPlayer", "This is used to add new HumanPlayer to level" + "\n" + "Usage: Task_New(Task Id,HumanPlayer,Position X,Y,Z,Gamma,Model Id,Team Id)");
            gameObjectsInfo.Add("HumanPlayerInput", "This is used to add new inputs for HumanPlayer");
            gameObjectsInfo.Add("AIGraph", "This is used to define properties for level graph.");
            gameObjectsInfo.Add("PatrolPath", "This is used to add define path for AI Soldiers/Cars.");
            gameObjectsInfo.Add("PatrolPathCommand", "This is used to add define path commands for AI Soldiers/Cars.");
            gameObjectsInfo.Add("AmbientArea", "This is used to define an Ambient area like building where outside noise is not audible");
            gameObjectsInfo.Add("ConditionalSound", "This is used to define sound on some condition");
            gameObjectsInfo.Add("LODSettings", "Level Of Details Settings");
            gameObjectsInfo.Add("TextureModifier", "This is used to modify textures like Grass/Snow etc for level.");
            gameObjectsInfo.Add("LevelFlow", "This is the timespan flow of current level playing.");
            gameObjectsInfo.Add("Dirlight", "This is the direct sunlight.");
            gameObjectsInfo.Add("DirlightKeyframe", "This is the direct sunlight.");
            gameObjectsInfo.Add("FlatSky", "This is the floating sky.");
            gameObjectsInfo.Add("Task_New", "This is used to create new tasks in level for all game objects like 3D models/Building/AI/Animations." + "\n" + "Syntax : Task_New(Task Id,Task Type,Task Note,..... this is general syntax.");
            gameObjectsInfo.Add("Task_DeclareParameters", "This is used to define task information about their parameters and properties.");
            gameObjectsInfo.Add("CutScene", "This is used to create cutscene in the level.");
            gameObjectsInfo.Add("DefineSound", "This is used to define sound in the level.");
            gameObjectsInfo.Add("CreateTerrainMaterial", "This is used to create new terrain material in the level.");
            gameObjectsInfo.Add("DefineMagicObj", "This is used to define magic objects." + "\n" + "Magic objects are those models that are not static and needed to define magic vertex for it to attach to objects" + "\n" + "Example : Doors/Elevators/Gun Flame/Glass/Ladders have animations and are magic objects");
        }


        internal static bool InitEditorAppData()
        {
            bool initStatus = true;
            string initErrReason = String.Empty;
            editorAppName = AppDomain.CurrentDomain.FriendlyName.Replace(".exe", String.Empty);
            logFile = editorAppName + ".log";

            //Init Appdata paths.
            appdataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            qvmEditorQEdPath = appdataPath + Path.DirectorySeparatorChar + qEditor;
            qCompilerPath = qvmEditorQEdPath + @"\QCompiler";
            objectsModelsFile = qvmEditorQEdPath + Path.DirectorySeparatorChar + "IGIModels.txt";
            masterobjList = LoadFile(QUtils.objectsModelsFile);
            tempPathFile = qvmEditorQEdPath + "\\" + tempPathFileName;

            if (!Directory.Exists(qvmEditorQEdPath)) { initErrReason = "QEditor"; initStatus = false; }
            else if (!Directory.Exists(qCompilerPath)) { initErrReason = @"QEditor\QCompiler"; initStatus = false; }

            initErrReason = "'" + initErrReason + "' Directory is missing";
            
            //Show error if 'QEditor' path has invalid structure..
            if (!initStatus)
            {
                ShowSystemFatalError("QVVM Editor Appdata directory is invalid Error: (0x0000000F)\nReason: " + initErrReason + "\nPlease re-install new copy from Setup file.");
            }

            MoveQEditorDir(appdataPath);
            //Init Game objects and Keywords.
            InitGameObjectsInfo();
            InitKeywords();
            return initStatus;
        }

        private static void InitKeywords()
        {
            var keywordsList = File.ReadAllLines(qvmEditorQEdPath + "\\" + keywordsFile);
            keywords = String.Join(" ", keywordsList);
        }

        public static DialogResult ShowDialog(string infoMsg, string caption = "INFO")
        {
            return MessageBox.Show(infoMsg, caption, MessageBoxButtons.YesNo, MessageBoxIcon.Information);
        }

        internal static void ShowWarning(string warnMsg, string caption = "WARNING")
        {
            MessageBox.Show(warnMsg, caption, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        internal static void ShowError(string errMsg, string caption = "ERROR")
        {
            MessageBox.Show(errMsg, caption, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        internal static void ShowInfo(string infoMsg, string caption = "INFO")
        {
            MessageBox.Show(infoMsg, caption, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        internal static void ShowSystemFatalError(string errMsg)
        {
            ShowError(errMsg, CAPTION_FATAL_SYS_ERR);
            Environment.Exit(1);
        }

        private static void ShowConfigError(string keyword)
        {
            ShowError("Config file has invalid property for '" + keyword + "'", CAPTION_CONFIG_ERR);
            Environment.Exit(1);
        }


        internal static void LogException(string methodName, Exception ex)
        {
            methodName = methodName.Replace("_Click", String.Empty).Replace("_SelectedIndexChanged", String.Empty).Replace("_SelectedValueChanged", String.Empty);
            AddLog(methodName, "Exception MESSAGE: " + ex.Message + "\nREASON: " + ex.StackTrace);
        }

        internal static void ShowException(string methodName, Exception ex)
        {
            ShowError("MESSAGE: " + ex.Message + "\nREASON: " + ex.StackTrace, methodName + " Exception");
        }

        internal static void ShowLogException(string methodName, Exception ex)
        {
            methodName = methodName.Replace("_Click", String.Empty).Replace("_SelectedIndexChanged", String.Empty).Replace("_SelectedValueChanged", String.Empty);
            //Show and Log exception for method name.
            ShowException(methodName, ex);
            LogException(methodName, ex);
        }

        internal static void ShowLogError(string methodName, string errMsg, string caption = "ERROR")
        {
            methodName = methodName.Replace("_Click", String.Empty).Replace("_SelectedIndexChanged", String.Empty).Replace("_SelectedValueChanged", String.Empty);
            //Show and Log error for method name.
            ShowError(methodName + "(): " + errMsg, caption);
            AddLog(methodName, errMsg);
        }

        internal static void ShowLogInfo(string methodName, string logMsg)
        {
            ShowInfo(logMsg);
            AddLog(methodName, logMsg);
        }

        internal static void AddLog(string methodName, string logMsg)
        {
            if (logEnabled)
            {
                methodName = methodName.Replace("_Click", String.Empty).Replace("_SelectedIndexChanged", String.Empty).Replace("_SelectedValueChanged", String.Empty);
                File.AppendAllText(logFile, "[" + DateTime.Now.ToString("yyyy-MM-dd - HH:mm:ss") + "] " + methodName + "(): " + logMsg + "\n");
            }
        }

        internal static void SaveFile(string data = null, bool appendData = false, string qscFile = "objects.qsc")
        {
            SaveFile(qscFile, data, appendData);
        }

        internal static void SaveFile(string fileName, string data, bool appendData = false)
        {
            if (appendData)
                File.AppendAllText(fileName, data);
            else
                File.WriteAllText(fileName, data);
        }


        internal static DialogResult ShowInputDialog(ref string input)
        {
            Size size = new Size(200, 70);
            Form inputBox = new Form();

            inputBox.FormBorderStyle = FormBorderStyle.FixedDialog;
            inputBox.ClientSize = size;
            inputBox.Text = "Model Input Dialog";

            TextBox textBox = new TextBox();
            textBox.Size = new Size(size.Width - 10, 23);
            textBox.Location = new Point(5, 5);
            textBox.Text = input;
            inputBox.Controls.Add(textBox);

            Button okButton = new Button();
            okButton.DialogResult = DialogResult.OK;
            okButton.Name = "okButton";
            okButton.Size = new Size(75, 23);
            okButton.Text = "&OK";
            okButton.Location = new Point(size.Width - 80 - 80, 39);
            inputBox.Controls.Add(okButton);

            Button cancelButton = new Button();
            cancelButton.DialogResult = DialogResult.Cancel;
            cancelButton.Name = "cancelButton";
            cancelButton.Size = new Size(75, 23);
            cancelButton.Text = "&Cancel";
            cancelButton.Location = new Point(size.Width - 80, 39);
            inputBox.Controls.Add(cancelButton);

            inputBox.AcceptButton = okButton;
            inputBox.CancelButton = cancelButton;

            DialogResult result = inputBox.ShowDialog();
            input = textBox.Text;
            return result;
        }

        internal static string FindModelName(string modelId, bool addLogs = false)
        {
            string modelName = "UNKNOWN_OBJECT";
            try
            {
                if (modelId.Contains("\""))
                    modelId = modelId.Replace("\"", String.Empty);

                if (File.Exists(QUtils.objectsModelsFile))
                {
                    if (String.IsNullOrEmpty(masterobjList)) return String.Empty;

                    var objList = masterobjList.Split('\n');

                    foreach (var obj in objList)
                    {
                        if (obj.Contains(modelId))
                        {
                            modelName = obj.Split('=')[0];
                            if (modelName.Length < 3 || String.IsNullOrEmpty(modelName))
                            {
                                if (addLogs)
                                    QUtils.AddLog(MethodBase.GetCurrentMethod().Name, "couldn't find model name for Model id : " + modelId);
                                return modelName;
                            }
                        }
                    }

                    if (modelName.Length > 3 && !String.IsNullOrEmpty(modelName) && addLogs)
                        QUtils.AddLog(MethodBase.GetCurrentMethod().Name, "Found model name '" + modelName + "' for id : " + modelId);
                }
            }
            catch (Exception ex)
            {
                QUtils.LogException(MethodBase.GetCurrentMethod().Name, ex);
            }
            return modelName.Trim();
        }

        //Execute shell command and get std-output.
        internal static string ShellExec(string cmdArgs, bool runAsAdmin = false, bool waitForExit = true, string shell = "cmd.exe")
        {
            var process = new Process();
            var startInfo = new ProcessStartInfo();
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;
            startInfo.CreateNoWindow = true;
            startInfo.FileName = shell;
            startInfo.Arguments = "/c " + cmdArgs;
            startInfo.RedirectStandardOutput = !runAsAdmin;
            startInfo.RedirectStandardError = !runAsAdmin;
            startInfo.UseShellExecute = runAsAdmin;
            process.StartInfo = startInfo;
            if (runAsAdmin) process.StartInfo.Verb = "runas";
            process.Start();
            if (!waitForExit) return String.Empty;
            string output = (runAsAdmin) ? String.Empty : process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            return output;
        }

        internal static string LoadFile()
        {
            return LoadFile(objectsQsc);
        }

        internal static string LoadFile(string fileName)
        {
            string data = null;
            if (File.Exists(fileName))
                data = File.ReadAllText(fileName);
            return data;
        }

        internal static FOpenIO ShowOpenFileDlg(string title, string defaultExt, string filter, bool initDir = false, string initialDirectory = "", bool openFileData = true, bool exceptionOnEmpty = true)
        {
            var fopenIO = new FOpenIO();

            try
            {
                var fileBrowser = new OpenFileDialog();
                fileBrowser.ValidateNames = false;
                fileBrowser.CheckFileExists = false;
                fileBrowser.CheckPathExists = true;
                fileBrowser.Title = title;
                fileBrowser.DefaultExt = defaultExt;
                fileBrowser.Filter = filter;
                if (initDir)
                    fileBrowser.InitialDirectory = initialDirectory;

                if (fileBrowser.ShowDialog() == DialogResult.OK)
                {
                    fopenIO.FileName = fileBrowser.FileName;
                    fopenIO.FileLength = new FileInfo(fopenIO.FileName).Length;
                    fopenIO.FileSize = ((float)fopenIO.FileLength / (float)1024);
                    if (openFileData)
                    {
                        fopenIO.FileData = LoadFile(fopenIO.FileName);
                        if (String.IsNullOrEmpty(fopenIO.FileData) && exceptionOnEmpty) throw new FileLoadException("File '" + fopenIO.FileName + "' is invalid or data is empty.");
                    }
                }
            }
            catch (Exception ex)
            {
                ShowLogException(MethodBase.GetCurrentMethod().Name, ex);
            }
            return fopenIO;
        }


        //File Operation Utilities C# Version.
        internal static void FileMove(string srcPath, string destPath)
        {
            try
            {
                if (File.Exists(srcPath)) File.Move(srcPath, destPath);
            }
            catch (Exception ex) { ShowLogException(MethodBase.GetCurrentMethod().Name, ex); }
        }

        internal static void FileCopy(string srcPath, string destPath, bool overwirte = true)
        {
            try
            {
                if (File.Exists(srcPath)) File.Copy(srcPath, destPath, overwirte);
            }
            catch (Exception ex) { ShowLogException(MethodBase.GetCurrentMethod().Name, ex); }
        }

        internal static void FileDelete(string path)
        {
            try
            {
                if (File.Exists(path)) File.Delete(path);
            }
            catch (Exception ex) { ShowLogException(MethodBase.GetCurrentMethod().Name, ex); }
        }

        //File Operation Utilities VB Version.
        internal static void FileIOMove(string srcPath, string destPath, bool overwrite = true)
        {
            try
            {
                if (File.Exists(srcPath)) FileSystem.MoveFile(srcPath, destPath, overwrite);
            }
            catch (Exception ex) { ShowLogException(MethodBase.GetCurrentMethod().Name, ex); }
        }


        internal static void FileIOMove(string srcPath, string destPath, FileIO.UIOption showUI, FileIO.UICancelOption onUserCancel)
        {
            try
            {
                if (File.Exists(srcPath)) FileSystem.MoveFile(srcPath, destPath, showUI, onUserCancel);
            }
            catch (Exception ex) { ShowLogException(MethodBase.GetCurrentMethod().Name, ex); }
        }

        internal static void FileIOCopy(string srcPath, string destPath, bool overwrite = true)
        {
            try
            {
                if (File.Exists(srcPath)) FileSystem.CopyFile(srcPath, destPath, overwrite);
            }
            catch (Exception ex) { ShowLogException(MethodBase.GetCurrentMethod().Name, ex); }
        }

        internal static void FileIOCopy(string srcPath, string destPath, FileIO.UIOption showUI, FileIO.UICancelOption onUserCancel)
        {
            try
            {
                if (File.Exists(srcPath)) FileSystem.CopyFile(srcPath, destPath);
            }
            catch (Exception ex) { ShowLogException(MethodBase.GetCurrentMethod().Name, ex); }
        }

        internal static void FileIODelete(string path, FileIO.UIOption showUI = FileIO.UIOption.OnlyErrorDialogs, FileIO.RecycleOption recycle = FileIO.RecycleOption.SendToRecycleBin, FileIO.UICancelOption onUserCancel = FileIO.UICancelOption.ThrowException)
        {
            try
            {
                if (File.Exists(path)) FileSystem.DeleteFile(path, showUI, recycle, onUserCancel);
            }
            catch (Exception ex) { ShowLogException(MethodBase.GetCurrentMethod().Name, ex); }
        }

        internal static void FileRename(string oldName, string newName)
        {
            try
            {
                if (File.Exists(oldName)) FileSystem.RenameFile(oldName, newName);
            }
            catch (Exception ex) { ShowLogException(MethodBase.GetCurrentMethod().Name, ex); }
        }


        //Directory Operation Utilities C#.
        internal static void DirectoryMove(string srcPath, string destPath)
        {
            try
            {
                if (Directory.Exists(srcPath)) Directory.Move(srcPath, destPath);
            }
            catch (Exception ex) { ShowLogException(MethodBase.GetCurrentMethod().Name, ex); }
        }

        internal static void DirectoryMove(string srcPath, string destPath, int __ignore)
        {
            var mvCmd = "mv " + srcPath + " " + destPath;
            var moveCmd = "move " + srcPath + " " + destPath + " /y";

            try
            {
                //#1 solution to move with same root directory.
                Directory.Move(srcPath, destPath);
            }
            catch (IOException ex)
            {
                if (ex.Message.Contains("already exist"))
                {
                    DirectoryDelete(srcPath);
                }
                else
                {
                    //#2 solution to move with POSIX 'mv' command.
                    ShellExec(mvCmd, true, true, "powershell.exe");
                    if (Directory.Exists(srcPath))
                        //#3 solution to move with 'move' command.
                        ShellExec(moveCmd, true);
                }
            }
        }

        internal static void DirectoryDelete(string dirPath)
        {
            try
            {
                if (Directory.Exists(dirPath))
                {
                    DirectoryInfo di = new DirectoryInfo(dirPath);
                    foreach (FileInfo file in di.GetFiles())
                        file.Delete();
                    foreach (DirectoryInfo dir in di.GetDirectories())
                        dir.Delete(true);
                    Directory.Delete(dirPath);
                }
            }
            catch (Exception ex) { ShowLogException(MethodBase.GetCurrentMethod().Name, ex); }
        }

        //Directory Operation Utilities VB.
        internal static void DirectoryIOMove(string srcPath, string destPath, bool overwrite = true)
        {
            try
            {
                if (Directory.Exists(srcPath)) FileSystem.MoveDirectory(srcPath, destPath, overwrite);
            }
            catch (Exception ex) { ShowLogException(MethodBase.GetCurrentMethod().Name, ex); }
        }

        internal static void DirectoryIOMove(string srcPath, string destPath, FileIO.UIOption showUI, FileIO.UICancelOption onUserCancel)
        {
            try
            {
                if (Directory.Exists(srcPath)) FileSystem.MoveDirectory(srcPath, destPath, showUI, onUserCancel);
            }
            catch (Exception ex) { ShowLogException(MethodBase.GetCurrentMethod().Name, ex); }
        }

        internal static void DirectoryIOCopy(string srcPath, string destPath, bool overwirte = true)
        {
            try
            {
                if (Directory.Exists(srcPath)) FileSystem.CopyDirectory(srcPath, destPath, overwirte);
            }
            catch (Exception ex) { ShowLogException(MethodBase.GetCurrentMethod().Name, ex); }
        }


        internal static void DirectoryIOCopy(string srcPath, string destPath, FileIO.UIOption showUI, FileIO.UICancelOption onUserCancel)
        {
            try
            {
                if (Directory.Exists(srcPath)) FileSystem.CopyDirectory(srcPath, destPath, showUI, onUserCancel);
            }
            catch (Exception ex) { ShowLogException(MethodBase.GetCurrentMethod().Name, ex); }
        }

        internal static void DirectoryIODelete(string path, FileIO.DeleteDirectoryOption deleteContents = FileIO.DeleteDirectoryOption.DeleteAllContents)
        {
            try
            {
                if (Directory.Exists(path)) FileSystem.DeleteDirectory(path, deleteContents);
            }
            catch (Exception ex) { ShowLogException(MethodBase.GetCurrentMethod().Name, ex); }
        }

        internal static void DirectoryRename(string oldName, string newName)
        {
            try
            {
                if (File.Exists(oldName)) FileSystem.RenameDirectory(oldName, newName);
            }
            catch (Exception ex) { ShowLogException(MethodBase.GetCurrentMethod().Name, ex); }
        }


        internal static bool IsDirectoryEmpty(string path)
        {
            return !Directory.EnumerateFileSystemEntries(path).Any();
        }

        internal static string Reverse(string str)
        {
            char[] charArray = str.ToCharArray();
            Array.Reverse(charArray);
            return new string(charArray);
        }
    }
}