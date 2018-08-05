using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using Inedo.Agents;
using Inedo.Diagnostics;
using Inedo.Extensibility.Agents;
using Inedo.Extensibility.Operations;
using Inedo.Web;

namespace Inedo.Extensions.Subversion
{
    internal sealed class SvnClient
    {
        private string svnExePath;
        private string userName;
        private SecureString password;
        private CancellationToken cancellationToken;
        private Lazy<IRemoteProcessExecuter> execOps;
        private ILogSink log;

        public SvnClient(IOperationExecutionContext context, string userName, SecureString password, string svnExePath, ILogSink log)
            : this(userName, password, context.Agent, svnExePath, log, context.CancellationToken)
        {
        }

        public SvnClient(string userName, SecureString password, Agent agent, string svnExePath, ILogSink log, CancellationToken? cancellationToken = null)
        {
            this.execOps = new Lazy<IRemoteProcessExecuter>(() => agent.GetService<IRemoteProcessExecuter>());
            this.userName = userName;
            this.password = password;
            this.svnExePath = AH.CoalesceString(svnExePath, RemoteMethods.GetEmbeddedSvnExePath(agent));
            this.log = log ?? (ILogSink)Logger.Null;
            this.cancellationToken = cancellationToken ?? CancellationToken.None;
        }

        internal SvnClient(string userName, SecureString password, IRemoteProcessExecuter execOps, string svnExePath, ILogSink log, CancellationToken? cancellationToken = null)
        {
            this.execOps = new Lazy<IRemoteProcessExecuter>(() => execOps);
            this.userName = userName;
            this.password = password;
            this.svnExePath = svnExePath;
            this.log = log ?? (ILogSink)Logger.Null;
            this.cancellationToken = cancellationToken ?? CancellationToken.None;
        }

        public async Task<SvnClientExecutionResult> UpdateAsync(string workingCopyDirectory, string additionalArguments)
        {
            var args = new SvnArgumentBuilder();
            args.Append("update");
            args.AppendQuoted(workingCopyDirectory);
            args.Append(additionalArguments);

            return await this.ExecuteCommandLineAsync(args).ConfigureAwait(false);
        }

        public async Task<SvnClientExecutionResult> CheckoutAsync(SvnPath sourcePath, string destinationDirectory, string additionalArguments)
        {
            var args = new SvnArgumentBuilder();
            args.Append("checkout");
            args.AppendQuoted(sourcePath.AbsolutePath);
            args.AppendQuoted(destinationDirectory);
            args.Append(additionalArguments);

            return await this.ExecuteCommandLineAsync(args).ConfigureAwait(false);
        }

        public async Task<SvnClientExecutionResult> ExportAsync(SvnPath sourcePath, string destinationDirectory, string additionalArguments)
        {
            var args = new SvnArgumentBuilder();
            args.Append("export");
            args.AppendQuoted(sourcePath.AbsolutePath);
            args.AppendQuoted(destinationDirectory);
            args.Append(additionalArguments);

            return await this.ExecuteCommandLineAsync(args).ConfigureAwait(false);
        }

        public async Task<SvnClientExecutionResult> CopyAsync(SvnPath sourcePath, SvnPath destinationPath, string message, string additionalArguments)
        {
            var args = new SvnArgumentBuilder();
            args.Append("copy");
            args.AppendQuoted(sourcePath.AbsolutePath);
            args.AppendQuoted(destinationPath.AbsolutePath);
            args.Append("-m");
            args.AppendQuoted(message);
            args.Append(additionalArguments);

            return await this.ExecuteCommandLineAsync(args).ConfigureAwait(false);
        }

        public async Task<SvnClientExecutionResult> DeleteAsync(SvnPath path, string message, string additionalArguments)
        {
            var args = new SvnArgumentBuilder();
            args.Append("delete");
            args.AppendQuoted(path.AbsolutePath);
            args.Append("-m");
            args.AppendQuoted(message);
            args.Append(additionalArguments);

            return await this.ExecuteCommandLineAsync(args).ConfigureAwait(false);
        }

        public async Task<IEnumerable<SvnPath>> EnumerateChildSourcePathsAsync(SvnPath path)
        {
            var args = new SvnArgumentBuilder();
            args.Append("ls");
            args.AppendQuoted(path.AbsolutePath);
            var result = await this.ExecuteCommandLineAsync(args).ConfigureAwait(false);
            if (result.ErrorLines.Count > 0)
                throw new InvalidOperationException(string.Join(Environment.NewLine, result.ErrorLines));

            var lines = from o in result.OutputLines
                        where o?.Length > 0
                        select o;

            return lines.Select(o => new SvnPath(path, o));
        }

        private async Task<SvnClientExecutionResult> ExecuteCommandLineAsync(SvnArgumentBuilder args)
        {
            args.Append("--non-interactive");
            args.Append("--trust-server-cert");

            if (!string.IsNullOrEmpty(this.userName))
            {
                args.Append("--username");
                args.AppendQuoted(this.userName);
            }
            if (this.password != null)
            {
                args.Append("--password");
                args.AppendSensitive(AH.Unprotect(this.password));
            }

            var startInfo = new RemoteProcessStartInfo
            {
                FileName = this.svnExePath,
                Arguments = args.ToString()
            };

            this.log.LogDebug("Working directory: " + startInfo.WorkingDirectory);
            this.log.LogDebug("Executing: " + startInfo.FileName + " " + args.ToSensitiveString());

            var execOps = this.execOps.Value;
            using (var process = execOps.CreateProcess(startInfo))
            {
                var outputLines = new List<string>();
                var errorLines = new List<string>();

                process.OutputDataReceived += (s, e) => { if (e?.Data != null) outputLines.Add(e.Data); };
                process.ErrorDataReceived += (s, e) => { if (e?.Data != null) errorLines.Add(e.Data); };

                process.Start();

                await process.WaitAsync(this.cancellationToken).ConfigureAwait(false);

                return new SvnClientExecutionResult(process.ExitCode ?? -1, outputLines, errorLines);
            }
        }
    }

    internal sealed class SvnPath : IPathInfo
    {
        public SvnPath(string repositoryUrl, string relativePath)
        {
            if (string.IsNullOrEmpty(repositoryUrl))
                throw new ArgumentNullException(nameof(repositoryUrl));

            this.RepositoryUrl = repositoryUrl;
            this.RepositoryRelativePath = relativePath?.TrimStart('/') ?? "";
        }

        public SvnPath(SvnPath other, string relativePath)
        {
            if (other == null)
                throw new ArgumentNullException(nameof(other));

            this.RepositoryUrl = other.RepositoryUrl;
            this.RepositoryRelativePath = other.RepositoryRelativePath.TrimEnd('/') + '/' + relativePath?.TrimStart('/');
        }

        public string RepositoryUrl { get; }
        public string RepositoryRelativePath { get; }
        public string AbsolutePath => this.RepositoryUrl.TrimEnd('/') + '/' + this.RepositoryRelativePath;
        public bool IsDirectory => this.RepositoryRelativePath.EndsWith("/");

        string IPathInfo.DisplayName => this.RepositoryRelativePath.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries).LastOrDefault() ?? "";
        string IPathInfo.FullPath => this.RepositoryRelativePath;

        public override string ToString() => this.AbsolutePath;
    }

    public sealed class SvnClientExecutionResult
    {
        public SvnClientExecutionResult(int exitCode, IList<string> outputLines, IList<string> errorLines)
        {
            this.ExitCode = exitCode;
            this.OutputLines = outputLines;
            this.ErrorLines = errorLines;
        }

        public int ExitCode { get; }
        public IList<string> OutputLines { get; }
        public IList<string> ErrorLines { get; }
    }
}
