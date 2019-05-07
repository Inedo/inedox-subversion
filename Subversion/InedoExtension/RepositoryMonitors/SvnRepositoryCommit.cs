using System;
using System.Collections.Generic;
using Inedo.ExecutionEngine;
using Inedo.Extensibility.RepositoryMonitors;
using Inedo.Serialization;

namespace Inedo.Extensions.Subversion.RepositoryMonitors
{
    [Serializable]
    internal sealed class SvnRepositoryCommit : RepositoryCommit
    {
        [Persistent]
        public string Revision { get; set; }

        public override bool Equals(RepositoryCommit other)
        {
            if (!(other is SvnRepositoryCommit svnCommit))
                return false;

            return string.Equals(this.Revision, svnCommit.Revision, StringComparison.OrdinalIgnoreCase);
        }
        public override int GetHashCode() => StringComparer.OrdinalIgnoreCase.GetHashCode(this.Revision);

        public override string GetFriendlyDescription() => this.ToString();

        public override string ToString() => this.Revision ?? string.Empty;

        public override IReadOnlyDictionary<RuntimeVariableName, RuntimeValue> GetRuntimeVariables()
        {
            return new Dictionary<RuntimeVariableName, RuntimeValue>()
            {
                [new RuntimeVariableName("RevisionNumber", RuntimeValueType.Scalar)] = this.Revision
            };
        }
    }
}
