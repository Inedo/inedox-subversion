using Inedo.Documentation;
using Inedo.Extensibility;
using Inedo.Extensibility.Credentials;
using Inedo.Extensibility.RepositoryMonitors;
using Inedo.Extensions.Subversion.Credentials;
using Inedo.Serialization;
using System.Collections.Generic;
using System.ComponentModel;
using System.Security;
using System.Threading.Tasks;

namespace Inedo.Extensions.Subversion.RepositoryMonitors
{
    [DisplayName("Subversion")]
    [Description("Monitors a Subversion repository for new commits.")]
    public sealed class SvnRepositoryMonitor : RepositoryMonitor, IHasCredentials<SubversionCredentials>
    {
        [Persistent]
        [DisplayName("Credentials")]
        [Category("Connection/Identity")]
        public string CredentialName { get; set; }

        [Persistent]
        [Category("Connection")]
        [ScriptAlias("RepositoryUrl")]
        [DisplayName("Repository URL")]
        [PlaceholderText("Use repository URL from credentials")]
        [MappedCredential(nameof(SubversionCredentials.RepositoryUrl))]
        public string RepositoryUrl { get; set; }
        [Persistent]
        [Category("Connection")]
        [ScriptAlias("UserName")]
        [DisplayName("User name")]
        [PlaceholderText("Use user name from credentials")]
        [MappedCredential(nameof(SubversionCredentials.UserName))]
        public string UserName { get; set; }
        [Persistent(Encrypted = true)]
        [Category("Connection")]
        [ScriptAlias("Password")]
        [DisplayName("Password")]
        [PlaceholderText("Use password from credentials")]
        [MappedCredential(nameof(SubversionCredentials.Password))]
        public SecureString Password { get; set; }
        [Persistent]
        [Category("Connection")]
        [ScriptAlias("SvnExePath")]
        [DisplayName("svn.exe path")]
        [DefaultValue("$SvnExePath")]
        public string SvnExePath { get; set; }

        public override async Task<IReadOnlyDictionary<string, RepositoryCommit>> GetCurrentCommitsAsync(IRepositoryMonitorContext context)
        {
            var client = new SvnClient(this.UserName, this.Password, context.Agent, this.SvnExePath, this, context.CancellationToken);
            var branches = await client.EnumerateBranchesAsync(new SvnPath(this.RepositoryUrl, string.Empty));

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
