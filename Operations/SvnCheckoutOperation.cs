using System.ComponentModel;
using System.Threading.Tasks;
using Inedo.BuildMaster;
using Inedo.BuildMaster.Extensibility;
using Inedo.BuildMaster.Extensibility.Operations;
using Inedo.BuildMaster.Web.Controls;
using Inedo.BuildMaster.Web.Controls.Plans;
using Inedo.BuildMasterExtensions.Subversion.SuggestionProviders;
using Inedo.Diagnostics;
using Inedo.Documentation;

namespace Inedo.BuildMasterExtensions.Subversion.Operations
{
    [DisplayName("SVN Checkout")]
    [Description("Checks out a working copy from a repository.")]
    [Tag(Tags.SourceControl)]
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

        public override async Task ExecuteAsync(IOperationExecutionContext context)
        {
            this.LogInformation("Executing SVN checkout...");

            var client = new SvnClient(context, this.UserName, this.Password, this.SvnExePath, this);
            var sourcePath = new SvnPath(this.RespositoryUrl, this.SourcePath);
            var result = await client.CheckoutAsync(sourcePath, context.ResolvePath(this.DestinationPath), this.AdditionalArguments).ConfigureAwait(false);

            this.LogClientResult(result);

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
