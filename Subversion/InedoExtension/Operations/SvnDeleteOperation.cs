using System.ComponentModel;
using System.Threading.Tasks;
using Inedo.Diagnostics;
using Inedo.Documentation;
using Inedo.Extensibility;
using Inedo.Extensibility.Operations;
using Inedo.Extensions.Subversion.SuggestionProviders;
using Inedo.Web;

namespace Inedo.Extensions.Subversion.Operations
{
    [DisplayName("SVN Delete")]
    [Description("Deletes a file in a Subversion repository.")]
    [Tag("source-control")]
    [ScriptAlias("Svn-Delete")]
    public sealed class SvnDeleteOperation : SvnOperation
    {
        [ScriptAlias("Credentials")]
        [DisplayName("Credentials")]
        public override string CredentialName { get; set; }
        [Required]
        [ScriptAlias("Path")]
        [DisplayName("File path")]
        [BrowsablePath(typeof(SvnPathBrowser))]
        public string Path { get; set; }
        [Required]
        [ScriptAlias("Message")]
        [DisplayName("Log message")]
        public string Message { get; set; }

        public override async Task ExecuteAsync(IOperationExecutionContext context)
        {
            this.LogInformation("Executing SVN delete...");

            var client = new SvnClient(context, this.UserName, this.Password, this.SvnExePath, this);
            var path = new SvnPath(this.RespositoryUrl, this.Path);
            var result = await client.DeleteAsync(path, this.Message, this.AdditionalArguments).ConfigureAwait(false);

            this.LogClientResult(result);

            this.LogInformation("SVN delete executed.");
        }

        protected override ExtendedRichDescription GetDescription(IOperationConfiguration config)
        {
            return new ExtendedRichDescription(
                new RichDescription("SVN delete"),
                new RichDescription(new Hilite(config[nameof(this.Path)]))
            );
        }
    }
}
