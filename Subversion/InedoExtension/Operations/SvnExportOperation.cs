﻿using System.ComponentModel;
using System.Threading.Tasks;
using Inedo.Diagnostics;
using Inedo.Documentation;
using Inedo.Extensibility;
using Inedo.Extensibility.Operations;
using Inedo.Web;
using Inedo.Web.Plans.ArgumentEditors;

namespace Inedo.Extensions.Subversion.Operations
{
    [DisplayName("SVN Export")]
    [Description("Gets the unversioned contents of a repository to a specified directory.")]
    [Tag("source-control")]
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
        [Required]
        [ScriptAlias("SourcePath")]
        [DisplayName("Source path")]
        public string SourcePath { get; set; }
        [ScriptAlias("DiskPath")]
        [DisplayName("Export to directory")]
        [FieldEditMode(FieldEditMode.ServerDirectoryPath)]
        [PlaceholderText("$WorkingDirectory")]
        public string DestinationPath { get; set; }

        public override async Task ExecuteAsync(IOperationExecutionContext context)
        {
            this.LogInformation("Executing SVN export...");
            var (c, r) = this.GetCredentialsAndResource(context);
            var client = new SvnClient(context, c, this.SvnExePath, this);
            var sourcePath = new SvnPath(r?.RepositoryUrl, this.SourcePath);
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
