using System.ComponentModel;
using Inedo.Documentation;
using Inedo.Extensibility;
using Inedo.Extensibility.VariableFunctions;

namespace Inedo.BuildMasterExtensions.Subversion.VariableFunctions
{
    [ScriptAlias("SvnExePath")]
    [Description("an overridden path to svn.exe on the server; if not supplied, the embedded svn.exe client will be used")]
    [Tag("svn")]
    [ExtensionConfigurationVariable(Required = false)]
    public sealed class SvnExePathVariableFunction : ScalarVariableFunction
    {
        protected override object EvaluateScalar(IVariableFunctionContext context)
        {
            return string.Empty;
        }
    }
}
