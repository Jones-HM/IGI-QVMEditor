using System;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using ScintillaNET;
using System.Reflection;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace QVM_Editor
{
    public partial class QVMEditorForm : Form
    {
        private string scriptFilePath = String.Empty;
        private string scriptFilePathAbsolute = String.Empty;
        internal Scintilla scintilla;
        private string fontName = "Consolas";
        private int fontSize = 12;
        internal static string openFileName = String.Empty;
        internal static QVMEditorForm qvmInstance;

        public QVMEditorForm()
        {
            InitializeComponent();
            qvmInstance = this;
            QUtils.InitEditorAppData();
            appVersionTxt.Text = "QVM Version: " + QUtils.qvmVersion;
            QUtils.logEnabled = true;

            QUtils.AddLog("QVM Editor Started.");
            // Init Scintilla component.
            scintilla = new StandardScintilla();
            TextPanel.Controls.Add(scintilla);


            //Loading output path.
            QUtils.appOutPath = QUtils.LoadFile(QUtils.tempPathFile);
            if (!String.IsNullOrEmpty(QUtils.appOutPath))
            {
                QUtils.AddLog("Output path loaded: " + QUtils.appOutPath);
            }

            //Setting output path.
            else
            {
                QUtils.appOutPath = QUtils.qCompilerPath + @"\Decompile\output\";
                QUtils.SaveFile(QUtils.tempPathFile, QUtils.appOutPath);
                QUtils.AddLog("Output path set to default: " + QUtils.appOutPath);
            }

            //Decompile the QVM File for Args (CLI).
            if (!String.IsNullOrEmpty(openFileName))
            {
                QUtils.AddLog("Decompiling QVM File: " + openFileName);
                DecompileQVM(openFileName);
            }
        }

        private void QVMEditorForm_Load(object sender, EventArgs e)
        {
            QUtils.AddLog("Entering method: QVMEditorForm_Load()");
            QUtils.AddLog("QVMEditorForm_Load param sender = " + sender);
            QUtils.AddLog("QVMEditorForm_Load param e = " + e);

            // BASIC CONFIG
            scintilla.Dock = DockStyle.Fill;

            // INITIAL VIEW CONFIG
            scintilla.WrapMode = WrapMode.Word;
            scintilla.IndentationGuides = IndentView.LookBoth;
            scintilla.CharAdded += scriptViewerText_CharAdded;
            scintilla.MouseDoubleClick += scriptViewerText_MouseDoubleClick;

            // styling
            InitTextSelectionColors(false);
            InitSyntaxColoring();

            // number margin
            InitNumberMargin();

            // bookmark margin
            InitBookmarkMargin();

            // code folding margin
            InitCodeFolding();

            // drag drop
            InitDragDropFile();

            // init hotkeys
            InitHotkeys();
            QUtils.AddLog("Exiting method: QVMEditorForm_Load()");
        }


        private void scriptViewerText_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            QUtils.AddLog("Entering method: scriptViewerText_MouseDoubleClick()");
            QUtils.AddLog("scriptViewerText_MouseDoubleClick param sender = " + sender);
            QUtils.AddLog("scriptViewerText_MouseDoubleClick param e = " + e);

            string objectType = ((Scintilla)sender).SelectedText;
            string objectInfo = String.Empty;

            if (!String.IsNullOrEmpty(objectType))
            {
                var modelRegex = @"\d{3}_\d{2}_\d{1}";
                var modelId = Regex.Match(objectType, modelRegex).Value;
                if (!String.IsNullOrEmpty(modelId))
                {
                    string modelName = QUtils.FindModelName(modelId);
                    modelName = modelName.Trim();

                    string modelInput = String.Empty;
                    if (modelName == "UNKNOWN_OBJECT")
                    {
                        var showDialog = QUtils.ShowDialog("An unknown object '" + modelId + "' was encountered do you want to improve it ?");
                        if (showDialog == DialogResult.Yes)
                        {
                            QUtils.ShowInputDialog(ref modelInput);
                            QUtils.AddLog("Exiting method: scriptViewerText_MouseDoubleClick()");
                        }

                        //Save the input model to model data.
                        if (!String.IsNullOrEmpty(modelInput))
                        {
                            modelInput = modelInput.ToUpper();

                            // Read the contents of the file into a string
                            string fileContents = QUtils.LoadFile(QUtils.objectsModelsFile);

                            // Parse the JSON string into a JArray object
                            JArray modelsArray = JArray.Parse(fileContents);

                            // Create a new JObject for the new data
                            JObject newModel = new JObject(
                                new JProperty("ModelName", modelInput),
                                new JProperty("ModelId", modelId)
                            );

                            // Add the new data to the array
                            modelsArray.Add(newModel);

                            // Serialize the updated array back to JSON format
                            string updatedFileContents = JsonConvert.SerializeObject(modelsArray, Formatting.Indented);

                            // Write the updated JSON string to the file
                            QUtils.SaveFile(updatedFileContents, false, QUtils.objectsModelsFile);

                            // Show a message to confirm that the model has been saved
                            QUtils.ShowInfo("Model saved " + modelId + " : " + modelInput);

                            //Reload the Master objects list.
                            QUtils.masterobjList = QUtils.LoadFile(QUtils.objectsModelsFile);
                        }
                    }

                    objectInfo = "Model: '" + modelName + "'";
                }

                else
                {
                    foreach (var objectInfoData in QUtils.gameObjectsInfo)
                    {
                        if (objectType == objectInfoData.Key)
                        {
                            objectInfo = objectInfoData.Value;
                            break;
                        }
                    }
                }

                if (!String.IsNullOrEmpty(objectInfo))
                    scintilla.CallTipShow(((Scintilla)sender).CurrentPosition, objectInfo);
            }
        }

        private void InitTextSelectionColors(bool darkMode)
        {
            QUtils.AddLog("Entering method: InitTextSelectionColors()");
            QUtils.AddLog("InitTextSelectionColors param darkMode = " + darkMode);

            if (darkMode) scintilla.SetSelectionBackColor(true, IntToColor(0x0078D7)); //For Dark Mode.
            else scintilla.SetSelectionBackColor(true, IntToColor(0x97C6EB)); //For Light Mode.
            QUtils.AddLog("Exiting method: InitTextSelectionColors()");
        }

        private void InitHotkeys()
        {
            QUtils.AddLog("Entering method: InitHotkeys()");

            // register the hotkeys with the form
            HotKeyManager.AddHotKey(this, OpenSearch, Keys.F, true);
            HotKeyManager.AddHotKey(this, OpenReplaceDialog, Keys.R, true);
            HotKeyManager.AddHotKey(this, OpenReplaceDialog, Keys.H, true);
            HotKeyManager.AddHotKey(this, ZoomIn, Keys.Oemplus, true);
            HotKeyManager.AddHotKey(this, ZoomOut, Keys.OemMinus, true);
            HotKeyManager.AddHotKey(this, ZoomDefault, Keys.D0, true);
            //HotKeyManager.AddHotKey(this, CloseSearch, Keys.Escape);

            // remove conflicting hotkeys from scintilla
            scintilla.ClearCmdKey(Keys.Control | Keys.F);
            scintilla.ClearCmdKey(Keys.Control | Keys.R);
            scintilla.ClearCmdKey(Keys.Control | Keys.H);
            scintilla.ClearCmdKey(Keys.Control | Keys.L);
            scintilla.ClearCmdKey(Keys.Control | Keys.U);
            QUtils.AddLog("Exiting method: InitHotkeys()");
        }

        private void InitSyntaxColoring()
        {
            QUtils.AddLog("Entering method: InitSyntaxColoring()");

            AutomaticThemeStyle();
            QUtils.AddLog("Exiting method: InitSyntaxColoring()");
        }


        #region Numbers, Bookmarks, Code Folding

        /// <summary>
        /// the background color of the text area
        /// </summary>
        private const int BACK_COLOR = 0xFFFFFF;

        /// <summary>
        /// default text color of the text area
        /// </summary>
        private const int FORE_COLOR = 0x056B31;

        /// <summary>
        /// change this to whatever margin you want the line numbers to show in
        /// </summary>
        private const int NUMBER_MARGIN = 1;

        /// <summary>
        /// change this to whatever margin you want the bookmarks/breakpoints to show in
        /// </summary>
        private const int BOOKMARK_MARGIN = 2;
        private const int BOOKMARK_MARKER = 2;

        /// <summary>
        /// change this to whatever margin you want the code folding tree (+/-) to show in
        /// </summary>
        private const int FOLDING_MARGIN = 3;

        /// <summary>
        /// set this true to show circular buttons for code folding (the [+] and [-] buttons on the margin)
        /// </summary>
        private const bool CODEFOLDING_CIRCULAR = true;

        private void InitNumberMargin()
        {
            QUtils.AddLog("Entering method: InitNumberMargin()");


            scintilla.Styles[Style.LineNumber].BackColor = IntToColor(BACK_COLOR);
            scintilla.Styles[Style.LineNumber].ForeColor = IntToColor(FORE_COLOR);
            scintilla.Styles[Style.IndentGuide].ForeColor = IntToColor(FORE_COLOR);
            scintilla.Styles[Style.IndentGuide].BackColor = IntToColor(BACK_COLOR);

            var nums = scintilla.Margins[NUMBER_MARGIN];
            nums.Width = 30;
            nums.Type = MarginType.Number;
            nums.Sensitive = true;
            nums.Mask = 0;

            scintilla.MarginClick += TextArea_MarginClick;
            QUtils.AddLog("Exiting method: InitNumberMargin()");
        }

        private void InitBookmarkMargin()
        {
            QUtils.AddLog("Entering method: InitBookmarkMargin()");

            var margin = scintilla.Margins[BOOKMARK_MARGIN];
            margin.Width = 20;
            margin.Sensitive = true;
            margin.Type = MarginType.Symbol;
            margin.Mask = (1 << BOOKMARK_MARKER);

            var marker = scintilla.Markers[BOOKMARK_MARKER];
            marker.Symbol = MarkerSymbol.Circle;
            marker.SetBackColor(IntToColor(0xFF003B));
            marker.SetForeColor(IntToColor(0x000000));
            marker.SetAlpha(100);
            QUtils.AddLog("Exiting method: InitBookmarkMargin()");
        }

        private void InitCodeFolding()
        {
            QUtils.AddLog("Entering method: InitCodeFolding()");


            scintilla.SetFoldMarginColor(true, IntToColor(BACK_COLOR));
            scintilla.SetFoldMarginHighlightColor(true, IntToColor(BACK_COLOR));

            // Enable code folding
            scintilla.SetProperty("fold", "1");
            scintilla.SetProperty("fold.compact", "1");

            // Configure a margin to display folding symbols
            scintilla.Margins[FOLDING_MARGIN].Type = MarginType.Symbol;
            scintilla.Margins[FOLDING_MARGIN].Mask = Marker.MaskFolders;
            scintilla.Margins[FOLDING_MARGIN].Sensitive = true;
            scintilla.Margins[FOLDING_MARGIN].Width = 20;

            // Set colors for all folding markers
            for (int i = 25; i <= 31; i++)
            {
                scintilla.Markers[i].SetForeColor(IntToColor(BACK_COLOR)); // styles for [+] and [-]
                scintilla.Markers[i].SetBackColor(IntToColor(FORE_COLOR)); // styles for [+] and [-]
                QUtils.AddLog("Exiting method: InitCodeFolding()");
            }

            // Configure folding markers with respective symbols
            scintilla.Markers[Marker.Folder].Symbol = CODEFOLDING_CIRCULAR ? MarkerSymbol.CirclePlus : MarkerSymbol.BoxPlus;
            scintilla.Markers[Marker.FolderOpen].Symbol = CODEFOLDING_CIRCULAR ? MarkerSymbol.CircleMinus : MarkerSymbol.BoxMinus;
            scintilla.Markers[Marker.FolderEnd].Symbol = CODEFOLDING_CIRCULAR ? MarkerSymbol.CirclePlusConnected : MarkerSymbol.BoxPlusConnected;
            scintilla.Markers[Marker.FolderMidTail].Symbol = MarkerSymbol.TCorner;
            scintilla.Markers[Marker.FolderOpenMid].Symbol = CODEFOLDING_CIRCULAR ? MarkerSymbol.CircleMinusConnected : MarkerSymbol.BoxMinusConnected;
            scintilla.Markers[Marker.FolderSub].Symbol = MarkerSymbol.VLine;
            scintilla.Markers[Marker.FolderTail].Symbol = MarkerSymbol.LCorner;

            // Enable automatic folding
            scintilla.AutomaticFold = (AutomaticFold.Show | AutomaticFold.Click | AutomaticFold.Change);

        }

        private void TextArea_MarginClick(object sender, MarginClickEventArgs e)
        {
            QUtils.AddLog("Entering method: TextArea_MarginClick()");
            QUtils.AddLog("TextArea_MarginClick param sender = " + sender);
            QUtils.AddLog("TextArea_MarginClick param e = " + e);

            if (e.Margin == BOOKMARK_MARGIN)
            {
                // Do we have a marker for this line?
                const uint mask = (1 << BOOKMARK_MARKER);
                var line = scintilla.Lines[scintilla.LineFromPosition(e.Position)];
                if ((line.MarkerGet() & mask) > 0)
                {
                    // Remove existing bookmark
                    line.MarkerDelete(BOOKMARK_MARKER);
                    QUtils.AddLog("Exiting method: TextArea_MarginClick()");
                }
                else
                {
                    // Add bookmark
                    line.MarkerAdd(BOOKMARK_MARKER);
                }
            }
        }

        #endregion

        #region Drag & Drop File

        public void InitDragDropFile()
        {
            QUtils.AddLog("Entering method: InitDragDropFile()");


            scintilla.AllowDrop = true;
            scintilla.DragEnter += delegate (object sender, DragEventArgs e)
            {
                if (e.Data.GetDataPresent(DataFormats.FileDrop))
                    e.Effect = DragDropEffects.Copy;
                else
                    e.Effect = DragDropEffects.None;
                QUtils.AddLog("Exiting method: InitDragDropFile()");
            };
            scintilla.DragDrop += delegate (object sender, DragEventArgs e)
            {
                string[] filePaths = (string[])e.Data.GetData(DataFormats.FileDrop, false);
                string fileName = filePaths[0];
                scriptFilePathAbsolute = Path.GetDirectoryName(fileName);

                if (fileName.Contains(".qvm") || fileName.Contains(".qsc") || fileName.Contains(".dat"))
                {
                    if (fileName.Contains(".qvm"))
                    {
                        DecompileQVM(fileName);
                    }
                    else
                    {
                        scintilla.Text = QUtils.LoadFile(fileName);
                    }
                }
                else
                {
                    QUtils.ShowError("File type is not a valid QVM(v5) type.");
                    scintilla.Text = String.Empty;
                }
            };

        }

        private void LoadDataFromFile()
        {
            QUtils.AddLog("Entering method: LoadDataFromFile()");

            try
            {
                var fopenIO = QUtils.ShowOpenFileDlg("Select QVM file", ".qvm", "QVM files (*.qvm)|*.qvm|DAT files (*.dat)|*.dat|QSC files (*.qsc)|*.qsc|All files (*.*)|*.*", true);
                string fileName = fopenIO.FileName;
                scriptFilePathAbsolute = Path.GetDirectoryName(fileName);

                string fileExtension = Path.GetExtension(fopenIO.FileName);
                if (fileExtension.ToLower() == ".dat" ||
                    fileExtension.ToLower() == ".qsc" ||
                    fileExtension.ToLower() == ".txt")
                {
                    scintilla.Text = QUtils.LoadFile(fopenIO.FileName);
                    QUtils.AddLog("Exiting method: LoadDataFromFile()");
                }
                else
                {
                    DecompileQVM(fileName);
                }
            }
            catch (Exception ex)
            {
                QUtils.LogException(MethodBase.GetCurrentMethod().Name, ex);
            }
        }

        #endregion

        #region Main Menu Commands

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            QUtils.AddLog("Entering method: openToolStripMenuItem_Click()");
            QUtils.AddLog("openToolStripMenuItem_Click param sender = " + sender);
            QUtils.AddLog("openToolStripMenuItem_Click param e = " + e);

            LoadDataFromFile();
            QUtils.AddLog("Exiting method: openToolStripMenuItem_Click()");
        }

        private void findToolStripMenuItem_Click(object sender, EventArgs e)
        {
            QUtils.AddLog("Entering method: findToolStripMenuItem_Click()");
            QUtils.AddLog("findToolStripMenuItem_Click param sender = " + sender);
            QUtils.AddLog("findToolStripMenuItem_Click param e = " + e);

            OpenSearch();
            QUtils.AddLog("Exiting method: findToolStripMenuItem_Click()");
        }

        private void findAndReplaceToolStripMenuItem_Click(object sender, EventArgs e)
        {
            QUtils.AddLog("Entering method: findAndReplaceToolStripMenuItem_Click()");
            QUtils.AddLog("findAndReplaceToolStripMenuItem_Click param sender = " + sender);
            QUtils.AddLog("findAndReplaceToolStripMenuItem_Click param e = " + e);

            OpenReplaceDialog();
            QUtils.AddLog("Exiting method: findAndReplaceToolStripMenuItem_Click()");
        }

        private void cutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            QUtils.AddLog("Entering method: cutToolStripMenuItem_Click()");
            QUtils.AddLog("cutToolStripMenuItem_Click param sender = " + sender);
            QUtils.AddLog("cutToolStripMenuItem_Click param e = " + e);

            scintilla.Cut();
            QUtils.AddLog("Exiting method: cutToolStripMenuItem_Click()");
        }

        private void copyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            QUtils.AddLog("Entering method: copyToolStripMenuItem_Click()");
            QUtils.AddLog("copyToolStripMenuItem_Click param sender = " + sender);
            QUtils.AddLog("copyToolStripMenuItem_Click param e = " + e);

            scintilla.Copy();
            QUtils.AddLog("Exiting method: copyToolStripMenuItem_Click()");
        }

        private void pasteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            QUtils.AddLog("Entering method: pasteToolStripMenuItem_Click()");
            QUtils.AddLog("pasteToolStripMenuItem_Click param sender = " + sender);
            QUtils.AddLog("pasteToolStripMenuItem_Click param e = " + e);

            scintilla.Paste();
            QUtils.AddLog("Exiting method: pasteToolStripMenuItem_Click()");
        }

        private void selectAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            QUtils.AddLog("Entering method: selectAllToolStripMenuItem_Click()");
            QUtils.AddLog("selectAllToolStripMenuItem_Click param sender = " + sender);
            QUtils.AddLog("selectAllToolStripMenuItem_Click param e = " + e);

            scintilla.SelectAll();
            QUtils.AddLog("Exiting method: selectAllToolStripMenuItem_Click()");
        }

        private void selectLineToolStripMenuItem_Click(object sender, EventArgs e)
        {
            QUtils.AddLog("Entering method: selectLineToolStripMenuItem_Click()");
            QUtils.AddLog("selectLineToolStripMenuItem_Click param sender = " + sender);
            QUtils.AddLog("selectLineToolStripMenuItem_Click param e = " + e);

            Line line = scintilla.Lines[scintilla.CurrentLine];
            scintilla.SetSelection(line.Position + line.Length, line.Position);
            QUtils.AddLog("Exiting method: selectLineToolStripMenuItem_Click()");
        }


        private void indentSelectionToolStripMenuItem_Click(object sender, EventArgs e)
        {
            QUtils.AddLog("Entering method: indentSelectionToolStripMenuItem_Click()");
            QUtils.AddLog("indentSelectionToolStripMenuItem_Click param sender = " + sender);
            QUtils.AddLog("indentSelectionToolStripMenuItem_Click param e = " + e);

            Indent();
            QUtils.AddLog("Exiting method: indentSelectionToolStripMenuItem_Click()");
        }

        private void outdentSelectionToolStripMenuItem_Click(object sender, EventArgs e)
        {
            QUtils.AddLog("Entering method: outdentSelectionToolStripMenuItem_Click()");
            QUtils.AddLog("outdentSelectionToolStripMenuItem_Click param sender = " + sender);
            QUtils.AddLog("outdentSelectionToolStripMenuItem_Click param e = " + e);

            Outdent();
            QUtils.AddLog("Exiting method: outdentSelectionToolStripMenuItem_Click()");
        }

        private void wordWrapToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            QUtils.AddLog("Entering method: wordWrapToolStripMenuItem1_Click()");
            QUtils.AddLog("wordWrapToolStripMenuItem1_Click param sender = " + sender);
            QUtils.AddLog("wordWrapToolStripMenuItem1_Click param e = " + e);

            // toggle word wrap
            wordWrapItem.Checked = !wordWrapItem.Checked;
            scintilla.WrapMode = wordWrapItem.Checked ? WrapMode.Word : WrapMode.None;
            QUtils.AddLog("Exiting method: wordWrapToolStripMenuItem1_Click()");
        }

        private void indentGuidesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            QUtils.AddLog("Entering method: indentGuidesToolStripMenuItem_Click()");
            QUtils.AddLog("indentGuidesToolStripMenuItem_Click param sender = " + sender);
            QUtils.AddLog("indentGuidesToolStripMenuItem_Click param e = " + e);

            // toggle indent guides
            indentGuidesItem.Checked = !indentGuidesItem.Checked;
            scintilla.IndentationGuides = indentGuidesItem.Checked ? IndentView.LookBoth : IndentView.None;
            QUtils.AddLog("Exiting method: indentGuidesToolStripMenuItem_Click()");
        }

        private void hiddenCharactersToolStripMenuItem_Click(object sender, EventArgs e)
        {
            QUtils.AddLog("Entering method: hiddenCharactersToolStripMenuItem_Click()");
            QUtils.AddLog("hiddenCharactersToolStripMenuItem_Click param sender = " + sender);
            QUtils.AddLog("hiddenCharactersToolStripMenuItem_Click param e = " + e);

            // toggle view whitespace
            hiddenCharactersItem.Checked = !hiddenCharactersItem.Checked;
            scintilla.ViewWhitespace = hiddenCharactersItem.Checked ? WhitespaceMode.VisibleAlways : WhitespaceMode.Invisible;
            QUtils.AddLog("Exiting method: hiddenCharactersToolStripMenuItem_Click()");
        }

        private void GotoLine(int lineNumber)
        {
            QUtils.AddLog("Entering method: GotoLine()");
            QUtils.AddLog("GotoLine param lineNumber = " + lineNumber);

            if (lineNumber > scintilla.Lines.Count)
                return;

            scintilla.Lines[lineNumber - 1].Goto();
            QUtils.AddLog("Exiting method: GotoLine()");
        }

        private void goToLineToolStripMenuItem_Click(object sender, EventArgs e)
        {
            QUtils.AddLog("Entering method: goToLineToolStripMenuItem_Click()");
            QUtils.AddLog("goToLineToolStripMenuItem_Click param sender = " + sender);
            QUtils.AddLog("goToLineToolStripMenuItem_Click param e = " + e);

            PanelReplace.Visible = true;

            if (PanelReplace.Visible)
            {
                if (!String.IsNullOrEmpty(ReplaceTextBox.Text))
                {
                    int lineNumber = Convert.ToInt32(ReplaceTextBox.Text);
                    GotoLine(lineNumber);
                    QUtils.AddLog("Exiting method: goToLineToolStripMenuItem_Click()");
                }
            }
        }

        private void zoomInToolStripMenuItem_Click(object sender, EventArgs e)
        {
            QUtils.AddLog("Entering method: zoomInToolStripMenuItem_Click()");
            QUtils.AddLog("zoomInToolStripMenuItem_Click param sender = " + sender);
            QUtils.AddLog("zoomInToolStripMenuItem_Click param e = " + e);

            ZoomIn();
            QUtils.AddLog("Exiting method: zoomInToolStripMenuItem_Click()");
        }

        private void zoomOutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            QUtils.AddLog("Entering method: zoomOutToolStripMenuItem_Click()");
            QUtils.AddLog("zoomOutToolStripMenuItem_Click param sender = " + sender);
            QUtils.AddLog("zoomOutToolStripMenuItem_Click param e = " + e);

            ZoomOut();
            QUtils.AddLog("Exiting method: zoomOutToolStripMenuItem_Click()");
        }

        private void zoom100ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            QUtils.AddLog("Entering method: zoom100ToolStripMenuItem_Click()");
            QUtils.AddLog("zoom100ToolStripMenuItem_Click param sender = " + sender);
            QUtils.AddLog("zoom100ToolStripMenuItem_Click param e = " + e);

            ZoomDefault();
            QUtils.AddLog("Exiting method: zoom100ToolStripMenuItem_Click()");
        }

        private void collapseAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            QUtils.AddLog("Entering method: collapseAllToolStripMenuItem_Click()");
            QUtils.AddLog("collapseAllToolStripMenuItem_Click param sender = " + sender);
            QUtils.AddLog("collapseAllToolStripMenuItem_Click param e = " + e);

            scintilla.FoldAll(FoldAction.Contract);
            QUtils.AddLog("Exiting method: collapseAllToolStripMenuItem_Click()");
        }

        private void expandAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            QUtils.AddLog("Entering method: expandAllToolStripMenuItem_Click()");
            QUtils.AddLog("expandAllToolStripMenuItem_Click param sender = " + sender);
            QUtils.AddLog("expandAllToolStripMenuItem_Click param e = " + e);

            scintilla.FoldAll(FoldAction.Expand);
            QUtils.AddLog("Exiting method: expandAllToolStripMenuItem_Click()");
        }


        #endregion

        #region Indent / Outdent

        private void Indent()
        {
            QUtils.AddLog("Entering method: Indent()");

            // we use this hack to send "Shift+Tab" to scintilla, since there is no known API to indent,
            // although the indentation function exists. Pressing TAB with the editor focused confirms this.
            GenerateKeystrokes("{TAB}");
            QUtils.AddLog("Exiting method: Indent()");
        }

        private void Outdent()
        {
            QUtils.AddLog("Entering method: Outdent()");

            // we use this hack to send "Shift+Tab" to scintilla, since there is no known API to outdent,
            // although the indentation function exists. Pressing Shift+Tab with the editor focused confirms this.
            GenerateKeystrokes("+{TAB}");
            QUtils.AddLog("Exiting method: Outdent()");
        }

        private void GenerateKeystrokes(string keys)
        {
            QUtils.AddLog("Entering method: GenerateKeystrokes()");
            QUtils.AddLog("GenerateKeystrokes param keys = " + keys);

            HotKeyManager.Enable = false;
            scintilla.Focus();
            SendKeys.Send(keys);
            HotKeyManager.Enable = true;
            QUtils.AddLog("Exiting method: GenerateKeystrokes()");
        }

        #endregion

        #region Zoom

        private void ZoomIn()
        {
            QUtils.AddLog("Entering method: ZoomIn()");

            scintilla.ZoomIn();
            QUtils.AddLog("Exiting method: ZoomIn()");
        }

        private void ZoomOut()
        {
            QUtils.AddLog("Entering method: ZoomOut()");

            scintilla.ZoomOut();
            QUtils.AddLog("Exiting method: ZoomOut()");
        }

        private void ZoomDefault()
        {
            QUtils.AddLog("Entering method: ZoomDefault()");

            scintilla.Zoom = 0;
            QUtils.AddLog("Exiting method: ZoomDefault()");
        }


        #endregion

        #region Quick Search Bar

        bool SearchIsOpen = false;
        bool ReplaceIsOpen = false;

        private void OpenSearch()
        {
            QUtils.AddLog("Entering method: OpenSearch()");


            SearchManager.SearchBox = SearchTextBox;
            SearchManager.TextArea = scintilla;

            if (!SearchIsOpen)
            {
                SearchIsOpen = true;
                InvokeIfNeeded(delegate ()
                {
                    PanelSearch.Visible = true;
                    SearchTextBox.Text = SearchManager.LastSearch;
                    SearchTextBox.Focus();
                    SearchTextBox.SelectAll();
                    QUtils.AddLog("Exiting method: OpenSearch()");
                });
            }
            else
            {
                InvokeIfNeeded(delegate ()
                {
                    SearchTextBox.Focus();
                    SearchTextBox.SelectAll();
                });
            }
        }

        private void CloseSearchPanel(Panel panel, ref bool panelIsOpen)
        {
            QUtils.AddLog("Entering method: CloseSearchPanel()");
            QUtils.AddLog("CloseSearchPanel param panel = " + panel);
            QUtils.AddLog("CloseSearchPanel param panelIsOpen = " + panelIsOpen);

            if (panelIsOpen)
            {
                panelIsOpen = false;
                InvokeIfNeeded(delegate ()
                {
                    panel.Visible = false;
                    QUtils.AddLog("Exiting method: CloseSearchPanel()");
                });
            }
        }

        private void BtnClearSearch_Click(object sender, EventArgs e)
        {
            QUtils.AddLog("Entering method: BtnClearSearch_Click()");
            QUtils.AddLog("BtnClearSearch_Click param sender = " + sender);
            QUtils.AddLog("BtnClearSearch_Click param e = " + e);

            CloseSearchPanel(PanelSearch, ref SearchIsOpen);
            QUtils.AddLog("Exiting method: BtnClearSearch_Click()");
        }

        private void BtnCloseReplace_Click(object sender, EventArgs e)
        {
            QUtils.AddLog("Entering method: BtnCloseReplace_Click()");
            QUtils.AddLog("BtnCloseReplace_Click param sender = " + sender);
            QUtils.AddLog("BtnCloseReplace_Click param e = " + e);

            CloseSearchPanel(PanelReplace, ref ReplaceIsOpen);
            QUtils.AddLog("Exiting method: BtnCloseReplace_Click()");
        }

        private void BtnPrevSearch_Click(object sender, EventArgs e)
        {
            QUtils.AddLog("Entering method: BtnPrevSearch_Click()");
            QUtils.AddLog("BtnPrevSearch_Click param sender = " + sender);
            QUtils.AddLog("BtnPrevSearch_Click param e = " + e);

            SearchManager.Find(false, false);
            QUtils.AddLog("Exiting method: BtnPrevSearch_Click()");
        }
        private void BtnNextSearch_Click(object sender, EventArgs e)
        {
            QUtils.AddLog("Entering method: BtnNextSearch_Click()");
            QUtils.AddLog("BtnNextSearch_Click param sender = " + sender);
            QUtils.AddLog("BtnNextSearch_Click param e = " + e);

            SearchManager.Find(true, false);
            QUtils.AddLog("Exiting method: BtnNextSearch_Click()");
        }
        private void TxtSearch_TextChanged(object sender, EventArgs e)
        {
            QUtils.AddLog("Entering method: TxtSearch_TextChanged()");
            QUtils.AddLog("TxtSearch_TextChanged param sender = " + sender);
            QUtils.AddLog("TxtSearch_TextChanged param e = " + e);

            SearchManager.Find(true, true);
            QUtils.AddLog("Exiting method: TxtSearch_TextChanged()");
        }

        private void TxtSearch_KeyDown(object sender, KeyEventArgs e)
        {
            QUtils.AddLog("Entering method: TxtSearch_KeyDown()");
            QUtils.AddLog("TxtSearch_KeyDown param sender = " + sender);
            QUtils.AddLog("TxtSearch_KeyDown param e = " + e);

            if (HotKeyManager.IsHotkey(e, Keys.Enter))
            {
                SearchManager.Find(true, false);
                QUtils.AddLog("Exiting method: TxtSearch_KeyDown()");
            }
            if (HotKeyManager.IsHotkey(e, Keys.Enter, true) || HotKeyManager.IsHotkey(e, Keys.Enter, false, true))
            {
                SearchManager.Find(false, false);
            }
        }

        private void ReplaceTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            QUtils.AddLog("Entering method: ReplaceTextBox_KeyDown()");
            QUtils.AddLog("ReplaceTextBox_KeyDown param sender = " + sender);
            QUtils.AddLog("ReplaceTextBox_KeyDown param e = " + e);

            if (HotKeyManager.IsHotkey(e, Keys.Enter))
            {
                OpenReplaceDialog();
                QUtils.AddLog("Exiting method: ReplaceTextBox_KeyDown()");
            }
        }

        #endregion

        #region Find & Replace Dialog

        private void OpenReplaceDialog()
        {
            QUtils.AddLog("Entering method: OpenReplaceDialog()");

            SearchManager.SearchBox = SearchTextBox;
            SearchManager.TextArea = scintilla;

            if (!ReplaceIsOpen)
            {
                ReplaceIsOpen = true;
                InvokeIfNeeded(delegate ()
                {
                    PanelSearch.Visible = PanelReplace.Visible = true;
                    SearchTextBox.Text = SearchManager.LastSearch;
                    SearchTextBox.Focus();
                    SearchTextBox.SelectAll();
                    QUtils.AddLog("Exiting method: OpenReplaceDialog()");
                });
            }
            else
            {
                InvokeIfNeeded(delegate ()
                {
                    SearchTextBox.Focus();
                    SearchTextBox.SelectAll();
                });
            }

            string replaceText = ReplaceTextBox.Text;
            scintilla.ReplaceSelection(replaceText);
        }

        #endregion

        #region Utils

        public static Color IntToColor(int rgb)
        {
            return Color.FromArgb(255, (byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb);
        }

        public void InvokeIfNeeded(Action action)
        {
            QUtils.AddLog("Entering method: InvokeIfNeeded()");
            QUtils.AddLog("InvokeIfNeeded param action = " + action);

            if (this.InvokeRequired)
            {
                this.BeginInvoke(action);
                QUtils.AddLog("Exiting method: InvokeIfNeeded()");
            }
            else
            {
                action.Invoke();
            }
        }




        #endregion

        #region Themes

        private void DarkThemeStyle(string fontName, int fontSize)
        {
            QUtils.AddLog("Entering method: DarkThemeStyle()");
            QUtils.AddLog("DarkThemeStyle param fontName = " + fontName);
            QUtils.AddLog("DarkThemeStyle param fontSize = " + fontSize);

            // Configure the default style
            scintilla.StyleResetDefault();
            scintilla.Styles[Style.Default].Font = fontName;
            scintilla.Styles[Style.Default].Size = fontSize;
            scintilla.Styles[Style.Default].BackColor = IntToColor(0x212121);
            scintilla.Styles[Style.Default].ForeColor = IntToColor(0xFFFFFF);
            scintilla.StyleClearAll();

            // Configure the CPP (C#) lexer styles
            scintilla.Styles[Style.Cpp.Identifier].ForeColor = IntToColor(0xD0DAE2);
            scintilla.Styles[Style.Cpp.Comment].ForeColor = IntToColor(0xBD758B);
            scintilla.Styles[Style.Cpp.CommentLine].ForeColor = IntToColor(0x40BF57);
            scintilla.Styles[Style.Cpp.CommentDoc].ForeColor = IntToColor(0x2FAE35);
            scintilla.Styles[Style.Cpp.Number].ForeColor = IntToColor(0xFFFF00);
            scintilla.Styles[Style.Cpp.String].ForeColor = IntToColor(0xFFFF00);
            scintilla.Styles[Style.Cpp.Character].ForeColor = IntToColor(0xE95454);
            scintilla.Styles[Style.Cpp.Preprocessor].ForeColor = IntToColor(0x8AAFEE);
            scintilla.Styles[Style.Cpp.Operator].ForeColor = IntToColor(0xE0E0E0);
            scintilla.Styles[Style.Cpp.Regex].ForeColor = IntToColor(0xff00ff);
            scintilla.Styles[Style.Cpp.CommentLineDoc].ForeColor = IntToColor(0x77A7DB);
            scintilla.Styles[Style.Cpp.Word].ForeColor = IntToColor(0x48A8EE);
            scintilla.Styles[Style.Cpp.Word2].ForeColor = IntToColor(0xF98906);
            scintilla.Styles[Style.Cpp.CommentDocKeyword].ForeColor = IntToColor(0xB3D991);
            scintilla.Styles[Style.Cpp.CommentDocKeywordError].ForeColor = IntToColor(0xFF0000);
            scintilla.Styles[Style.Cpp.GlobalClass].ForeColor = IntToColor(0x48A8EE);

            scintilla.Lexer = Lexer.Cpp;
            QUtils.AddLog("Exiting method: DarkThemeStyle()");
        }

        private void LightThemeStyle(string fontName, int fontSize)
        {
            QUtils.AddLog("Entering method: LightThemeStyle()");
            QUtils.AddLog("LightThemeStyle param fontName = " + fontName);
            QUtils.AddLog("LightThemeStyle param fontSize = " + fontSize);

            // Configuring the default style with properties
            // we have common to every lexer style saves time.
            scintilla.StyleResetDefault();
            scintilla.Styles[Style.Default].Font = fontName;
            scintilla.Styles[Style.Default].Size = fontSize;
            scintilla.StyleClearAll();

            // Configure the Lua lexer styles
            scintilla.Styles[Style.Lua.Default].ForeColor = Color.Silver;
            scintilla.Styles[Style.Lua.Comment].ForeColor = Color.Green;
            scintilla.Styles[Style.Lua.CommentLine].ForeColor = Color.Green;
            scintilla.Styles[Style.Lua.Number].ForeColor = Color.Olive;
            scintilla.Styles[Style.Lua.Word].ForeColor = Color.Blue;
            scintilla.Styles[Style.Lua.Word2].ForeColor = Color.BlueViolet;
            scintilla.Styles[Style.Lua.Word3].ForeColor = Color.DarkSlateBlue;
            scintilla.Styles[Style.Lua.Word4].ForeColor = Color.DarkSlateBlue;
            scintilla.Styles[Style.Lua.String].ForeColor = Color.Red;
            scintilla.Styles[Style.Lua.Character].ForeColor = Color.Red;
            scintilla.Styles[Style.Lua.LiteralString].ForeColor = Color.Red;
            scintilla.Styles[Style.Lua.StringEol].BackColor = Color.Pink;
            scintilla.Styles[Style.Lua.Operator].ForeColor = Color.Purple;
            scintilla.Styles[Style.Lua.Preprocessor].ForeColor = Color.Maroon;
            scintilla.Lexer = Lexer.Lua;
            QUtils.AddLog("Exiting method: LightThemeStyle()");
        }

        private void ClassicThemeStyle(string fontName, int fontSize)
        {
            QUtils.AddLog("Entering method: ClassicThemeStyle()");
            QUtils.AddLog("ClassicThemeStyle param fontName = " + fontName);
            QUtils.AddLog("ClassicThemeStyle param fontSize = " + fontSize);

            // Reset the styles
            scintilla.StyleResetDefault();
            scintilla.Styles[Style.Default].Font = fontName;
            scintilla.Styles[Style.Default].Size = fontSize;
            scintilla.StyleClearAll(); // i.e. Apply to all

            // Set the lexer
            scintilla.Lexer = Lexer.Python;

            // Use margin 2 for fold markers
            scintilla.Margins[2].Type = MarginType.Symbol;
            scintilla.Margins[2].Mask = Marker.MaskFolders;
            scintilla.Margins[2].Sensitive = true;
            scintilla.Margins[2].Width = 20;

            // Reset folder markers
            for (int i = Marker.FolderEnd; i <= Marker.FolderOpen; i++)
            {
                scintilla.Markers[i].SetForeColor(SystemColors.ControlLightLight);
                scintilla.Markers[i].SetBackColor(SystemColors.ControlDark);
                QUtils.AddLog("Exiting method: ClassicThemeStyle()");
            }

            // Style the folder markers
            scintilla.Markers[Marker.Folder].Symbol = MarkerSymbol.BoxPlus;
            scintilla.Markers[Marker.Folder].SetBackColor(SystemColors.ControlText);
            scintilla.Markers[Marker.FolderOpen].Symbol = MarkerSymbol.BoxMinus;
            scintilla.Markers[Marker.FolderEnd].Symbol = MarkerSymbol.BoxPlusConnected;
            scintilla.Markers[Marker.FolderEnd].SetBackColor(SystemColors.ControlText);
            scintilla.Markers[Marker.FolderMidTail].Symbol = MarkerSymbol.TCorner;
            scintilla.Markers[Marker.FolderOpenMid].Symbol = MarkerSymbol.BoxMinusConnected;
            scintilla.Markers[Marker.FolderSub].Symbol = MarkerSymbol.VLine;
            scintilla.Markers[Marker.FolderTail].Symbol = MarkerSymbol.LCorner;

            // Enable automatic folding
            scintilla.AutomaticFold = (AutomaticFold.Show | AutomaticFold.Click | AutomaticFold.Change);

            // Set the styles
            scintilla.Styles[Style.Python.Default].ForeColor = Color.FromArgb(0x80, 0x80, 0x80);
            scintilla.Styles[Style.Python.CommentLine].ForeColor = Color.FromArgb(0x00, 0x7F, 0x00);
            scintilla.Styles[Style.Python.CommentLine].Italic = true;
            scintilla.Styles[Style.Python.Number].ForeColor = Color.FromArgb(0x00, 0x7F, 0x7F);
            scintilla.Styles[Style.Python.String].ForeColor = Color.FromArgb(0x7F, 0x00, 0x7F);
            scintilla.Styles[Style.Python.Character].ForeColor = Color.FromArgb(0x7F, 0x00, 0x7F);
            scintilla.Styles[Style.Python.Word].ForeColor = Color.FromArgb(0x00, 0x00, 0x7F);
            scintilla.Styles[Style.Python.Word].Bold = true;
            scintilla.Styles[Style.Python.Triple].ForeColor = Color.FromArgb(0x7F, 0x00, 0x00);
            scintilla.Styles[Style.Python.TripleDouble].ForeColor = Color.FromArgb(0x7F, 0x00, 0x00);
            scintilla.Styles[Style.Python.ClassName].ForeColor = Color.FromArgb(0x00, 0x00, 0xFF);
            scintilla.Styles[Style.Python.ClassName].Bold = true;
            scintilla.Styles[Style.Python.DefName].ForeColor = Color.FromArgb(0x00, 0x7F, 0x7F);
            scintilla.Styles[Style.Python.DefName].Bold = true;
            scintilla.Styles[Style.Python.Operator].Bold = true;

            scintilla.Styles[Style.Python.CommentBlock].ForeColor = Color.FromArgb(0x7F, 0x7F, 0x7F);
            scintilla.Styles[Style.Python.CommentBlock].Italic = true;
            scintilla.Styles[Style.Python.StringEol].ForeColor = Color.FromArgb(0x00, 0x00, 0x00);
            scintilla.Styles[Style.Python.StringEol].BackColor = Color.FromArgb(0xE0, 0xC0, 0xE0);
            scintilla.Styles[Style.Python.StringEol].FillLine = true;
            scintilla.Styles[Style.Python.Word2].ForeColor = Color.FromArgb(0x40, 0x70, 0x90);
            scintilla.Styles[Style.Python.Decorator].ForeColor = Color.FromArgb(0x80, 0x50, 0x00);
        }

        public static bool IsNight(DateTime from, DateTime to)
=> (to > from) && (from.Hour < 6 || to.Hour < 6 || to > from.Date.AddDays(1));

        private void AutomaticThemeStyle()
        {
            QUtils.AddLog("Entering method: AutomaticThemeStyle()");

            if (IsNight(DateTime.Now, DateTime.Now.AddHours(6)))
            {
                DarkThemeStyle(fontName, fontSize);
                InitTextSelectionColors(true);
                QUtils.AddLog("Exiting method: AutomaticThemeStyle()");
            }
            else
            {
                ClassicThemeStyle(fontName, fontSize);
                InitTextSelectionColors(false);
            }
        }

        private void automaticToolStripMenuItem_Click(object sender, EventArgs e)
        {
            QUtils.AddLog("Entering method: automaticToolStripMenuItem_Click()");
            QUtils.AddLog("automaticToolStripMenuItem_Click param sender = " + sender);
            QUtils.AddLog("automaticToolStripMenuItem_Click param e = " + e);

            AutomaticThemeStyle();
            QUtils.AddLog("Exiting method: automaticToolStripMenuItem_Click()");
        }

        private void darkToolStripMenuItem_Click(object sender, EventArgs e)
        {
            QUtils.AddLog("Entering method: darkToolStripMenuItem_Click()");
            QUtils.AddLog("darkToolStripMenuItem_Click param sender = " + sender);
            QUtils.AddLog("darkToolStripMenuItem_Click param e = " + e);

            DarkThemeStyle(fontName, fontSize);
            InitTextSelectionColors(true);
            QUtils.AddLog("Exiting method: darkToolStripMenuItem_Click()");
        }

        private void lightToolStripMenuItem_Click(object sender, EventArgs e)
        {
            QUtils.AddLog("Entering method: lightToolStripMenuItem_Click()");
            QUtils.AddLog("lightToolStripMenuItem_Click param sender = " + sender);
            QUtils.AddLog("lightToolStripMenuItem_Click param e = " + e);

            LightThemeStyle(fontName, fontSize);
            InitTextSelectionColors(false);
            QUtils.AddLog("Exiting method: lightToolStripMenuItem_Click()");
        }

        private void classicToolStripMenuItem_Click(object sender, EventArgs e)
        {
            QUtils.AddLog("Entering method: classicToolStripMenuItem_Click()");
            QUtils.AddLog("classicToolStripMenuItem_Click param sender = " + sender);
            QUtils.AddLog("classicToolStripMenuItem_Click param e = " + e);

            ClassicThemeStyle(fontName, fontSize);
            InitTextSelectionColors(false);
            QUtils.AddLog("Exiting method: classicToolStripMenuItem_Click()");
        }

        #endregion

        #region Font
        private void smallToolStripMenuItem_Click(object sender, EventArgs e)
        {
            QUtils.AddLog("Entering method: smallToolStripMenuItem_Click()");
            QUtils.AddLog("smallToolStripMenuItem_Click param sender = " + sender);
            QUtils.AddLog("smallToolStripMenuItem_Click param e = " + e);

            fontSize = 10;
            QUtils.AddLog("Exiting method: smallToolStripMenuItem_Click()");
        }

        private void mediumToolStripMenuItem_Click(object sender, EventArgs e)
        {
            QUtils.AddLog("Entering method: mediumToolStripMenuItem_Click()");
            QUtils.AddLog("mediumToolStripMenuItem_Click param sender = " + sender);
            QUtils.AddLog("mediumToolStripMenuItem_Click param e = " + e);

            fontSize = 14;
            QUtils.AddLog("Exiting method: mediumToolStripMenuItem_Click()");
        }

        private void largeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            QUtils.AddLog("Entering method: largeToolStripMenuItem_Click()");
            QUtils.AddLog("largeToolStripMenuItem_Click param sender = " + sender);
            QUtils.AddLog("largeToolStripMenuItem_Click param e = " + e);

            fontSize = 18;
            QUtils.AddLog("Exiting method: largeToolStripMenuItem_Click()");
        }
        #endregion

        #region CoreMethods

        private void scriptViewerText_CharAdded(object sender, CharAddedEventArgs e)
        {
            QUtils.AddLog("Entering method: scriptViewerText_CharAdded()");
            QUtils.AddLog("scriptViewerText_CharAdded param sender = " + sender);
            QUtils.AddLog("scriptViewerText_CharAdded param e = " + e);

            // Find the word start
            var currentPos = scintilla.CurrentPosition;
            var wordStartPos = scintilla.WordStartPosition(currentPos, true);

            // Display the autocompletion list
            var word = currentPos - wordStartPos;
            if (word >= 3)
            {
                if (!scintilla.AutoCActive)
                {
                    scintilla.AutoCShow(word, QUtils.keywords);
                    QUtils.AddLog("Exiting method: scriptViewerText_CharAdded()");
                }
            }
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            QUtils.AddLog("Entering method: saveToolStripMenuItem_Click()");
            QUtils.AddLog("saveToolStripMenuItem_Click param sender = " + sender);
            QUtils.AddLog("saveToolStripMenuItem_Click param e = " + e);

            string scriptFileQvm = scriptFilePathAbsolute + "\\" + fileNameLabel.Text;
            string scriptFileQsc = fileNameLabel.Text.Replace(QUtils.qvmFile, QUtils.qscFile);
            string scriptData = scintilla.Text;

            // Get the first 10 lines of scriptData
            string[] allLines = scriptData.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
            int lineCount = Math.Min(10, allLines.Length);
            string scriptDataLines = string.Join(Environment.NewLine, allLines, 0, lineCount);

            QUtils.AddLog(string.Format("Saving file: {0} scriptFileQvm: {1}" +
                          "scriptData:{2}{3}",
                          scriptFileQsc, scriptFileQvm, Environment.NewLine, scriptDataLines));
            bool status = QCompiler.Compile(scriptData, scriptFilePathAbsolute, false, fileNameLabel.Text.Replace(QUtils.qvmFile, QUtils.qscFile));

            if (status)
            {
                QUtils.FileIODelete(scriptFileQsc);
                QUtils.AddLog("File saved successfully");
                QUtils.AddLog("Exiting method: saveToolStripMenuItem_Click()");
            }
        }


        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            QUtils.AddLog("Entering method: exitToolStripMenuItem_Click()");
            QUtils.AddLog("exitToolStripMenuItem_Click param sender = " + sender);
            QUtils.AddLog("exitToolStripMenuItem_Click param e = " + e);

            Environment.Exit(0);
            QUtils.AddLog("Exiting method: exitToolStripMenuItem_Click()");
        }

        private void DecompileQVM(string fileName)
        {
            QUtils.AddLog("Entering method: DecompileQVM()");
            QUtils.AddLog("DecompileQVM param fileName = " + fileName);

            QUtils.AddLog("Decompiling file: " + fileName);
            QCompiler.DecompileFile(fileName, QUtils.appOutPath);
            QUtils.AddLog("Decompiling done");

            scriptFilePath = QUtils.appOutPath + Path.DirectorySeparatorChar + Path.GetFileName(fileName).Replace(QUtils.qvmFile, QUtils.qscFile);
            fileNameLabel.Text = Path.GetFileNameWithoutExtension(fileName) + QUtils.qvmFile;
            scintilla.Text = QUtils.LoadFile(scriptFilePath);
            QUtils.AddLog($"Files path are {scriptFilePath} {fileNameLabel.Text}");

            // decompile the qvm version.
            QUtils.qvmVersion = QUtils.ReadQVMVersion(fileName);
            InvokeIfNeeded(() => appVersionTxt.Text += QUtils.qvmVersion);
            QUtils.AddLog("Exiting method: DecompileQVM()");
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            QUtils.AddLog("Entering method: aboutToolStripMenuItem_Click()");
            QUtils.AddLog("aboutToolStripMenuItem_Click param sender = " + sender);
            QUtils.AddLog("aboutToolStripMenuItem_Click param e = " + e);

            string infoMsg = "Project IGI QVM Editor is tool to Edit QVM File in IGI 1 & IGI 2\n" +
    "Developed by: Haseeb Mir\n" +
    "App/Language: C# (.NET 4.0) / GUI.\n" +
    "Credits/Thanks: Artiom (QCompiler), Dark (UI/Tools).\n" +
    "Application Version: v" + QUtils.appVersion + "\n";
            QUtils.ShowInfo(infoMsg);
            QUtils.AddLog("Exiting method: aboutToolStripMenuItem_Click()");
        }

        #endregion
    }
}
