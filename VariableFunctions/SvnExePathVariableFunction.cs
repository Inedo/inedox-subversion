using System.ComponentModel;
using Inedo.BuildMaster.Extensibility;
using Inedo.BuildMaster.Extensibility.VariableFunctions;
using Inedo.Documentation;

namespace Inedo.BuildMasterExtensions.Subversion.VariableFunctions
{
    [ScriptAlias("SvnExePath")]
    [Description("an overridden path to svn.exe on the server; if not supplied, the embedded svn.exe client will be used")]
    [Tag("svn")]
    [ExtensionConfigurationVariable(Required = false)]
    public sealed class SvnExePathVariableFunction : ScalarVariableFunction
    {
        protected override object EvaluateScalar(IGenericBuildMasterContext context)
        {
            return string.Empty;
        }
    }
}
