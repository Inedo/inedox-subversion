using System.ComponentModel;
using System.Security;
using Inedo.Documentation;
using Inedo.Extensibility;
using Inedo.Extensibility.Credentials;
using Inedo.Serialization;
using Inedo.Web;

namespace Inedo.BuildMasterExtensions.Subversion.Credentials
{
    [ScriptAlias("Subversion")]
    [DisplayName("Subversion")]
    [Description("Credentials for Subversion.")]
    public sealed class SubversionCredentials : ResourceCredentials
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
    }
}
