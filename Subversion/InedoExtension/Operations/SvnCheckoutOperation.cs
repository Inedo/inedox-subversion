using System.ComponentModel;
using System.Threading.Tasks;
using Inedo.Diagnostics;
using Inedo.Documentation;
using Inedo.Extensibility;
using Inedo.Extensibility.Operations;
using Inedo.Extensions.Subversion.SuggestionProviders;
using Inedo.Web;
using Inedo.Web.Plans.ArgumentEditors;

namespace Inedo.Extensions.Subversion.Operations
{
    [DisplayName("SVN Checkout")]
    [Description("Checks out a working copy from a repository.")]
    [Tag("source-control")]
    [ScriptAlias("Svn-Checkout")]
    [Example(@"
# checkout a remote repository locally
Svn-Checkout(
    Credentials: Hdars-Subversion,
    SourcePath: trunk,
    To: ~\Sources
);
")]
    public sealed class SvnCheckoutOperation : SvnOperation
    {
        [ScriptAlias("SourcePath")]
        [DisplayName("Source path")]
        [PlaceholderText("Repository root")]
        [BrowsablePath(typeof(SvnPathBrowser))]
        public string SourcePath { get; set; }
        [ScriptAlias("DiskPath")]
        [DisplayName("Working copy directory")]
        [FieldEditMode(FieldEditMode.ServerDirectoryPath)]
        [PlaceholderText("$WorkingDirectory")]
        public string DestinationPath { get; set; }
        [Output]
        [ScriptAlias("RevisionNumber")]
        [DisplayName("Revision number")]
        [PlaceholderText("eg. $RevisionNumber")]
        public string RevisionNumber { get; set; }

        public override async Task ExecuteAsync(IOperationExecutionContext context)
        {
            this.LogInformation("Executing SVN checkout...");

            var (c, r) = this.GetCredentialsAndResource(context);

            var client = new SvnClient(context, c, this.SvnExePath, this);
            var sourcePath = new SvnPath(r?.RepositoryUrl, this.SourcePath);
            var destinationPath = context.ResolvePath(this.DestinationPath);
            var result = await client.CheckoutAsync(sourcePath, destinationPath, this.AdditionalArguments).ConfigureAwait(false);

            this.LogClientResult(result);

            this.RevisionNumber = await client.GetRevisionNumberAsync(new SvnPath(destinationPath, null)).ConfigureAwait(false);

            this.LogInformation("SVN checkout executed.");
        }

        protected override ExtendedRichDescription GetDescription(IOperationConfiguration config)
        {
            return new ExtendedRichDescription(
                new RichDescription("SVN Checkout"),
                new RichDescription("from ", new Hilite(config[nameof(this.SourcePath)]), " to ", new Hilite(config[nameof(this.DestinationPath)]))
            );
        }
    }
}
