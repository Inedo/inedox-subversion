﻿using System;
using System.Collections.Generic;
using Inedo.Documentation;
using Inedo.ExecutionEngine;
using Inedo.Extensibility.RepositoryMonitors;
using Inedo.Extensibility.ResourceMonitors;
using Inedo.Serialization;

namespace Inedo.Extensions.Subversion.RepositoryMonitors
{
    [Serializable]
    public sealed class SvnRepositoryState : ResourceMonitorState
    {
        [Persistent]
        public string Revision { get; set; }

        public override bool Equals(ResourceMonitorState other)
        {
            if (other is not SvnRepositoryState svnCommit)
                return false;

            return string.Equals(this.Revision, svnCommit.Revision, StringComparison.OrdinalIgnoreCase);
        }

        public override RichDescription GetDescription() => new (this.Revision ?? string.Empty);
    }
}
