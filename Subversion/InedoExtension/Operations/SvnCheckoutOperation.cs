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
        private static readonly LazyRegex CheckedOutRevisionPattern = new LazyRegex(@"^Checked out revision (?<rev>[0-9]+)\.");

        [ScriptAlias("Credentials")]
        [DisplayName("Credentials")]
        public override string CredentialName { get; set; }
        [ScriptAlias("SourcePath")]
        [DisplayName("Source path")]
        [PlaceholderText("Repository root")]
        [BrowsablePath(typeof(SvnPathBrowser))]
        public string SourcePath { get; set; }
        [ScriptAlias("DiskPath")]
        [DisplayName("Working copy directory")]
        [FilePathEditor]
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

            var client = new SvnClient(context, this.UserName, this.Password, this.SvnExePath, this);
            var sourcePath = new SvnPath(this.BaseUrl, this.SourcePath);
            var result = await client.CheckoutAsync(sourcePath, context.ResolvePath(this.DestinationPath), this.AdditionalArguments).ConfigureAwait(false);

            this.LogClientResult(result);

            this.LogInformation("SVN checkout executed.");

            var match = CheckedOutRevisionPattern.Match(result.OutputLines[result.OutputLines.Count - 1]);
            if (match != null)
                this.RevisionNumber = match.Groups["rev"].Value;
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
