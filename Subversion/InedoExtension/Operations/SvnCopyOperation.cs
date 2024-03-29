﻿using System.ComponentModel;
using System.Threading.Tasks;
using Inedo.Diagnostics;
using Inedo.Documentation;
using Inedo.Extensibility;
using Inedo.Extensibility.Operations;

namespace Inedo.Extensions.Subversion.Operations
{
    [DisplayName("SVN Copy")]
    [Description("Creates a copy of a source path to facilitate branching and tagging.")]
    [Tag("source-control")]
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
        [Required]
        [ScriptAlias("Source")]
        [DisplayName("From path")]
        [Description("This is the path relative to the Repository URL")]
        public string SourcePath { get; set; }
        [Required]
        [ScriptAlias("To")]
        [DisplayName("To path")]
        [Description("This is the path relative to the Repository URL.  All parent folders listed in the path must exist except the final child folder.")]
        public string DestinationPath { get; set; }
        [Required]
        [ScriptAlias("Message")]
        [DisplayName("Log message")]
        public string Message { get; set; }
        [ScriptAlias("RevisionNumber")]
        [DisplayName("Revision number")]
        public string RevisionNumber { get; set; }

        public override async Task ExecuteAsync(IOperationExecutionContext context)
        {
            this.LogInformation("Executing SVN copy...");

            var (c, r) = this.GetCredentialsAndResource(context);

            var client = new SvnClient(context, c, this.SvnExePath, this);
            var sourcePath = new SvnPath(r?.RepositoryUrl, this.SourcePath);
            var destinationPath = new SvnPath(r?.RepositoryUrl, this.DestinationPath);
            var result = await client.CopyAsync(sourcePath, destinationPath, this.Message, this.AdditionalArguments, this.RevisionNumber).ConfigureAwait(false);

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
