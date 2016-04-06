using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.Framework.Client;
using Microsoft.TeamFoundation.VersionControl.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using TFSMigrationTool.Command;
using TFSMigrationTool.Models;

namespace TFSMigrationTool.ViewModels
{
    /// <summary>
    /// Implementation of http://blog.jessehouwing.nl/2015/11/work-around-now-commercial-features-of.html
    /// </summary>
    public class MetadataChangerViewModel : AbstractViewModel
    {
        protected override string ViewName { get { return "MetadataChanger"; } }
        protected LoginData Login { get; set; }
        public MetadataChangerViewModel(LoginData data):base(){
            Login = data;
            Task.Run(Worker);
            ShowDialog();
        }

        private bool _isrunning = false;

        public bool IsRunning
        {
            get
            {
                return _isrunning;
            }
            set
            {
                _isrunning = value;
                InvokePropertyChanged();
            }
        }
        private int _maxsteps=5, _currentstep=0;

        public int MaxSteps
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

        private string _output;
        public string Output
        {
            get
            {
                return _output;
            }
            set
            {
                _output = value;
                InvokePropertyChanged();
            }
        }
        public ICommand OnClose
        {
            get
            {
                return new RelayCommand(a => Close(), a => !IsRunning);
            }
        }
        
        private void AppendLine(string message)
        {
            Output += $"{(string.IsNullOrWhiteSpace(_output) ? "" : "\n")}[{DateTime.Now}]> {message}";
        }
        public async Task Worker()
        {
            MaxSteps = 2;
            CurrentStep = 0;
            IsRunning = true;
            AppendLine("Connecting to TFS..");

            TfsTeamProjectCollection configserver = Login.TFS;
            CurrentStep++;
            //Connect
            AppendLine("Connected");
            var versioncontrolserver = configserver.GetService<VersionControlServer>();
            var changes = versioncontrolserver.QueryHistory(Login.Project, RecursionType.Full);
            CurrentStep++;
            //Read all projects from default
            AppendLine("Changes");
            MaxSteps += changes.Count();
            foreach (var change in changes)
            {
                AppendLine("Found Change:");
                string changestr = 
$@"{(string.IsNullOrWhiteSpace(change.Comment) ? "No comment provided" : change.Comment)}
--
Original checkin:
 * Who: {change.Committer} ({change.CommitterDisplayName})
 * When: {change.CreationDate}
 * In: {change.VersionControlServer.TeamProjectCollection.Uri}
 * ID: {change.ChangesetId}";
                Output += "\n" + changestr.Replace("\n * ","\n\t * ") + "\n";
                if(!change.Comment.Contains("Original checkin:"))
                {
                    //Change comment
                    AppendLine("Updating comment...");
                    change.Comment = changestr;
                    change.Update();
                    Output += "Done!";
                }
                CurrentStep++;
            }
            Output += $"\n\n----DONE----";
            IsRunning = false;
        }
    }
}
