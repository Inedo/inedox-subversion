using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Inedo.Agents;
using Inedo.BuildMaster;
using Inedo.BuildMaster.Extensibility.Providers;
using Inedo.BuildMaster.Extensibility.Providers.SourceControl;
using Inedo.BuildMaster.Files;
using Inedo.BuildMaster.Web;
using Inedo.IO;
using Inedo.Serialization;

namespace Inedo.BuildMasterExtensions.Subversion
{
    [DisplayName("Subversion")]
    [Description("Includes built-in support for SVN v1.8 and earlier.")]
    [CustomEditor(typeof(Subversion15ProviderEditor))]
    public sealed class Subversion15Provider : SourceControlProviderBase, ILocalWorkspaceProvider, IMultipleRepositoryProvider, IBranchingProvider, IRevisionProvider, IClientCommandProvider
    {
        [Persistent]
        public string RepositoryRoot { get; set; }
        [Persistent]
        public string Username { get; set; }
        [Persistent]
        public string Password { get; set; }
        [Persistent]
        public bool UseSSH { get; set; }
        [Persistent]
        public string PrivateKeyPath { get; set; }
        [Persistent]
        public string ExePath { get; set; }

        public override char DirectorySeparator { get { return '/'; } }
        public bool RequiresComment { get { return true; } }

        public new IFileOperationsExecuter Agent => base.Agent.GetService<IFileOperationsExecuter>();
        internal string SafePrivateKeyPath => this.PrivateKeyPath.Replace(@"\", "/");
        internal bool EffectivelyUsesRepositories 
        { 
            get { return this.Repositories != null && this.Repositories.Length > 0 && !string.IsNullOrEmpty(Repositories[0].RemoteUrl); } 
        }

        private string SvnExePath
        {
            get
            {
                return Util.CoalesceStr(
                    this.ExePath,
                    this.Agent.CombinePath(PathEx.GetDirectoryName(typeof(Subversion15Provider).Assembly.Location), "Resources", "svn.exe")
                );
            }
        }
        /// <summary>
        /// Gets the path to the embedded plink.exe in Linux format (/path/to/_WEBTEMP/Subversion/plink.exe) 
        /// because SVN will not accept backslashes in its SSH configuration
        /// </summary>
        internal string PlinkExePath
        {
            get
            {
                return this.Agent
                    .CombinePath(PathEx.GetDirectoryName(typeof(Subversion15Provider).Assembly.Location), "Resources", "plink.exe")
                    .Replace(@"\", "/");
            }
        }

        bool IMultipleRepositoryProvider.DisplayEditor => true;
        string IMultipleRepositoryProvider.LabelText => "Relative path:";

        [Persistent(CustomSerializer = typeof(SourceRepositorySerializer))]
        public SourceRepository[] Repositories { get; set; }

        public override void GetLatest(string sourcePath, string targetPath)
        {
            var context = (SvnSourceControlContext)this.CreateSourceControlContext(sourcePath);
            this.GetLatest(context, targetPath);
        }

        private void GetLatest(SvnSourceControlContext context, string targetDirectory)
        {
            this.EnsureLocalWorkspace(context);
            this.UpdateLocalWorkspace(context);
            this.ExportFiles(context, targetDirectory);
        }

        public override DirectoryEntryInfo GetDirectoryEntryInfo(string sourcePath)
        {
            var context = (SvnSourceControlContext)this.CreateSourceControlContext(sourcePath);
            return this.GetDirectoryEntryInfo(context);
        }

        private DirectoryEntryInfo GetDirectoryEntryInfo(SvnSourceControlContext context)
        {
            if (this.EffectivelyUsesRepositories && context.PathSpecifiedRepositoryName == null)
            {
                return new DirectoryEntryInfo(
                    string.Empty, 
                    string.Empty, 
                    this.Repositories.Select(r => new DirectoryEntryInfo(r.Name, r.Name)), 
                    null
                );
            }
            else
            {
                var results = this.ExecuteSvn("list", context.SvnTargetUrl);
                var paths = results.Output;

                return new DirectoryEntryInfo(
                    context.LastSubDirectoryName,
                    context.RepositoryRelativePath,
                    paths.Select(p => context.CreateRelativeSystemEntryInfo(p))
                );
            }
        }

        public override byte[] GetFileContents(string filePath)
        {
            var context = (SvnSourceControlContext)this.CreateSourceControlContext(filePath);
            this.EnsureLocalWorkspace(context);
            this.UpdateLocalWorkspace(context);
            return this.Agent.ReadFileBytes(context.AbsoluteDiskPath);
        }

        public override bool IsAvailable() => true;
        
        public override void ValidateConnection()
        {
            try
            {
                if (!this.EffectivelyUsesRepositories)
                {
                    var context = (SvnSourceControlContext)this.CreateSourceControlContext("/");
                    this.GetCurrentRevisionInternal(context);
                }
                else
                {
                    foreach (var repo in this.Repositories)
                    {
                        var context = (SvnSourceControlContext)this.CreateSourceControlContext(repo.RemoteUrl);
                        this.GetCurrentRevisionInternal(context);
                    }
                }
            }
            catch (FileLoadException fex)
            {
                throw new ConnectionException(
                    "There was an error loading a required library. "
                    + "This error usually occurs when the the MICROSOFT VISUAL C++ 2005 SP1 RUNTIME LIBRARIES have not been installed. "
                    + "The specific message was: "
                    + fex.Message, fex);
            }
            catch (Exception ex)
            {
                throw new ConnectionException(ex.Message, ex);
            }
        }

        public void Branch(string sourcePath, string toPath, string comment)
        {
            var sourceContext = (SvnSourceControlContext)this.CreateSourceControlContext(sourcePath);
            var targetContext = (SvnSourceControlContext)this.CreateSourceControlContext(toPath);
            
            this.ExecuteSvn("copy", sourceContext.SvnTargetUrl, targetContext.SvnTargetUrl, "-m", comment);
        }
        public object GetCurrentRevision(string path)
        {
            var context = (SvnSourceControlContext)this.CreateSourceControlContext(path);
            return this.GetCurrentRevisionInternal(context);
        }

        public override string ToString()
        {
            string repRoot = this.RepositoryRoot;
            if (repRoot != null && repRoot.Length > 20) 
                repRoot = repRoot.Substring(0, 17) + "...";

            return "SVN at "
                + repRoot
                + Util.ConcatNE(" (Username: ", Username, ")");
        }

        public void ExecuteClientCommand(string commandName, string arguments)
        {
            this.ExecuteSvn(commandName, new SvnArguments(this, arguments) { QuoteArguments = false });
        }

        public IEnumerable<ClientCommand> GetAvailableCommands()
        {
            using (var stream = typeof(Subversion15Provider).Assembly.GetManifestResourceStream("Inedo.BuildMasterExtensions.Subversion.SvnCommands.txt"))
            using (var reader = new StreamReader(stream))
            {
                var line = reader.ReadLine();
                while (line != null)
                {
                    var commandInfo = line.Split(new[] { '\t' }, 2, StringSplitOptions.RemoveEmptyEntries);
                    yield return new ClientCommand(commandInfo[0].Trim(), commandInfo[1].Trim());

                    line = reader.ReadLine();
                }
            }
        }

        public string GetClientCommandHelp(string commandName)
        {
            try
            {
                return this.SvnHelp(commandName);
            }
            catch (Exception e)
            {
                return "Help not available for the \"" + commandName + "\" command. The specific error message was: " + e.Message;
            }
        }

        public string GetClientCommandPreview() => string.Format("{{command}} {0} ", new SvnArguments(this) { ObscurePassword = true });

        public bool SupportsCommandHelp => true;

        private int GetCurrentRevisionInternal(SvnSourceControlContext context)
        {
            var results = this.ExecuteSvn("info", context.SvnTargetUrl, "--xml");

            var doc = XDocument.Parse(string.Join(Environment.NewLine, results.Output));
            int revision = (int?)doc.Root.Element("entry").Element("commit").Attribute("revision") ?? 0;
            
            return revision;
        }

        private string SvnHelp(string command)
        {
            var results = this.ExecuteSvn("help", command);
            return string.Join(Environment.NewLine, results.Output);
        }

        private ProcessResults ExecuteSvn(string commandName, params string[] args)
        {
            return this.ExecuteSvn(commandName, new SvnArguments(this, args));
        }

        private ProcessResults ExecuteSvn(string commandName, SvnArguments args)
        {
            return this.ExecuteSvn(commandName, args, true);
        }

        private ProcessResults ExecuteSvn(string commandName, SvnArguments args, bool logErrors)
        {
            var results = this.ExecuteCommandLine(
                new RemoteProcessStartInfo 
                { 
                    FileName = this.SvnExePath, 
                    Arguments = commandName + " " + args 
                }
            );

            if (logErrors)
            {
                foreach (var line in results.Error)
                    this.LogError(line);
            }    

            if (results.ExitCode != 0)
                throw new InvalidOperationException(string.Join(Environment.NewLine, results.Error));
            else 
                return results;
        }

        public override SourceControlContext CreateSourceControlContext(object contextData)
        {
            return new SvnSourceControlContext(this, (string)contextData);
        }

        public void DeleteWorkspace(SourceControlContext context)
        {
            this.LogDebug("Deleting workspace at: " + context.WorkspaceDiskPath);
            this.Agent.ClearDirectory(context.WorkspaceDiskPath);
        }

        public void EnsureLocalWorkspace(SourceControlContext context)
        {
            this.LogDebug("Ensuring local workspace at: " + context.WorkspaceDiskPath);
            if (!this.Agent.DirectoryExists(context.WorkspaceDiskPath))
            {
                this.LogDebug("Workspace does not exist, creating...");
                this.Agent.CreateDirectory(context.WorkspaceDiskPath);
            }
            else
            {
                this.LogDebug("Workspace already exists.");
            }
        }

        public void ExportFiles(SourceControlContext context, string targetDirectory)
        {
            this.ExportFiles((SvnSourceControlContext)context, targetDirectory);
        }

        private void ExportFiles(SvnSourceControlContext context, string targetDirectory)
        {
            this.ExecuteSvn("export", context.WorkspaceDiskPath, targetDirectory, "--force");
        }

        public string GetWorkspaceDiskPath(SourceControlContext context)
        {
            return context.WorkspaceDiskPath;
        }

        public void UpdateLocalWorkspace(SourceControlContext context)
        {
            this.UpdateLocalWorkspace((SvnSourceControlContext)context);
        }

        private void UpdateLocalWorkspace(SvnSourceControlContext context)
        {
            this.LogDebug("Updating local workspace...");
            string remoteUrl = null;
            try
            {
                var results = this.ExecuteSvn("info", new SvnArguments(this, context.WorkspaceDiskPath, "--xml"), false);
                var xdoc = XDocument.Parse(string.Join(Environment.NewLine, results.Output));
                remoteUrl = (string)xdoc.Root.Element("entry").Element("url");
                this.LogDebug("Remote URL found: " + remoteUrl);
            }
            catch (InvalidOperationException)
            {
                /* SVN will return exit code 1 if you use INFO on an uninitialized 
                 * directory, this is the easiest way to ignore that and
                 * just perform a CHECKOUT instead of an UPDATE. */
                this.LogDebug("The local workspace was uninitialized.");
            }

            if (remoteUrl == null)
            {
                this.LogDebug("Remote repository cannot be found, performing checkout operation...");
                this.ExecuteSvn("checkout", context.SvnTargetUrl, context.WorkspaceDiskPath);
            }
            else if (!context.TargetUrlMatchesRemoteUrl(remoteUrl))
            {
                this.LogDebug(
                    "Remote repository URL \"{0}\" does not match the current SVN target URL \"{1}\", clearing and performing checkout operation...",
                    remoteUrl ?? "(null)",
                    context.SvnTargetUrl
                );

                this.DeleteWorkspace(context);
                this.EnsureLocalWorkspace(context);
                this.ExecuteSvn("checkout", context.SvnTargetUrl, context.WorkspaceDiskPath);
            }
            else
            {
                this.LogDebug("Updating local repository...");
                this.ExecuteSvn("update", context.WorkspaceDiskPath);
            }
        }
    }
}
