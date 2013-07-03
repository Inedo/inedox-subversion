using System;
using Inedo.BuildMaster;
using Inedo.BuildMaster.Extensibility.Providers.SourceControl;
using Inedo.BuildMaster.Web;

namespace Inedo.BuildMasterExtensions.Subversion
{
    [CustomEditor(typeof(SubversionRepositoryEditor))]
    public sealed class SubversionRepository : RepositoryBase
    {
        [Persistent]
        public string SvnRepositoryName { get; set; }

        public override string RepositoryName
        {
            get
            {
                if (!string.IsNullOrEmpty(SvnRepositoryName)) return SvnRepositoryName;
                string rp = (RepositoryPath ?? "").TrimEnd('/');
                return rp.Substring(Math.Max(rp.LastIndexOf('/'), 0));
            }
        }
    }
}
