using LiveSplit.Model;
using LiveSplit.TheLastCampfire;
using LiveSplit.UI.Components;
using LiveSplit.VoxSplitter;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using System.Windows.Forms;

[assembly: ComponentFactory(typeof(TheLastCampfireFactory))]
namespace LiveSplit.TheLastCampfire {
    public class TheLastCampfireFactory : IComponentFactory {

        public const string BaseSplitter = "LiveSplit.VoxSplitter";
        public const string BaseDll = BaseSplitter + ".dll";
        public const string BaseVer = "Version";

        public const string GitURL = "https://raw.githubusercontent.com/Voxelse/";
        public const string BaseURL = GitURL + BaseSplitter + "/main/Components/";

        public TheLastCampfireFactory() {
            foreach(Assembly asm in AppDomain.CurrentDomain.GetAssemblies()) {
                if(asm.GetName().Name == BaseSplitter) {
                    return;
                }
            }
            LoadBaseAssembly();
        }

        public virtual IComponent Create(LiveSplitState state) => new TheLastCampfireComponent(state, ExAssembly);
        public ComponentCategory Category => ComponentCategory.Control;
        public string ComponentName => ExAssembly.FullComponentName();
        public string UpdateName => ComponentName;
        public string Description => ExAssembly.Description();
        public string UpdateURL => ExAssembly.GitMainURL();
        public string XMLURL => Path.Combine(UpdateURL, "Components", "ComponentsUpdate.xml");
        public Version Version => ExAssembly.GetName().Version;

        public Assembly ExAssembly => Assembly.GetExecutingAssembly();

        private void LoadBaseAssembly() {
            string version;
            try {
                using(WebClient webClient = new WebClient()) {
                    version = webClient.DownloadString(BaseURL + BaseVer);
                }
            } catch(Exception e) {
                version = "1.0.0";
                Options.Log.Error(e.ToString());
            }
            string baseSplitterPath = Path.Combine(Path.GetDirectoryName(ExAssembly.Location), BaseSplitter, BaseDll);
            if(!File.Exists(baseSplitterPath)
            || new Version(FileVersionInfo.GetVersionInfo(baseSplitterPath).FileVersion) < new Version(version)) {
                DownloadBaseDll(baseSplitterPath);
            }
            Assembly.LoadFrom(baseSplitterPath);
        }

        private void DownloadBaseDll(string basePath) {
            Form dlForm = new Form {
                FormBorderStyle = FormBorderStyle.FixedToolWindow,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                AutoSize = true,
            };
            dlForm.Controls.Add(new Label {
                Text = "Downloading/updating base splitter library...",
                Padding = new Padding(10),
                Dock = DockStyle.Fill,
                AutoSize = true,
            });
            dlForm.Show();
            dlForm.Refresh();

            string baseDir = Path.GetDirectoryName(basePath);
            if(!Directory.Exists(baseDir)) {
                Directory.CreateDirectory(baseDir);
            }

            string dllName = Path.GetFileName(basePath);
            string tempDllName = dllName + "-temp";
            string tempPath = Path.GetFullPath(Path.Combine(baseDir, tempDllName));

            try {
                using(WebClient webClient = new WebClient()) {
                    webClient.DownloadFile(BaseURL + BaseDll, tempPath);
                }
                File.Copy(tempPath, basePath, true);
            } catch(Exception e) {
                Options.Log.Error(e.ToString());
            } finally {
                try {
                    File.Delete(tempPath);
                } catch(Exception e) {
                    Options.Log.Error(e.ToString());
                }
            }
            dlForm.Dispose();
        }
    }
}