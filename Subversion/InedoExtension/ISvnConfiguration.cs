using System.Security;
using Inedo.Extensibility.Credentials;
using Inedo.Extensibility.Operations;
using Inedo.Extensibility.RepositoryMonitors;
using Inedo.Extensibility.SecureResources;
using Inedo.Extensions.Subversion.Credentials;
using UsernamePasswordCredentials = Inedo.Extensions.Credentials.UsernamePasswordCredentials;

namespace Inedo.Extensions.Subversion
{
    internal interface ISvnConfiguration
    {
        string ResourceName { get; }
        string RepositoryUrl { get; }
        string UserName { get; }
        SecureString Password { get; }
    }

    internal static class SvnExtensions
    {
        public static (UsernamePasswordCredentials, SubversionSecureResource) GetCredentialsAndResource(this ISvnConfiguration config, IOperationExecutionContext context)
            => config.GetCredentialsAndResource(context as ICredentialResolutionContext);
        public static (UsernamePasswordCredentials, SubversionSecureResource) GetCredentialsAndResource(this ISvnConfiguration config, IRepositoryMonitorContext context)
            => config.GetCredentialsAndResource(context as ICredentialResolutionContext);

        public static (UsernamePasswordCredentials, SubversionSecureResource) GetCredentialsAndResource(this ISvnConfiguration config, ICredentialResolutionContext context)
        {
            UsernamePasswordCredentials credentials = null;
            SubversionSecureResource resource = null;
            if (!string.IsNullOrEmpty(config.ResourceName))
            {
                resource = (SubversionSecureResource)SecureResource.TryCreate(config.ResourceName, context);
                if (resource == null)
                {
                    var rc = SecureCredentials.TryCreate(config.ResourceName, context) as SubversionLegacyCredentials;
                    resource = (SubversionSecureResource)rc?.ToSecureResource();
                    credentials = (UsernamePasswordCredentials)rc?.ToSecureCredentials();
                }
                else
                {
                    credentials = (UsernamePasswordCredentials)resource.GetCredentials(context);
                }
            }

            return (
                string.IsNullOrEmpty(AH.CoalesceString(config.UserName, credentials?.UserName)) ? null : new UsernamePasswordCredentials
                {
                    UserName = AH.CoalesceString(config.UserName, credentials?.UserName),
                    Password = config.Password ?? credentials?.Password
                },
                new SubversionSecureResource
                {
                    RepositoryUrl = config.RepositoryUrl ?? resource.RepositoryUrl
                }
            );
        }
    }
}
