using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Inedo.Diagnostics;
using Inedo.Documentation;
using Inedo.Extensibility;
using Inedo.Extensibility.Operations;
using Inedo.Extensions.Credentials;
using Inedo.Extensions.Subversion.Credentials;
using Inedo.Extensions.Subversion.RepositoryMonitors;
using Inedo.Web;

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
        [Output]
        [ScriptAlias("RevisionNumber")]
        [DisplayName("Revision number")]
        [PlaceholderText("eg. $RevisionNumber")]
        public string RevisionNumber { get; set; }

        public override async Task ExecuteAsync(IOperationExecutionContext context)
        {
            this.LogInformation("Executing SVN export...");
            var (c, r) = this.GetCredentialsAndResource(context);
            var client = new SvnClient(context, c, this.SvnExePath, this);
            var sourcePath = new SvnPath(r?.RepositoryUrl, this.SourcePath);
            this.RevisionNumber = await GetRevisionNumberAsync(c, sourcePath, context.CancellationToken);

            var result = await client.ExportAsync(sourcePath, context.ResolvePath(this.DestinationPath), this.AdditionalArguments, this.RevisionNumber).ConfigureAwait(false);
            
            this.LogClientResult(result);

            this.LogInformation("SVN export executed.");
        }

        private async Task<string> GetRevisionNumberAsync(UsernamePasswordCredentials credentials, SvnPath sourcePath, CancellationToken cancellationToken)
        {
            var http = SDK.CreateHttpClient();
            var path = sourcePath.AbsolutePath;
            using var request = new HttpRequestMessage(HttpMethod.Options, path);
            if (!string.IsNullOrEmpty(credentials?.UserName))
                request.Headers.Authorization = new AuthenticationHeaderValue("basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{credentials.UserName}:{AH.Unprotect(credentials.Password)}")));
            request.Content = new StringContent("<?xml version=\"1.0\" encoding=\"utf-8\"?><D:options xmlns:D=\"DAV:\"><D:activity-collection-set></D:activity-collection-set></D:options>");

            using var response = await http.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var resp = response.Content.ReadAsStringAsync();
                this.LogError($"Error status code ({response.StatusCode}) received while checking path \"{path}\": {resp}");
                return null;
            }

            if (!response.Headers.TryGetValues("SVN-Youngest-Rev", out var rev) || string.IsNullOrEmpty(rev.FirstOrDefault()))
            {
                this.LogError($"Error while checking path \"{path}\": header \"SVN-Youngest-Rev\" not found. ");
                return null;
            }

            this.LogDebug($"Found {rev.FirstOrDefault()}");
            return rev.FirstOrDefault();
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
