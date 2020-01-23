using System.Collections.Generic;
using System.ComponentModel;
using System.Security;
using System.Threading.Tasks;
using Inedo.Documentation;
using Inedo.Extensibility;
using Inedo.Extensibility.RepositoryMonitors;
using Inedo.Serialization;

namespace Inedo.Extensions.Subversion.RepositoryMonitors
{
    [DisplayName("Subversion")]
    [Description("Monitors a Subversion repository for new commits.")]
    public sealed class SvnRepositoryMonitor : RepositoryMonitor, ISvnConfiguration
    {
        [Persistent]
        public string CredentialName { get; set; }

        [DisplayName("From")]
        [Category("Connection")]
        public string ResourceName { get => this.CredentialName; set => this.CredentialName = value; }

        [Persistent]
        [Category("Connection")]
        [ScriptAlias("RepositoryUrl")]
        [DisplayName("Repository URL")]
        [PlaceholderText("Use repository URL from credentials")]
        public string RepositoryUrl { get; set; }
        [Persistent]
        [Category("Connection")]
        [ScriptAlias("UserName")]
        [DisplayName("User name")]
        [PlaceholderText("Use user name from credentials")]
        public string UserName { get; set; }
        [Persistent(Encrypted = true)]
        [Category("Connection")]
        [ScriptAlias("Password")]
        [DisplayName("Password")]
        [PlaceholderText("Use password from credentials")]
        public SecureString Password { get; set; }
        [Persistent]
        [Category("Connection")]
        [ScriptAlias("SvnExePath")]
        [DisplayName("svn.exe path")]
        [DefaultValue("$SvnExePath")]
        public string SvnExePath { get; set; }

        public override async Task<IReadOnlyDictionary<string, RepositoryCommit>> GetCurrentCommitsAsync(IRepositoryMonitorContext context)
        {
            var (c, r) = this.GetCredentialsAndResource(context);
            var client = new SvnClient(c, context.Agent, this.SvnExePath, this, context.CancellationToken);
            var branches = await client.EnumerateBranchesAsync(new SvnPath(r?.RepositoryUrl, string.Empty));

            var results = new Dictionary<string, RepositoryCommit>();
            foreach (var b in branches)
                results[b.Path.RepositoryRelativePath.Trim('/')] = new SvnRepositoryCommit { Revision = b.Revision };

            return results;
        }

        public override RichDescription GetDescription()
        {
            return new RichDescription(
                "Subversion repository at ",
                new Hilite(AH.CoalesceString(this.RepositoryUrl, this.CredentialName))
            );
        }
    }
}
