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
    [DisplayName("SVN Export")]
    [Description("Gets the unversioned contents of a repository to a specified directory.")]
    [Tag(Tags.SourceControl)]
    [ScriptAlias("Svn-Export")]
    [Example(@"
# export the contents of a remote repository so .svn directories are not included
Svn-Export(
    Credentials: Hdars-Subversion,
    SourcePath: trunk,
    To: ~\Sources
);
")]
    public sealed class SvnExportOperation : SvnOperation
    {
        [ScriptAlias("Credentials")]
        [DisplayName("Credentials")]
        public override string CredentialName { get; set; }
        [Required]
        [ScriptAlias("SourcePath")]
        [DisplayName("Source path")]
        [BrowsablePath(typeof(SvnPathBrowser))]
        public string SourcePath { get; set; }
        [ScriptAlias("DiskPath")]
        [DisplayName("Export to directory")]
        [FilePathEditor]
        [PlaceholderText("$WorkingDirectory")]
        public string DestinationPath { get; set; }

        public override async Task ExecuteAsync(IOperationExecutionContext context)
        {
            this.LogInformation("Executing SVN export...");

            var client = new SvnClient(context, this.UserName, this.Password, this.SvnExePath, this);
            var sourcePath = new SvnPath(this.RespositoryUrl, this.SourcePath);
            var result = await client.ExportAsync(sourcePath, context.ResolvePath(this.DestinationPath), this.AdditionalArguments).ConfigureAwait(false);

            this.LogClientResult(result);

            this.LogInformation("SVN export executed.");
        }

        protected override ExtendedRichDescription GetDescription(IOperationConfiguration config)
        {
            return new ExtendedRichDescription(
                new RichDescription("SVN Export"),
                new RichDescription("from ", new Hilite(config[nameof(this.SourcePath)]), " to ", new Hilite(config[nameof(this.DestinationPath)]))
            );
        }
    }
}
