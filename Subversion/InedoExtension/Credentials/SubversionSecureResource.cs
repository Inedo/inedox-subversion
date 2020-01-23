using System.ComponentModel;
using Inedo.Documentation;
using Inedo.Extensibility.SecureResources;
using Inedo.Serialization;

namespace Inedo.Extensions.Subversion.Credentials
{
    [DisplayName("Subversion Repository")]
    [Description("Connect to a SVN repository for source control integration.")]
    public sealed class SubversionSecureResource : SecureResource<Extensions.Credentials.UsernamePasswordCredentials>
    {
        [Required]
        [Persistent]
        [DisplayName("SVN repository URL")]
        public string RepositoryUrl { get; set; }

        public override RichDescription GetDescription() => new RichDescription(this.RepositoryUrl);
    }
}
