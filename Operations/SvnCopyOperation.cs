using System.ComponentModel;
using System.Threading.Tasks;
using Inedo.BuildMaster;
using Inedo.BuildMaster.Extensibility;
using Inedo.BuildMaster.Extensibility.Operations;
using Inedo.BuildMaster.Web.Controls;
using Inedo.BuildMasterExtensions.Subversion.SuggestionProviders;
using Inedo.Diagnostics;
using Inedo.Documentation;

namespace Inedo.BuildMasterExtensions.Subversion.Operations
{
    [DisplayName("SVN Copy")]
    [Description("Creates a copy of a source path to facilitate branching and tagging.")]
    [Tag(Tags.SourceControl)]
    [ScriptAlias("Svn-Copy")]
    [Example(@"
# branch trunk to a path using the current release name
Svn-Copy(
    Credentials: Hdars-Subversion,
    From: trunk,
    To: branches/$ReleaseName
);

# create a tag of the current package number
Svn-Copy(
    Credentials: Hdars-Subversion,
    From: trunk,
    To: tags/$ReleaseNumber.$PackageNumber
);
")]
    public sealed class SvnCopyOperation : SvnOperation
    {
        [ScriptAlias("Credentials")]
        [DisplayName("Credentials")]
        public override string CredentialName { get; set; }
        [Required]
        [ScriptAlias("From")]
        [DisplayName("From path")]
        [BrowsablePath(typeof(SvnPathBrowser))]
        public string SourcePath { get; set; }
        [Required]
        [ScriptAlias("To")]
        [DisplayName("To path")]
        [BrowsablePath(typeof(SvnPathBrowser))]
        public string DestinationPath { get; set; }
        [Required]
        [ScriptAlias("Message")]
        [DisplayName("Log message")]
        public string Message { get; set; }

        public override async Task ExecuteAsync(IOperationExecutionContext context)
        {
            this.LogInformation("Executing SVN copy...");

            var client = new SvnClient(context, this.UserName, this.Password, this.SvnExePath, this);
            var sourcePath = new SvnPath(this.RespositoryUrl, this.SourcePath);
            var destinationPath = new SvnPath(this.RespositoryUrl, this.DestinationPath);
            var result = await client.CopyAsync(sourcePath, destinationPath, this.Message, this.AdditionalArguments).ConfigureAwait(false);

            this.LogClientResult(result);

            this.LogInformation("SVN copy executed.");
        }

        protected override ExtendedRichDescription GetDescription(IOperationConfiguration config)
        {
            return new ExtendedRichDescription(
                new RichDescription("SVN Copy"),
                new RichDescription("from ", new Hilite(config[nameof(this.SourcePath)]), " to ", new Hilite(config[nameof(this.DestinationPath)]))
            );
        }
    }
}
