using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Inedo.Agents;
using Inedo.Diagnostics;
using Inedo.Extensibility;
using Inedo.Extensibility.Credentials;
using Inedo.Extensions.Subversion.Credentials;
using Inedo.IO;
using Inedo.Web;
using Inedo.Web.Plans.ArgumentEditors;

namespace Inedo.Extensions.Subversion.SuggestionProviders
{
    public sealed class SvnPathBrowser : IPathBrowser
    {
        public async Task<IEnumerable<IPathInfo>> GetPathInfosAsync(string path, IComponentConfiguration config)
        {
            var info = new PathBrowserInfo(config);

            var execOps = new LocalProcessExecuter();
            var client = new SvnClient(info.UserName, AH.CreateSecureString(info.Password), execOps, info.GetSvnExePath(), (ILogSink)Logger.Null);
                
            if (string.IsNullOrEmpty(info.RepositoryUrl))
                throw new InvalidOperationException("The SVN repository URL could not be determined.");

            var paths = await client.EnumerateChildSourcePathsAsync(new SvnPath(info.RepositoryUrl, path)).ConfigureAwait(false);
            return paths.Where(p => p.IsDirectory);
        }

        private sealed class PathBrowserInfo
        {
            private IComponentConfiguration config;
            private Lazy<SubversionCredentials> getCredentials;

            public PathBrowserInfo(IComponentConfiguration config)
            {
                this.config = config;
                this.getCredentials = new Lazy<SubversionCredentials>(GetCredentials);
            }

            public string SourcePath => config[nameof(this.SourcePath)];
            public string RepositoryUrl
            {
                get
                {
                    var repositoryUrl = AH.CoalesceString(config[nameof(this.RepositoryUrl)], this.getCredentials.Value?.RepositoryUrl);
                    if (!string.Equals(config[nameof(Operations.SvnOperation.UseCanonicalLayout)], bool.TrueString, StringComparison.OrdinalIgnoreCase))
                        return repositoryUrl;

                    string tag = config[nameof(Operations.SvnOperation.Tag)];
                    string branch = config[nameof(Operations.SvnOperation.Branch)];

                    return repositoryUrl + "/" + AH.CoalesceString(AH.ConcatNE("tags/", tag), AH.ConcatNE("branches/", branch), "trunk");
                }
            }
            public string UserName => AH.CoalesceString(config[nameof(this.UserName)], this.getCredentials.Value?.UserName);
            public string Password => AH.CoalesceString(config[nameof(this.Password)], AH.Unprotect(this.getCredentials.Value?.Password));
            public int? ApplicationId => ((IBrowsablePathEditorContext)config.EditorContext).ProjectId;

            public string GetSvnExePath()
            {
                string path = this.config["SvnExePath"];
                if (!string.IsNullOrEmpty(path))
                    return path;

                return PathEx.Combine(PathEx.GetDirectoryName(typeof(RemoteMethods).Assembly.Location), "Resources", "svn.exe");
            }

            private SubversionCredentials GetCredentials()
            {
                string credentialName = this.config["CredentialName"];
                if (string.IsNullOrEmpty(credentialName))
                    return null;

                return ResourceCredentials.Create<SubversionCredentials>(credentialName);
            }
        }

        private sealed class LocalProcessExecuter : IRemoteProcessExecuter
        {
            public IRemoteProcess CreateProcess(RemoteProcessStartInfo startInfo)
            {
                return new LocalProcess(startInfo);
            }

            public Task<string> GetEnvironmentVariableValueAsync(string name)
            {
                return Task.FromResult(Environment.GetEnvironmentVariable(name));
            }
        }
    }
}
