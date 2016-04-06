using Microsoft.TeamFoundation.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TFSMigrationTool.Command;
using TFSMigrationTool.Models;

namespace TFSMigrationTool.ViewModels
{
    public class MainWindowViewModel : AbstractViewModel
    {
        protected override string ViewName
        {
            get
            {
                return "MainWindow";
            }
        }

        public MainWindowViewModel() : base() { }

        public string SourceTFSString
        {
            get
            {
                if (SourceTFS == null) return "No server selected";
                return $"{SourceTFS.AuthorizedIdentity.DisplayName}@{SourceTFS.Uri.ToString()}/{SourceTFSProject}";
            }
        }
        public string TargetTFSString
        {
            get
            {
                if (TargetTFS == null) return "No server selected";
                return $"{TargetTFS.AuthorizedIdentity.DisplayName}@{TargetTFS.Uri.ToString()}/{TargetTFSProject}";
            }
        }
        public string SourceTFSProject { get; set; }

        public TfsTeamProjectCollection SourceTFS
        {
            get; set;
        } = null;

        public string TargetTFSProject { get; set; }
        public TfsTeamProjectCollection TargetTFS
        {
            get; set;
        } = null;

        public ICommand OnManage
        {
            get
            {
                return new RelayCommand(id => {
                    TeamProjectPicker picker = new TeamProjectPicker(TeamProjectPickerMode.SingleProject, false);
                    picker.Text = $"Select a {(id as string)} TFS server";
                    if (picker.ShowDialog() == System.Windows.Forms.DialogResult.OK && picker.SelectedProjects.Length > 0)
                    {

                        if((id as string) == "Source") { 
                            SourceTFS = picker.SelectedTeamProjectCollection;
                            SourceTFSProject = picker.SelectedProjects[0].Name;
                            InvokePropertyChanged("SourceTFSString");
                        }
                        else
                        {
                            TargetTFS = picker.SelectedTeamProjectCollection;
                            TargetTFSProject = picker.SelectedProjects[0].Name;
                            InvokePropertyChanged("TargetTFSString");
                        }
                    }
                });
            }
        }
        public ICommand OnStart
        {
            get
            {
                return new RelayCommand(_ =>
                {
                    LoginData data = new LoginData()
                    {
                        TFS = SourceTFS,
                        Project = "$/" + SourceTFSProject
                    };
                    MetadataChangerViewModel rvm = new MetadataChangerViewModel(data);
                },
                _=>SourceTFS != null);
            }
        }
        public ICommand OnMigrate
        {
            get
            {
                return new RelayCommand(_ =>
                {
                    LoginData from = new LoginData()
                    {
                        Project = "$/" + SourceTFSProject,
                        TFS = SourceTFS
                    };
                    LoginData to = new LoginData()
                    {
                        Project = "$/" + TargetTFSProject,
                        TFS = TargetTFS
                    };
                    MigrateViewModel mvm = new MigrateViewModel(from, to, Workspace);

                },
                _ => SourceTFS != null && TargetTFS != null && !string.IsNullOrWhiteSpace(Workspace));
            }
        }
        public ICommand OnMigrateHistory
        {
            get
            {
                return new RelayCommand(_ =>
                {
                    LoginData from = new LoginData()
                    {
                        Project = "$/" + SourceTFSProject,
                        TFS = SourceTFS
                    };
                    LoginData to = new LoginData()
                    {
                        Project = "$/" + TargetTFSProject,
                        TFS = TargetTFS
                    };
                    MigrateHistoryViewModel mvm = new MigrateHistoryViewModel(from, to, Workspace);

                },
                _ => SourceTFS != null && TargetTFS != null && !string.IsNullOrWhiteSpace(Workspace));
            }
        }
        public ICommand OnSelectWorkSpace
        {
            get
            {
                return new RelayCommand(_ => {
                    System.Windows.Forms.FolderBrowserDialog d = new System.Windows.Forms.FolderBrowserDialog();
                    d.Description = "Select a workspace directory";
                    d.ShowNewFolderButton = true;
                    if(d.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    {
                        Workspace = d.SelectedPath;
                    }
                });
            }
        }

        private string _workspace;
        public string Workspace
        {
            get { return _workspace; }
            set
            {
                _workspace = value;
                InvokePropertyChanged();
            }
        }


    }
}
