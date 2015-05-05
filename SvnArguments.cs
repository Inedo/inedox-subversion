using System;
using System.Text;

namespace Inedo.BuildMasterExtensions.Subversion
{
    internal sealed class SvnArguments
    {
        private Lazy<string> argumentString;
        private Subversion15Provider provider;
        private string[] args;

        public SvnArguments(Subversion15Provider provider, params string[] args)
        {
            this.QuoteArguments = true;
            this.provider = provider;
            this.args = args;

            this.argumentString = new Lazy<string>(this.BuildArguments);
        }

        public string Value { get { return this.argumentString.Value; } }
        public bool ObscurePassword { get; set; }
        public bool QuoteArguments { get; set; }

        public override string ToString()
        {
            return this.Value;
        }

        private string BuildArguments()
        {
            var argBuffer = new StringBuilder();

            foreach (var arg in this.args)
            {
                if (this.QuoteArguments)
                    argBuffer.AppendFormat("\"{0}\" ", arg);
                else
                    argBuffer.AppendFormat("{0} ", arg);
            }

            argBuffer.Append("--non-interactive --trust-server-cert ");

            if (!string.IsNullOrEmpty(provider.Username))
                argBuffer.AppendFormat("--username \"{0}\" ", provider.Username);
            if (!string.IsNullOrEmpty(provider.Password))
                argBuffer.AppendFormat("--password \"{0}\" ", this.ObscurePassword ? "xxxxx" : provider.Password);

            if (provider.UseSSH)
            {
                // --config-option=config:tunnels:ssh="plink.exe -batch -i /path/to/private-key.ppk"
                argBuffer.AppendFormat(@"--config-option=config:tunnels:ssh=""{0} -batch", provider.PlinkExePath);
                if (!string.IsNullOrEmpty(provider.PrivateKeyPath))
                    argBuffer.AppendFormat(" -i {0}", provider.SafePrivateKeyPath);
                argBuffer.Append("\"");
            }

            return argBuffer.ToString();
        }
    }
}
