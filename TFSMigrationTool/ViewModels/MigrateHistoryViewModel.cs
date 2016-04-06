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
    /// Migrates code with its history, sorted by the commit date/time
    /// Note: not all commits will be the same, cause some changes are triggered earlier (due to branch detection)
    /// </summary>
    public class MigrateHistoryViewModel : AbstractViewModel
    {
        protected override string ViewName
        {
            get
            {
                return "MigrateHistory";
            }
        }
        public LoginData From { get; set; }
        public LoginData To { get; set; }
        private string _ofrom, _oto;

        private Dictionary<string, string> branching { get; set; }
        private Dictionary<string, bool> branchCreationStatus { get; set; } = new Dictionary<string, bool>();
        private int _maxsteps, _currentstep;
        private bool IsRunning { get; set; }

        private string WorkspacePath = "";

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
        private int _maxstepstwo;
        private int _currentsteptwo;

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
        public int MaxStepTwo
        {
            get { return _maxstepstwo; }
            set
            {
                _maxstepstwo = value;
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
        public int CurrentStepTwo
        {
            get { return _currentsteptwo; }
            set
            {
                _currentsteptwo = value;
                InvokePropertyChanged();
            }
        }


        public ICommand OnStart
        {
            get
            {
                return new RelayCommand(_ => { Task.Run(Worker); }, _ => !IsRunning);
            }
        }
        public MigrateHistoryViewModel(LoginData from, LoginData to, string workspacepath) : base()
        {
            From = from; To = to;
            this.WorkspacePath = workspacepath;
            ShowDialog();
        }

        //public void AppendTo(string msg)
        //{
        //    OutputFrom += $"\n\t[{DateTime.Now.ToLongTimeString()}]: {msg}";
        //}
        public void AppendFrom(string msg)
        {
            OutputFrom += $"{(OutputFrom == "" ? "" : "\n")}[{DateTime.Now.ToLongTimeString()}]: {msg}";
        }

        /// <summary>
        /// The Migration Worker
        /// </summary>
        /// <returns></returns>
        public async Task Worker()
        {
            IsRunning = true;
            try
            {

                #region "Preparing the migration"
                AppendFrom($"{ From.Project} -> { To.Project}");
                MaxStep = 3;
                AppendFrom("Preparing Source...");
                TfsTeamProjectCollection tfs1 = From.TFS;
                var vcs1 = tfs1.GetService<VersionControlServer>();
                AppendFrom("Preparing Target...");
                TfsTeamProjectCollection tfs2 = To.TFS;
                var vcs2 = tfs2.GetService<VersionControlServer>();
                CurrentStep++;
                AppendFrom("Waiting for 'Target' workspace to be completed...");

                AppendFrom("Preparing local Target workspace...");
                if (!Directory.Exists(this.WorkspacePath))
                {
                    Directory.CreateDirectory(WorkspacePath);
                }
                if (Directory.EnumerateFileSystemEntries(WorkspacePath).Count() != 0)
                {
                    throw new Exception("The selected folder is not empty!\n Please clear this folder before migrating!");
                }
                if (!Directory.Exists(Path.Combine(this.WorkspacePath, "to")))
                {
                    Directory.CreateDirectory(Path.Combine(this.WorkspacePath, "to"));
                }
                CurrentStep++;
                OutputFrom += "Done!";

                AppendFrom("Loading Target workspace...");

                Workspace wsTo = null;
                try
                {
                    wsTo = vcs2.GetWorkspace("TFSMigration", tfs2.AuthorizedIdentity.UniqueName);
                    vcs2.DeleteWorkspace("TFSMigration", tfs2.AuthorizedIdentity.UniqueName);
                }
                catch
                {
                }
                wsTo = vcs2.CreateWorkspace("TFSMigration", tfs2.AuthorizedIdentity.UniqueName, $"Workspace which is used during the migration of {tfs1.Uri.ToString()}/{From.Project} to {tfs2.Uri.ToString()}/{To.Project}");
                wsTo.Map(To.Project, Path.Combine(this.WorkspacePath, "to"));
                CurrentStep++;
                OutputFrom += "Done!";
                AppendFrom("Retrieving History...");
                //var history = vcs1.QueryHistory(From.Project, RecursionType.Full);
                var history = vcs1.QueryHistory(From.Project, RecursionType.Full, Int32.MaxValue);
                int historyCount = history.Count();
                MaxStep = historyCount;
                OutputFrom += $" Found {historyCount} changes=> Done";
                string dirFrom = Path.Combine(this.WorkspacePath, "to");
                AppendFrom("Finding all branches with their parents (child->parent)...");
                branching = GetBranchHierarchy(vcs1);
                MaxStepTwo = branching.Count;
                CurrentStepTwo = 0;
                bool hadLastChange = false;
                var lastId = vcs1.GetLatestChangesetId();
                foreach (var kv in branching)
                {
                    AppendFrom($"\t{kv.Key}->{kv.Value}");
                    branchCreationStatus.Add(kv.Key, false);
                    CurrentStepTwo++;
                }
                AppendFrom("Done with branch discovery!");
                var historyWithNow = history.OrderBy(a => a.CreationDate).ToList();
                //historyWithNow.Add(vcs1.GetChangeset(lastId));
                #endregion
                #region "Migration"
                foreach (var change in historyWithNow)
                {
                    OutputFrom += $"\n------new changeset: {change.ChangesetId}------";
                    if (hadLastChange && change.ChangesetId == lastId)
                        continue;
                    hadLastChange = change.ChangesetId == lastId;
                    string newMessage = change.Comment;
                    if (!newMessage.ToLower().Contains("original checkin:"))
                    { //if the project was migrated before: dont rename again!
                        newMessage =
    $@"{(string.IsNullOrWhiteSpace(change.Comment) ? "No comment provided" : change.Comment)}
--
Original checkin:
    * Who: {change.Committer} ({change.CommitterDisplayName})
    * When: {change.CreationDate}
    * In: {change.VersionControlServer.TeamProjectCollection.Uri}
    * ID: {change.ChangesetId}";
                    }
                    int tmp = CurrentStep;
                    //var changeSet = vcs1.GetChangeset(change.ChangesetId,true,true,true).Changes;
                    var changeSet = vcs1.GetChangesForChangeset(change.ChangesetId, true, int.MaxValue, null, null, true);
                    MaxStepTwo = changeSet.Count();
                    CurrentStepTwo = 0;
                    var version = new ChangesetVersionSpec(change.ChangesetId);
                    foreach (var item in changeSet)
                    {
                        try
                        {
                            Item serverItem = item.Item;
                            if (!item.ChangeType.HasFlag(ChangeType.Delete))
                            {
                                serverItem = vcs1.GetItem(item.Item.ServerItem, version);
                            }
                            string d = DirectoryUtils.GetLocalPath(serverItem.ServerItem, From.Project, dirFrom);
                            string remotepath = DirectoryUtils.CreateTFSPathFromSource(To.Project, From.Project, serverItem.ServerItem);
                            switch (item.Item.ItemType)
                            {
                                case ItemType.File:
                                    HandleFile(item, serverItem, wsTo, d, remotepath);
                                    break;
                                case ItemType.Folder:
                                    HandleFolder(item, serverItem, wsTo, d, version, vcs1, vcs2, remotepath, newMessage);
                                    break;
                                default:
                                    break;
                            }
                        }
                        catch (VersionControlException vce)
                        {
                            AppendFrom($"VCE: Item {item.Item.ServerItem} {item.ChangeType}>> {vce.Message}");
                        }
                        catch (Exception e)
                        {
                            AppendFrom($"ERR: Item {item.Item.ServerItem}>> {e.Message}");
                        }
                        CurrentStepTwo++;
                    }
                    try
                    {
                        var changes = wsTo.GetPendingChanges();
                        if (changes.Length != 0) { wsTo.CheckIn(changes, newMessage); AppendFrom($"Checked in {changes.Length} changes"); }
                        else { AppendFrom("No changes to checkin!"); }
                    }
                    catch (CheckinException ce)
                    {
                        if (ce.Conflicts != null && ce.Conflicts.Length != 0)
                        {
                            //conflicts to resolve => use Client side over srv
                            Conflict[] conflicts = wsTo.QueryConflicts(new[] { To.Project }, true);
                            foreach (var conflict in conflicts)
                            {
                                if (conflict.YourItemType.HasFlag(ItemType.Folder))
                                    conflict.Resolution = Resolution.AcceptMerge;//allways merge folders
                                else
                                    conflict.Resolution = Resolution.AcceptYours;//Accept mine
                                wsTo.ResolveConflict(conflict);
                            }
                        }
                        var changes = wsTo.GetPendingChanges();
                        if (changes.Length != 0) { wsTo.CheckIn(changes, newMessage); AppendFrom($"Checked in {changes.Length} changes after a automerge"); }
                    }
                    CurrentStep++;
                }
                AppendFrom("Done");
                CurrentStep = 1; MaxStep = 1;
            }
            catch (Exception e)
            {
                MessageBox.Show($"{e.Message}\n{e.StackTrace}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                OutputFrom = $"Failed: {e.Message}\n\t {e.StackTrace}";
                CurrentStep = 1;
                MaxStep = 1;
                ProgressColor = "red";
            }
            IsRunning = false;
            #endregion
        }
        /// <summary>
        /// Manages the migration of a folder
        /// </summary>
        /// <param name="change">the changeitem from the changeset</param>
        /// <param name="item">The server item (file/folder)</param>
        /// <param name="ws">The workspace</param>
        /// <param name="localPath">the localpath on this machine of the item</param>
        /// <param name="changesetversion">the version of the changeset</param>
        /// <param name="serverFrom">the versioncontrolserver of the source TFS</param>
        /// <param name="serverTo">The VersionControlServer of the target TFS</param>
        /// <param name="remotepath">the remote TARGET path of the item</param>
        /// <param name="message">The Commit message of this commit group</param>
        private void HandleFolder(Change change, Item item, Workspace ws, string localPath, ChangesetVersionSpec changesetversion, VersionControlServer serverFrom, VersionControlServer serverTo, string remotepath, string message)
        {
            AppendFrom($"Folder>{item.ServerItem} Changetype: {change.ChangeType.ToString()}");
            AppendFrom($"\tNew path: {remotepath}");
            //If it is a branch, make it a branch
            if (IsBranch(item.ServerItem))
            {
                AppendFrom($"\t->{item.ServerItem} is a branch...");
                //if there is a Parent of this branch, branch of it
                if (!string.IsNullOrWhiteSpace(branching[item.ServerItem]))
                {
                    if (Directory.Exists(localPath))
                    {
                        //if a directory exists, remove the child tree so we can branch of it
                        Directory.Delete(localPath, true);
                        ws.PendDelete(localPath, RecursionType.Full);
                        AppendFrom($"\t-->{item.ServerItem} and it's children are removed for branching!");
                    }
                    //Commit all the changes before branching of
                    var changes = ws.GetPendingChanges();
                    if (changes.Length != 0) ws.CheckIn(changes, $"Branching of to {remotepath} & " + message);

                    string parentbranch = DirectoryUtils.CreateTFSPathFromSource(To.Project, From.Project, branching[item.ServerItem]);//get the parent target path
                    serverTo.CreateBranch(parentbranch, remotepath, VersionSpec.Latest, null, $"Branching of to {remotepath}", null, null, null);//Branch of it
                }
                //Else create a new folder and convert it to a branch
                else {
                //TODO is this Block needed?
                    if (Directory.Exists(localPath))
                    {
                        //if a directory exists, remove the whole child tree!
                        Directory.Delete(localPath, true);
                        ws.PendDelete(localPath, RecursionType.Full);
                        AppendFrom($"\t-->{item.ServerItem} and it's children are removed for branching!");
                    }
                    Directory.CreateDirectory(localPath);
                    ws.PendAdd(localPath);
                //TODO END
                    //Checkin dir creation
                    var changes = ws.GetPendingChanges();
                    if (changes.Length != 0) ws.CheckIn(changes, $"Branching of to {remotepath} & " + message);

                    serverTo.CreateBranchObject(new BranchProperties(new ItemIdentifier(remotepath)));
                }
                branchCreationStatus[item.ServerItem] = true;//mark this branch as created
            }
            //If it is added
            else if (change.ChangeType.HasFlag(ChangeType.Add))
            {
                if (!Directory.Exists(localPath))
                {
                    Directory.CreateDirectory(localPath);
                    ws.PendAdd(localPath);
                    AppendFrom($"\t->{remotepath} is added");
                }
                else
                {
                    AppendFrom($"\t->{remotepath} allready exists!");
                }
            }
            //if the item was removed
            else if (change.ChangeType.HasFlag(ChangeType.Delete))
            {
                //Recusive Delete folder if exists
                if (Directory.Exists(localPath))
                {
                    //if a directory is removed, remove the whole child tree!
                    Directory.Delete(localPath, true);
                    ws.PendDelete(localPath, RecursionType.Full);
                    AppendFrom($"\t->{remotepath} and it's children are removed!");
                }
            }
            //if the item was renamed/moved
            else if (change.ChangeType.HasFlag(ChangeType.Rename))
            {
                if (change.MergeSources != null && change.MergeSources.Count() != 0)//get the original name from the mergesource
                {
                    string oldSourceName = change.MergeSources[0].ServerItem;//the original name when the changetype is Rename
                    string oldname = DirectoryUtils.CreateTFSPathFromSource(To.Project, From.Project, oldSourceName);
                    ws.PendRename(oldname, remotepath);
                    AppendFrom($"\t->moving {oldname} to {remotepath}");
                }
            }
        }

        //isBranch if the branch is in the dictionary and is not created yet
        public bool IsBranch(string remotepath)
        {
            return branching.ContainsKey(remotepath) && branchCreationStatus.ContainsKey(remotepath) && !branchCreationStatus[remotepath];
        }
        //gets the hierarchy of the branches => Key = child, value = parent from a versioncontrol. It only takes the branches which are found in the From.Project property
        private Dictionary<string, string> GetBranchHierarchy(VersionControlServer vcs)
        {
            Dictionary<string, string> res = new Dictionary<string, string>();
            var branches = vcs.QueryRootBranchObjects(RecursionType.Full);
            Console.WriteLine($"---------");
            foreach (var branch in branches)
            {
                if (branch.Properties.RootItem.Item.Contains(From.Project))
                {
                    var parent = (branch.Properties.ParentBranch == null ? "" : branch.Properties.ParentBranch.Item);
                    res.Add(branch.Properties.RootItem.Item, parent);
                    Console.WriteLine($"\t{branch.Properties.RootItem.Item}->{parent}");
                }
                else {
                    Console.WriteLine($"\t{branch.Properties.RootItem.Item}");
                }
            }
            Console.WriteLine($"---------");
            return res;
        }
        /// <summary>
        /// Handles the migration of a File
        /// </summary>
        /// <param name="change">the changeitem from the changeset</param>
        /// <param name="item">The server item (file/folder)</param>
        /// <param name="ws">The workspace</param>
        /// <param name="localPath">the localpath on this machine of the item</param>
        /// <param name="remotepath">the remote TARGET path of the item</param>
        private void HandleFile(Change change, Item item, Workspace ws, string localPath, string remotepath)
        {
            AppendFrom($"File>{item.ServerItem} Changetype: {change.ChangeType.ToString()}");
            AppendFrom($"\tNew path: {remotepath}");
            if (change.ChangeType.HasFlag(ChangeType.Add) || change.ChangeType.HasFlag(ChangeType.Branch))
            {
                item.DownloadFile(localPath);
                ws.PendAdd(localPath);
                AppendFrom($"\t->{remotepath} is added/branched");
            }
            else if (change.ChangeType.HasFlag(ChangeType.Edit))
            {
                item.DownloadFile(localPath);
                ws.PendEdit(remotepath, RecursionType.Full);
                AppendFrom($"\t->{remotepath} is changed");
            }
            else if (change.ChangeType.HasFlag(ChangeType.Delete))
            {
                if (File.Exists(localPath))
                {
                    File.Delete(localPath);
                    ws.PendDelete(localPath);
                    AppendFrom($"\t->{remotepath} is removed");
                }
            }
            else if (change.ChangeType.HasFlag(ChangeType.Rename))
            {
                if (change.MergeSources != null && change.MergeSources.Count() != 0)
                {
                    string oldSourceName = change.MergeSources[0].ServerItem;
                    string oldname = DirectoryUtils.CreateTFSPathFromSource(To.Project, From.Project, oldSourceName);
                    ws.PendRename(oldname, remotepath);
                    AppendFrom($"\t->moving {oldname} to {remotepath}");
                }

            }
        }
    }
}
