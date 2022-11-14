using System.Security;
using Inedo.Extensibility.Credentials;
using Inedo.Extensibility.Operations;
using Inedo.Extensibility.SecureResources;
using Inedo.Extensions.Credentials;
using Inedo.Extensions.Subversion.Credentials;

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

        public static (UsernamePasswordCredentials, SubversionSecureResource) GetCredentialsAndResource(this ISvnConfiguration config, ICredentialResolutionContext context)
        {
            UsernamePasswordCredentials credentials = null;
            SubversionSecureResource resource = null;
            if (!string.IsNullOrEmpty(config.ResourceName))
            {
                resource = (SubversionSecureResource)SecureResource.TryCreate(config.ResourceName, context);
                credentials = (UsernamePasswordCredentials)resource.GetCredentials(context);
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
