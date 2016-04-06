using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.VersionControl.Client;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using TFSMigrationTool.Command;
using TFSMigrationTool.Models;
using TFSMigrationTool.Utils;

namespace TFSMigrationTool.ViewModels
{
    /// <summary>
    /// Migrates code without any history.
    /// </summary>
    public class MigrateViewModel : AbstractViewModel
    {
        protected override string ViewName
        {
            get
            {
                return "Migrate";
            }
        }
        public LoginData From { get; set; }
        public LoginData To { get; set; }
        private string _ofrom, _oto;
        private int _maxsteps, _currentstep;
        private bool IsRunning { get; set; }

        private string WorkspacePath = "B:\\tmp\tfsmigration";

        public string OutputFrom
        {
            get { return _ofrom; }
            set
            {
                _ofrom = value;
                InvokePropertyChanged();
            }
        }
        public string OutputTo
        {
            get { return _oto; }
            set
            {
                _oto = value;
                InvokePropertyChanged();
            }
        }
        private string _progresscolor = "green";
        public string ProgressColor
        {
            get
            {
                return _progresscolor;
            }
            set
            {
                _progresscolor = value;
                InvokePropertyChanged();
            }
        }

        public int MaxStep
        {
            get { return _maxsteps; }
            set
            {
                _maxsteps = value;
                InvokePropertyChanged();
            }
        }
        public int CurrentStep
        {
            get { return _currentstep; }
            set
            {
                _currentstep = value;
                InvokePropertyChanged();
            }
        }


        public ICommand OnStart
        {
            get
            {
                return new RelayCommand(_ => { Task.Run(Worker); },_=> !IsRunning);
            }
        }
        public MigrateViewModel(LoginData from, LoginData to,string workspacepath):base()
        {
            From = from; To = to;
            this.WorkspacePath = workspacepath;
            ShowDialog();
        }

        public void AppendTo(string msg)
        {
            OutputTo += $"{(OutputTo == "" ? "" : "\n")}[{DateTime.Now.ToLongTimeString()}]: {msg}";
        }
        public void AppendFrom(string msg)
        {
            OutputFrom += $"{(OutputFrom == "" ? "" : "\n")}[{DateTime.Now.ToLongTimeString()}]: {msg}";
        }


        public async Task Worker()
        {
            try
            {
                MaxStep = 8;
                CurrentStep = 0;
                IsRunning = true;
                ProgressColor = "green";
                AppendFrom("Connecting to TFS");
                TfsTeamProjectCollection tfs1 = From.TFS;
                CurrentStep++;
                AppendFrom("Connected...");
                AppendFrom("Gathering information...");
                var vcs1 = tfs1.GetService<VersionControlServer>();
                CurrentStep++;
                OutputFrom += "Done!";
                AppendTo("Connecting to TFS");
                TfsTeamProjectCollection tfs2 = To.TFS;
                CurrentStep++;
                AppendTo("Connected...");
                AppendTo("Gathering information...");
                var vcs2 = tfs2.GetService<VersionControlServer>();
                AppendTo("Preparing workspace...");
                AppendFrom("Preparing workspace...");
                if (!Directory.Exists(this.WorkspacePath))
                {
                    Directory.CreateDirectory(WorkspacePath);
                }
                if (Directory.EnumerateFileSystemEntries(WorkspacePath).Count() != 0)
                {
                    MessageBox.Show("The selected folder is not empty!\n Please clear this folder before migrating!", "Folder not empty", MessageBoxButton.OK, MessageBoxImage.Warning);
                    OutputTo = "Failed: Workspace folder is not empty!";
                    OutputFrom = "Failed: Workspace folder is not empty!";
                    CurrentStep = 1;
                    MaxStep = 1;
                    ProgressColor = "red";
                    return;
                }
                if (!Directory.Exists(Path.Combine(this.WorkspacePath, "to")))
                {
                    Directory.CreateDirectory(Path.Combine(this.WorkspacePath, "to"));
                }
                if (!Directory.Exists(Path.Combine(this.WorkspacePath, "from")))
                {
                    Directory.CreateDirectory(Path.Combine(this.WorkspacePath, "to"));
                }
                CurrentStep++;
                Workspace workspaceto = vcs2.CreateWorkspace("Migration" + DateTime.Now.Ticks, tfs2.AuthorizedIdentity.UniqueName, $"Workspace which is used during the migration of {tfs1.Uri.ToString()}/{From.Project} to {tfs2.Uri.ToString()}/{To.Project}");
                workspaceto.Map(To.Project, Path.Combine(this.WorkspacePath, "to"));
                OutputTo += "Done!";
                CurrentStep++;
                AppendTo("Getting workspace from remote server...");
                workspaceto.Get();
                OutputTo += "Done!";
                CurrentStep++;
                //
                Workspace workspacefrom = vcs1.CreateWorkspace("Migration" + DateTime.Now.Ticks, tfs1.AuthorizedIdentity.UniqueName, $"Workspace which is used during the migration of {tfs1.Uri.ToString()}/{From.Project} to {tfs2.Uri.ToString()}/{To.Project}");
                workspacefrom.Map(From.Project, Path.Combine(this.WorkspacePath, "from"));
                OutputFrom += "Done!";
                CurrentStep++;
                AppendFrom("Getting workspace from remote server...");
                workspacefrom.Get();
                OutputTo += "Done!";
                CurrentStep++;
                //Createing all directories
                var fromdir = Path.Combine(this.WorkspacePath, "from");
                var todir = Path.Combine(this.WorkspacePath, "to");
                MaxStep = DirectoryUtils.CountFiles(fromdir);
                CurrentStep = 0;

                DirectoryUtils.CloneDirectory(fromdir, todir, (file) => { CurrentStep++; AppendFrom($"Cloning {file}"); });
                AppendFrom("Done!");
                workspaceto.PendAdd(todir, true);
                workspaceto.CheckIn(workspaceto.GetPendingChanges(), $"Migrating from {From.Project} => {To.Project} at {DateTime.Now.ToString()}");
                IsRunning = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                IsRunning = false;
                CurrentStep = 1;
                MaxStep = 1;
                ProgressColor = "red";
            }
        }
    }
}
