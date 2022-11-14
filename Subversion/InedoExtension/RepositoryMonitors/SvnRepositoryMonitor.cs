using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Inedo.Diagnostics;
using Inedo.Documentation;
using Inedo.Extensibility.ResourceMonitors;
using Inedo.Extensions.Credentials;
using Inedo.Extensions.Subversion.Credentials;
using Inedo.Serialization;

namespace Inedo.Extensions.Subversion.RepositoryMonitors
{
    [DisplayName("Subversion")]
    [Description("Monitors a Subversion repository for new commits.")]
    public sealed class SvnRepositoryMonitor : ResourceMonitor<SvnRepositoryState, SubversionSecureResource>, IMissingPersistentPropertyHandler
    {
        [Persistent]
        [DisplayName("Paths to monitor")]
        [PlaceholderText("e.g. /trunk")]
        [Description("Enter each path to monitor, one per line")]
        public string[] PathsToMonitor { get; set; }

        public async override Task<IReadOnlyDictionary<string, ResourceMonitorState>> GetCurrentStatesAsync(IResourceMonitorContext context)
        {
            var r = (SubversionSecureResource)context.Resource;
            var c = r.GetCredentials(context) as UsernamePasswordCredentials;

            var http = SDK.CreateHttpClient();
            if (!Uri.TryCreate(r.RepositoryUrl, UriKind.Absolute, out var baseUri))
                throw new InvalidOperationException($"Invalid {nameof(r.RepositoryUrl)}: {r.RepositoryUrl}");

            http.BaseAddress = baseUri;
            var paths = this.PathsToMonitor?.Length > 0 ? this.PathsToMonitor : new[] { "/" };

            var results = new Dictionary<string, ResourceMonitorState>();
            foreach (var path in paths)
            {
                this.LogDebug($"Querying {path}");

                using var request = new HttpRequestMessage(HttpMethod.Options, path.TrimStart('/'));
                if (!string.IsNullOrEmpty(c?.UserName))
                    request.Headers.Authorization = new AuthenticationHeaderValue("basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{c.UserName}:{AH.Unprotect(c.Password)}")));
                request.Content = new StringContent("<?xml version=\"1.0\" encoding=\"utf-8\"?><D:options xmlns:D=\"DAV:\"><D:activity-collection-set></D:activity-collection-set></D:options>");

                using var response = await http.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    var resp = response.Content.ReadAsStringAsync();
                    this.LogError($"Error status code ({response.StatusCode}) received while checking path \"{path}\": {resp}");
                    continue;
                }

                if (!response.Headers.TryGetValues("SVN-Youngest-Rev", out var rev) || string.IsNullOrEmpty(rev.FirstOrDefault()))
                {
                    this.LogError($"Error while checking path \"{path}\": header \"SVN-Youngest-Rev\" not found. ");
                    continue;
                }
                
                this.LogDebug($"Found {rev.FirstOrDefault()}");
                results[path.Trim('/') + "/"] = new SvnRepositoryState { Revision = rev.FirstOrDefault() };
            }

            return results;
        }

        public override RichDescription GetDescription()
        {
            var paths = this.PathsToMonitor?.Length > 0 ? this.PathsToMonitor : new[] { "/" };
            return new RichDescription(new ListHilite(paths));
        }

        void IMissingPersistentPropertyHandler.OnDeserializedMissingProperties(IReadOnlyDictionary<string, string> missingProperties)
        {
            if (missingProperties.ContainsKey("SvnExePath"))
                _ = missingProperties["SvnExePath"];
            if (missingProperties.ContainsKey("CredentialName"))
                _ = missingProperties["CredentialName"];
            if (missingProperties.ContainsKey("ResourceName"))
                _ = missingProperties["ResourceName"];
            //
        }
    }
}
