using Inedo.Agents;
using Inedo.Extensibility.Agents;
using Inedo.IO;

namespace Inedo.BuildMasterExtensions.Subversion
{
    internal static class RemoteMethods
    {
        public static string GetEmbeddedSvnExePath(Agent agent)
        {
            var executer = agent.GetService<IRemoteMethodExecuter>();
            string assemblyDir = executer.InvokeFunc(GetAgentProviderAssemblyDirectory);
            var fileOps = agent.GetService<IFileOperationsExecuter>();
            return fileOps.CombinePath(assemblyDir, "Resources", "svn.exe");
        }

        public static string GetEmbeddedPlinkExePath(Agent agent)
        {
            // returns the path to the embedded plink.exe in Linux format (/path/to/_WEBTEMP/Subversion/plink.exe) 
            // because SVN will not accept backslashes in its SSH configuration
            var executer = agent.GetService<IRemoteMethodExecuter>();
            string assemblyDir = executer.InvokeFunc(GetAgentProviderAssemblyDirectory);
            var fileOps = agent.GetService<IFileOperationsExecuter>();
            string path = fileOps.CombinePath(assemblyDir, "Resources", "plink.exe");
            return path.Replace(@"\", "/");
        }

        private static string GetAgentProviderAssemblyDirectory()
        {
            return PathEx.GetDirectoryName(typeof(Subversion15Provider).Assembly.Location);
        }
    }
}
