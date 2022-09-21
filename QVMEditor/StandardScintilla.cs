using System;
using System.Text.RegularExpressions;
using System.Windows.Forms;


namespace QVM_Editor
{
    /// <summary>
    /// StandardScintilla enhances contextmenu of ScintillaNET - based on Stephen Swensen's Programming Blog
    /// http://swensencode.blogspot.com/2013/03/extending-scintilla-with-modifiable.html
    /// </summary>
    public class StandardScintilla : ScintillaNET.Scintilla
    {
        MenuItem miUndo;
        MenuItem miRedo;
        MenuItem miCut;
        MenuItem miCopy;
        MenuItem miDelete;
        MenuItem miSelectAll;

        public StandardScintilla() : base()
        {
            initContextMenu();
        }

        private void initContextMenu()
        {
            var cm = this.ContextMenu = new ContextMenu();
            {
                this.miUndo = new MenuItem("Undo", (s, ea) => this.Undo());
                cm.MenuItems.Add(this.miUndo);
            }
            {
                this.miRedo = new MenuItem("Redo", (s, ea) => this.Redo());
                cm.MenuItems.Add(this.miRedo);
            }
            cm.MenuItems.Add(new MenuItem("-"));
            {
                this.miCut = new MenuItem("Cut", (s, ea) => this.Cut());
                cm.MenuItems.Add(miCut);
            }
            {
                this.miCopy = new MenuItem("Copy", (s, ea) => this.Copy());
                cm.MenuItems.Add(miCopy);
            }
            cm.MenuItems.Add(new MenuItem("Paste", (s, ea) => this.Paste()));
            {
                this.miDelete = new MenuItem("Delete", (s, ea) => this.ReplaceSelection(""));
                cm.MenuItems.Add(miDelete);
            }
            cm.MenuItems.Add(new MenuItem("-"));
            {
                this.miSelectAll = new MenuItem("Select All", (s, ea) => this.SelectAll());
                cm.MenuItems.Add(miSelectAll);
            }
            cm.MenuItems.Add(new MenuItem("-"));
            {
                MenuItem modelInformation = new MenuItem("Model Information", (s, ea) => this.ModelInformation(ea));
                cm.MenuItems.Add(modelInformation);
            }
            cm.MenuItems.Add(new MenuItem("-"));
            {
                MenuItem revealAllModels = new MenuItem("Reveal All Models", (s, ea) => this.RevealAllModels(ea));
                cm.MenuItems.Add(revealAllModels);
            }
        }


        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                miUndo.Enabled = this.CanUndo;
                miRedo.Enabled = this.CanRedo;
                miCut.Enabled = true;
                miCopy.Enabled = true;
                miDelete.Enabled = this.SelectedText.Length > 0;
                miSelectAll.Enabled = this.Text.Length > 0 && this.Text.Length != this.SelectedText.Length;
            }
            else
                base.OnMouseDown(e);
        }

        protected void ModelInformation(EventArgs e)
        {
            string objectType = this.SelectedText;

            var modelRegex = @"\d{3}_\d{2}_\d{1}";
            var modelId = Regex.Match(objectType, modelRegex).Value;
            if (!String.IsNullOrEmpty(modelId))
            {
                string modelName = QUtils.FindModelName(modelId);
                QUtils.ShowInfo("Model: '" + modelName + "'");
            }
        }

        private void RevealAllModels(EventArgs ea)
        {
            string scriptText = QVMEditorForm.qvmInstance.scintilla.Text;

            var modelRegex = @"\d{3}_\d{2}_\d{1}";
            var scriptTextList = scriptText.Split('\n');

            foreach (var text in scriptTextList)
            {
                var modelId = Regex.Match(text, modelRegex).Value;
                if (!String.IsNullOrEmpty(modelId))
                {
                    string modelName = QUtils.FindModelName(modelId);
                    scriptText = scriptText.Replace(modelId, modelName);
                }
            }

            QVMEditorForm.qvmInstance.scintilla.Text = scriptText;
        }
    }
}