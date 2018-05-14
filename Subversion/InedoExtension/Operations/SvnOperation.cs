using System.ComponentModel;
using System.Security;
using Inedo.Diagnostics;
using Inedo.Documentation;
using Inedo.Extensibility;
using Inedo.Extensibility.Credentials;
using Inedo.Extensibility.Operations;
using Inedo.Extensions.Subversion.Credentials;

namespace Inedo.Extensions.Subversion.Operations
{
    public abstract class SvnOperation : ExecuteOperation, IHasCredentials<SubversionCredentials>
    {
        public abstract string CredentialName { get; set;  }

        [Category("Advanced")]
        [ScriptAlias("AdditionalArguments")]
        [DisplayName("Additional arguments")]
        public string AdditionalArguments { get; set; }

        [Category("Connection")]
        [ScriptAlias("RepositoryUrl")]
        [DisplayName("Repository URL")]
        [PlaceholderText("Use repository URL from credentials")]
        [MappedCredential(nameof(SubversionCredentials.RepositoryUrl))]
        public string RespositoryUrl { get; set; }
        [Category("Connection")]
        [ScriptAlias("UserName")]
        [DisplayName("User name")]
        [PlaceholderText("Use user name from credentials")]
        [MappedCredential(nameof(SubversionCredentials.UserName))]
        public string UserName { get; set; }
        [Category("Connection")]
        [ScriptAlias("Password")]
        [DisplayName("Password")]
        [PlaceholderText("Use password from credentials")]
        [MappedCredential(nameof(SubversionCredentials.Password))]
        public SecureString Password { get; set; }
        [Category("Connection")]
        [ScriptAlias("SvnExePath")]
        [DisplayName("svn.exe path")]
        [DefaultValue("$SvnExePath")]
        public string SvnExePath { get; set; }

        protected void LogClientResult(SvnClientExecutionResult result)
        {
            foreach (var line in result.OutputLines)
                this.LogDebug(line);

            foreach (var line in result.ErrorLines)
                this.LogError(line);
        }
    }
}
