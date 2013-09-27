using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using Inedo.BuildMaster;
using Inedo.BuildMaster.Extensibility.Agents;
using Inedo.BuildMaster.Extensibility.Providers;
using Inedo.BuildMaster.Extensibility.Providers.SourceControl;
using Inedo.BuildMaster.Files;
using Inedo.BuildMaster.Web;

namespace Inedo.BuildMasterExtensions.Subversion
{
    [ProviderProperties(
        "Subversion",
        "Includes built-in support for SVN v1.8 and earlier.")]
    [CustomEditor(typeof(Subversion15ProviderEditor))]
    public sealed class Subversion15Provider : SourceControlProviderBase, IMultipleRepositoryProvider<SubversionRepository>, IBranchingProvider, IRevisionProvider, IClientCommandProvider
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SubversionProviderBase"/> class.
        /// </summary>
        public Subversion15Provider()
        {
        }

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
        [Persistent]
        public bool UseUpdateInsteadOfExport { get; set; }
        [Persistent]
        public bool AlwaysUseCommandLine { get; set; }

        public override char DirectorySeparator { get { return '/'; } }
        public bool RequiresComment { get { return true; } }
        
        private new IFileOperationsExecuter Agent { get { return (IFileOperationsExecuter)base.Agent.GetService<IFileOperationsExecuter>(); } }
        private string SafePrivateKeyPath { get { return this.PrivateKeyPath.Replace(@"\", "/"); } }
        private bool EffectivelyUsesRepositories { get { return Repositories.Length > 0 && !string.IsNullOrEmpty(Repositories[0].RepositoryPath); } }

        private string SvnExePath
        {
            get
            {
                return Util.CoalesceStr(
                    this.ExePath,
                    this.Agent.CombinePath(this.Agent.GetBaseWorkingDirectory(), string.Format(@"ExtTemp\{0}\Resources\svn.exe", typeof(Subversion15Provider).Assembly.GetName().Name))
                );
            }
        }
        /// <summary>
        /// Gets the path to the embedded plink.exe in Linux format (/path/to/_WEBTEMP/Subversion/plink.exe) 
        /// because SVN will not accept backslashes in its SSH configuration
        /// </summary>
        private string PlinkExePath
        {
            get
            {
                return this.Agent
                    .CombinePath(this.Agent.GetBaseWorkingDirectory(), string.Format(@"ExtTemp\{0}\Resources\plink.exe", typeof(Subversion15Provider).Assembly.GetName().Name))
                    .Replace(@"\", "/");
            }
        }

        public override void GetLatest(string sourcePath, string targetPath)
        {
            var svnTarget = CreateSvnUriTarget(sourcePath);

            if (this.UseUpdateInsteadOfExport)
                this.UpdateLatest(svnTarget, targetPath);
            else
                SVN("export", svnTarget, targetPath, "--force");
        }
        public override DirectoryEntryInfo GetDirectoryEntryInfo(string sourcePath)
        {
            sourcePath = sourcePath ?? string.Empty;
            var splitPath = sourcePath.Split(new[] { this.DirectorySeparator }, StringSplitOptions.RemoveEmptyEntries);

            if (EffectivelyUsesRepositories)
            {
                if (splitPath.Length == 0)
                {
                    var subDirs = new DirectoryEntryInfo[Repositories.Length];
                    for (int i = 0; i < Repositories.Length; i++)
                        subDirs[i] = new DirectoryEntryInfo(
                            Repositories[i].RepositoryName,
                            Repositories[i].RepositoryName,
                            null,
                            null);
                    return new DirectoryEntryInfo(string.Empty, string.Empty, subDirs, null);
                }
            }

            var target = CreateSvnUriTarget(sourcePath);

            DirectoryEntryBuilder container;
            if (splitPath.Length > 0)
                container = new DirectoryEntryBuilder(splitPath[splitPath.Length - 1]);
            else
                container = new DirectoryEntryBuilder(string.Empty);

            string prependPath = string.Empty;
            if (splitPath.Length > 1)
                prependPath = string.Join("/", splitPath, 0, splitPath.Length - 1);

            this.GetDirectoryEntryInfo(target, container);

            return container.ToDirectoryEntryInfo(this.DirectorySeparator.ToString(), prependPath, true);
        }
        public override byte[] GetFileContents(string filePath)
        {
            var target = CreateSvnUriTarget(filePath);
            return this.GetFileContentsInternal(target);
        }
        public override bool IsAvailable()
        {
            return true;
        }
        public override void ValidateConnection()
        {
            try
            {
                this.GetCurrentRevisionInternal(CombineSvnPaths(this.RepositoryRoot, this.Repositories[0].RepositoryPath));
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
            var svnSourcePath = CreateSvnUriTarget(sourcePath);
            var svnToPath = CreateSvnUriTarget(toPath);
            
            SVN("copy", svnSourcePath, svnToPath, "-m", comment);
        }
        public byte[] GetCurrentRevision(string path)
        {
            var svnPath = CreateSvnUriTarget(path);
            return this.GetCurrentRevisionInternal(svnPath);
        }
        public override string ToString()
        {
            string repRoot = RepositoryRoot;
            if (repRoot != null && repRoot.Length > 20) 
                repRoot = repRoot.Substring(0, 17) + "...";

            return "SVN at "
                + repRoot
                + Util.ConcatNE(" (Username: ", Username, ")");
        }

        public void UpdateLatest(string svnUrl, string targetPath)
        {
            string remoteUrl = null;
            try 
            {
                var lines = SVN("info", targetPath, "--xml");
                var doc = new XmlDocument();
                doc.LoadXml(string.Join(Environment.NewLine, lines.ToArray()));
                var node = doc.SelectSingleNode("//entry/url");
                if (node != null)
                    remoteUrl = node.InnerText;
            }
            catch (InvalidOperationException) 
            { 
                /* SVN will return exit code 1 if you use INFO on an uninitialized 
                 * directory, this is the easiest way to ignore that and
                 * just perform a CHECKOUT instead of an UPDATE. */ 
            }

            if (remoteUrl == null)
            {
                // perform checkout since remote repo URL can't be found
                SVN("checkout", svnUrl, targetPath);
            }
            else if (!svnUrl.Trim('/').Equals(remoteUrl.TrimEnd('/'), StringComparison.OrdinalIgnoreCase))
            {
                ThrowInvalidRepoUrl(svnUrl, targetPath, remoteUrl);
            }
            else
            {
                SVN("update", svnUrl, targetPath);
            }
        }

        public void GetDirectoryEntryInfo(string svnSourceUrl, DirectoryEntryBuilder container)
        {
            var items = SVN("list", svnSourceUrl);

            foreach (var item in items)
            {
                if (item.EndsWith("/"))
                    container.Directories.Add(item.TrimEnd('/'));
                else
                    container.AddFile(item);
            }
        }

        public void ExecuteClientCommand(string commandName, string arguments)
        {
            SVN(commandName, arguments);
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
                return SvnHelp(commandName);
            }
            catch (Exception e)
            {
                return "Help not available for the \"" + commandName + "\" command. The specific error message was: " + e.Message;
            }
        }

        public string GetClientCommandPreview()
        {
            return string.Format("{{command}} {0} ", BuildArguments(true));
        }

        public bool SupportsCommandHelp
        {
            get { return true; }
        }

        private static void ThrowInvalidRepoUrl(string svnUrl, string targetPath, string remoteUrl)
        {
            throw new InvalidOperationException(string.Format(
                    "The provider's SVN repository URL \"{0}\" is not the same as the one specified in the metadata for the local "
                  + "workspace (\"{1}\") at \"{2}\". To use the SVN UPDATE feature, these URLs must match, or an empty directory "
                  + "should be chosen such that a CHECKOUT will be performed instead.",
                    svnUrl,
                    remoteUrl,
                    targetPath));
        }

        private byte[] GetCurrentRevisionInternal(string svnUrl)
        {
            int revision = 0;
            var lines = SVN("info", svnUrl, "--xml");
            var doc = new XmlDocument();
            doc.LoadXml(string.Join(Environment.NewLine, lines.ToArray()));
            var node = doc.SelectSingleNode("//commit/@revision") as XmlAttribute;
            if (node != null)
                revision = int.Parse(node.Value);

            return BitConverter.GetBytes(revision);
        }

        private byte[] GetFileContentsInternal(string svnUrl)
        {
            var fileName = Path.GetTempFileName();
            SVN("export", svnUrl, fileName, "--force");
            var data = File.ReadAllBytes(fileName);
            try { File.Delete(fileName); }
            catch { }

            return data;
        }

        private IEnumerable<string> SVN(string command, params string[] args)
        {
            return this.ExecuteSvnCommand(command, BuildArguments(false, args));
        }

        private string SvnHelp(string command)
        {
            var commandOutput = this.ExecuteSvnCommand("help", command);
            return string.Join(Environment.NewLine, commandOutput.ToArray());
        }

        private string BuildArguments(bool obscurePassword, params string[] args)
        {
            var argBuffer = new StringBuilder();

            foreach (var arg in args)
                argBuffer.AppendFormat("\"{0}\" ", arg);

            argBuffer.Append("--non-interactive --trust-server-cert ");

            if (!string.IsNullOrEmpty(this.Username))
                argBuffer.AppendFormat("--username \"{0}\" ", this.Username);
            if (!string.IsNullOrEmpty(this.Password))
                argBuffer.AppendFormat("--password \"{0}\" ", obscurePassword ? "xxxxx" : this.Password);

            if (this.UseSSH)
            {
                // --config-option=config:tunnels:ssh="plink.exe -batch -i /path/to/private-key.ppk"
                argBuffer.AppendFormat(@"--config-option=config:tunnels:ssh=""{0} -batch", this.PlinkExePath);
                if (!string.IsNullOrEmpty(this.PrivateKeyPath))
                    argBuffer.AppendFormat(" -i {0}", this.SafePrivateKeyPath);
                argBuffer.Append("\"");
            }

            return argBuffer.ToString();
        }

        private IEnumerable<string> ExecuteSvnCommand(string commandName, string arguments)
        {
            var results = this.ExecuteCommandLine(
                this.SvnExePath,
                commandName + " " + arguments,
                null
            );

            foreach (var line in results.Output)
                this.LogInformation(line);

            foreach (var line in results.Error)
                this.LogError(line);

            if (results.ExitCode != 0)
            {
                var errorMessage = string.Join("", results.Error.ToArray());
                throw new InvalidOperationException(errorMessage);
            }

            return results.Output;
        }

        private string CombineSvnPaths(string pathA, string pathB)
        {
            if (string.IsNullOrEmpty(pathA)) pathA = string.Empty;
            if (string.IsNullOrEmpty(pathB)) pathB = string.Empty;
            return pathA.TrimEnd('/') + '/' + pathB.TrimStart('/');
        }

        private string CreateSvnUriTarget(string sourcePath)
        {
            // Ensure sourcePath starts with a /
            if (string.IsNullOrEmpty(sourcePath))
                sourcePath = DirectorySeparator.ToString();
            else if (!sourcePath.StartsWith(DirectorySeparator.ToString()))
                sourcePath = DirectorySeparator.ToString() + sourcePath;

            // Replace RepoName w/ Path if Necessary
            {
                int idx = sourcePath.IndexOf(DirectorySeparator, 1);
                if (idx == -1)
                    idx = sourcePath.Length;

                string name = sourcePath.Substring(1, idx - 1);

                var repo = this.Repositories.FirstOrDefault(r => r.RepositoryName.Equals(name, StringComparison.OrdinalIgnoreCase));
                if (repo != null)
                {
                    sourcePath = CombineSvnPaths(
                        repo.RepositoryPath,
                        sourcePath.Substring(idx));
                }
            }

            return CombineSvnPaths(RepositoryRoot, sourcePath);
        }

        public SubversionRepository[] Repositories { get; set; }
        RepositoryBase[] IMultipleRepositoryProvider.Repositories
        {
            get
            {
                return this.Repositories;
            }
            set
            {
                this.Repositories = Array.ConvertAll(value, r => (SubversionRepository)r);
            }
        }
    }
}
