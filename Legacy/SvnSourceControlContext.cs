using System;
using System.Linq;
using Inedo.BuildMaster.Extensibility.Providers.SourceControl;
using Inedo.BuildMaster.Files;

namespace Inedo.BuildMasterExtensions.Subversion
{
    internal sealed class SvnSourceControlContext : SourceControlContext
    {
        private static readonly string DirectorySeparator = "/";

        public string SvnTargetUrl { get; private set; }
        public string PathSpecifiedRepositoryName { get; private set; }
        public string[] SplitPath { get; private set; }
        public string LastSubDirectoryName { get; private set; }
        public string AbsoluteDiskPath { get; private set; }

        public SvnSourceControlContext(Subversion15Provider provider, string sourcePath)
        {
            this.SplitPath = (sourcePath ?? "").Split(DirectorySeparator.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            this.LastSubDirectoryName = this.SplitPath.LastOrDefault() ?? string.Empty;

            string path = EnsureStartingSlash(sourcePath);

            int index = path.IndexOf(DirectorySeparator, 1);
            if (index == -1)
                index = path.Length;

            string pathSpecifiedRepositoryName = path.Substring(1, index - 1);

            var repo = (provider.Repositories ?? new SourceRepository[0]).FirstOrDefault(r => r.Name.Equals(pathSpecifiedRepositoryName, StringComparison.OrdinalIgnoreCase));
            if (repo != null)
            {
                this.PathSpecifiedRepositoryName = pathSpecifiedRepositoryName;
                this.Repository = repo;
                this.RepositoryRelativePath = CombineSvnPaths(repo.RemoteUrl, path.Substring(index));
                this.WorkspaceDiskPath = repo.GetDiskPath(provider.FileOps);
            }
            else
            {
                var tmpRepo = new SourceRepository() { RemoteUrl = provider.RepositoryRoot };
                this.RepositoryRelativePath = path;
                this.WorkspaceDiskPath = tmpRepo.GetDiskPath(provider.FileOps);
            }

            this.SvnTargetUrl = CombineSvnPaths(provider.RepositoryRoot, this.RepositoryRelativePath);
            this.AbsoluteDiskPath = CombineSvnPaths(this.WorkspaceDiskPath, this.RepositoryRelativePath);
        }

        public bool TargetUrlMatchesRemoteUrl(string remoteUrl)
        {
            return this.SvnTargetUrl.Trim('/').Equals(remoteUrl.TrimEnd('/'), StringComparison.OrdinalIgnoreCase);
        }

        private static string CombineSvnPaths(string pathA, string pathB)
        {
            string a = pathA ?? string.Empty;
            string b = pathB ?? string.Empty;
            return a.TrimEnd('/') + "/" + b.TrimStart('/');
        }

        private static string EnsureStartingSlash(string path)
        {
            return CombineSvnPaths("/", path);
        }

        internal SystemEntryInfo CreateRelativeSystemEntryInfo(string relativePath)
        {
            if (relativePath.EndsWith("/"))
                return new DirectoryEntryInfo(relativePath.TrimEnd('/'), CombineSvnPaths(this.RepositoryRelativePath, relativePath));
            else
                return new FileEntryInfo(relativePath, CombineSvnPaths(this.RepositoryRelativePath, relativePath));
        }
    }
}
