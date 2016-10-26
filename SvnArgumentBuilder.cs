using System.Collections.Generic;
using System.Linq;

namespace Inedo.BuildMasterExtensions.Subversion
{
    internal sealed class SvnArgumentBuilder
    {
        private List<SvnArg> arguments = new List<SvnArg>(16);

        public SvnArgumentBuilder()
        {
        }

        public void Append(string arg) => this.arguments.Add(new SvnArg(arg, false, false));
        public void AppendQuoted(string arg) => this.arguments.Add(new SvnArg(arg, true, false));
        public void AppendSensitive(string arg) => this.arguments.Add(new SvnArg(arg, true, true));

        public override string ToString() => string.Join(" ", this.arguments);
        public string ToSensitiveString() => string.Join(" ", this.arguments.Select(a => a.ToSensitiveString()));

        private sealed class SvnArg
        {
            private bool quoted;
            private bool sensitive;
            private string arg;

            public SvnArg(string arg, bool quoted, bool sensitive)
            {
                this.arg = arg ?? "";
                this.quoted = quoted;
                this.sensitive = sensitive;
            }

            public override string ToString()
            {
                if (this.quoted)
                    return '"' + this.arg.Replace("\"", @"\""") + '"';
                else
                    return this.arg;
            }

            public string ToSensitiveString()
            {
                if (this.sensitive)
                    return "(hidden)";
                else if (this.quoted)
                    return '"' + this.arg.Replace("\"", @"\""") + '"';
                else
                    return this.arg;
            }
        }
    }
}
