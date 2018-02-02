using System.ComponentModel;
using System.Threading.Tasks;
using Inedo.BuildMaster;
using Inedo.Diagnostics;
using Inedo.Documentation;
using Inedo.Extensibility;
using Inedo.Extensibility.Operations;
using Inedo.Web.Plans.ArgumentEditors;

namespace Inedo.BuildMasterExtensions.Subversion.Operations
{
    [DisplayName("SVN Update")]
    [Description("Bring changes from a repository into the working copy.")]
    [Tag(Tags.SourceControl)]
    [ScriptAlias("Svn-Update")]
    [Example(@"
# update a local disk path (specifed as an application variable) to match the contents of its remote repository
Svn-Update(
    Credentials: Hdars-Subversion,
    DiskPath: E:\LocalSvnRepos\$ApplicationSvnRepo
);
")]
    public sealed class SvnUpdateOperation : SvnOperation
    {
        [ScriptAlias("Credentials")]
        [DisplayName("Credentials")]
        public override string CredentialName { get; set; }
        [ScriptAlias("DiskPath")]
        [DisplayName("Working copy directory")]
        [FilePathEditor]
        [PlaceholderText("$WorkingDirectory")]
        public string DiskPath { get; set; }

        public override async Task ExecuteAsync(IOperationExecutionContext context)
        {
            this.LogInformation("Executing SVN update...");

            var client = new SvnClient(context, this.UserName, this.Password, this.SvnExePath, this);
            var result = await client.UpdateAsync(context.ResolvePath(this.DiskPath), this.AdditionalArguments).ConfigureAwait(false);

            this.LogClientResult(result);

            this.LogInformation("SVN update executed.");
        }

        protected override ExtendedRichDescription GetDescription(IOperationConfiguration config)
        {
            return new ExtendedRichDescription(
                new RichDescription("SVN Update"),
                new RichDescription("working copy in ", new Hilite(config[nameof(this.DiskPath)]))
            );
        }
    }
}
