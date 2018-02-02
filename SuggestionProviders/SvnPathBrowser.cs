using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Inedo.BuildMaster.Data;
using Inedo.BuildMaster.Extensibility.Agents;
using Inedo.BuildMasterExtensions.Subversion.Credentials;
using Inedo.Diagnostics;
using Inedo.Extensibility;
using Inedo.Extensibility.Agents;
using Inedo.Extensibility.Credentials;
using Inedo.Web;
using Inedo.Web.Plans.ArgumentEditors;

namespace Inedo.BuildMasterExtensions.Subversion.SuggestionProviders
{
    public sealed class SvnPathBrowser : IPathBrowser
    {
        public async Task<IEnumerable<IPathInfo>> GetPathInfosAsync(string path, IComponentConfiguration config)
        {
            var info = new PathBrowserInfo(config);
            
            using (var agent = info.CreateAgent())
            {
                var client = new SvnClient(info.UserName, AH.CreateSecureString(info.Password), agent, info.GetSvnExePath(agent), (ILogSink)Logger.Null);
                
                if (string.IsNullOrEmpty(info.RepositoryUrl))
                    throw new InvalidOperationException("The SVN repository URL could not be determined.");

                var paths = await client.EnumerateChildSourcePathsAsync(new SvnPath(info.RepositoryUrl, path)).ConfigureAwait(false);
                return paths.Where(p => p.IsDirectory);
            }
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
            public string RepositoryUrl => AH.CoalesceString(config[nameof(this.RepositoryUrl)], this.getCredentials.Value?.RepositoryUrl);
            public string UserName => AH.CoalesceString(config[nameof(this.UserName)], this.getCredentials.Value?.UserName);
            public string Password => AH.CoalesceString(config[nameof(this.Password)], AH.Unprotect(this.getCredentials.Value?.Password));
            public int? ApplicationId => ((IBrowsablePathEditorContext)config).ProjectId;

            public string GetSvnExePath(Agent agent)
            {
                string path = this.config["SvnExePath"];
                if (!string.IsNullOrEmpty(path))
                    return path;

                return RemoteMethods.GetEmbeddedSvnExePath(agent);
            }

            public Agent CreateAgent()
            {
                var vars = DB.Variables_GetVariablesAccessibleFromScope(
                    Variable_Name: "SvnDefaultServerName",
                    Application_Id: this.ApplicationId,
                    IncludeSystemVariables_Indicator: true
                );

                var variable = (from v in vars
                                orderby v.Application_Id != null ? 0 : 1
                                select v).FirstOrDefault();

                if (variable == null)
                    return BuildMasterAgent.CreateLocalAgent();
                else
                    return BuildMasterAgent.Create(InedoLib.UTF8Encoding.GetString(variable.Variable_Value));
            }

            private SubversionCredentials GetCredentials()
            {
                string credentialName = this.config["CredentialName"];
                if (string.IsNullOrEmpty(credentialName))
                    return null;

                return ResourceCredentials.Create<SubversionCredentials>(credentialName);
            }
        }
    }
}
