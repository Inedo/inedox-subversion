using System.ComponentModel;
using System.Security;
using Inedo.Documentation;
using Inedo.Extensibility;
using Inedo.Extensibility.Credentials;
using Inedo.Extensibility.SecureResources;
using Inedo.Serialization;
using Inedo.Web;

namespace Inedo.Extensions.Subversion.Credentials
{
    [ScriptAlias("Subversion")]
    [DisplayName("Subversion (Legacy)")]
    [Description("Credentials for Subversion.")]
    [PersistFrom("Inedo.BuildMasterExtensions.Subversion.Credentials.SubversionCredentials,Subversion")]
    [PersistFrom("Inedo.Extensions.Subversion.Credentials.SubversionCredentials,Subversion")]
    public sealed class SubversionLegacyCredentials : ResourceCredentials
    {
        [Required]
        [Persistent]
        [DisplayName("SVN repository URL")]
        public string RepositoryUrl { get; set; }

        [Persistent]
        [DisplayName("User name")]
        [PlaceholderText("Anonymous")]
        public string UserName { get; set; }

        [Persistent(Encrypted = true)]
        [DisplayName("Password")]
        [FieldEditMode(FieldEditMode.Password)]
        public SecureString Password { get; set; }

        public override RichDescription GetDescription()
        {
            return new RichDescription(this.UserName, " @ ", this.RepositoryUrl);
        }

        public override SecureCredentials ToSecureCredentials() =>
            new Extensions.Credentials.UsernamePasswordCredentials { UserName = this.UserName, Password = this.Password };

        public override SecureResource ToSecureResource() =>
            new SubversionSecureResource { RepositoryUrl = this.RepositoryUrl };
    }
}
