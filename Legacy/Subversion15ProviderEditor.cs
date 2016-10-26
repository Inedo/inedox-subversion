using System.Linq;
using System.Web.UI.WebControls;
using Inedo.BuildMaster.Extensibility.Providers;
using Inedo.BuildMaster.Web.Controls;
using Inedo.BuildMaster.Web.Controls.Extensions;
using Inedo.Web.Controls;

namespace Inedo.BuildMasterExtensions.Subversion
{
    internal sealed class Subversion15ProviderEditor : ProviderEditorBase
    {
        private ValidatingTextBox txtUsername, txtRepositoryRoot;
        private PasswordTextBox txtPassword;
        private FileBrowserTextBox txtPrivateKeyPath;
        private CheckBox chkUseSSH;
        private FileBrowserTextBox txtExePath;

        public Subversion15ProviderEditor()
        {
            this.ValidateBeforeSave += SubversionProviderEditorBase_ValidateBeforeSave;
        }

        private void SubversionProviderEditorBase_ValidateBeforeSave(object sender, ValidationEventArgs<ProviderBase> e)
        {
            var prov = (Subversion15Provider)e.Extension;
            if (prov.Repositories != null)
            {
                var badRepo = prov.Repositories.FirstOrDefault(repo => repo.Name.Contains("/"));
                if (badRepo != null)
                {
                    e.Message = "Repository " + badRepo.Name + " is invalid because it contains the \"/\" character.";
                    e.ValidLevel = ValidationLevel.Error;
                    return;
                }
            }
        }

        protected override void CreateChildControls()
        {
            this.txtUsername = new ValidatingTextBox { DefaultText = "anonymous" };

            this.txtPassword = new PasswordTextBox();

            this.txtPrivateKeyPath = new FileBrowserTextBox
            {
                ServerId = EditorContext.ServerId,
                IncludeFiles = true
            };

            this.chkUseSSH = new CheckBox { Text = "Use SSH", ID = "chkUseSSH" };

            this.txtRepositoryRoot = new ValidatingTextBox { Required = true };

            this.txtExePath = new FileBrowserTextBox
            {
                DefaultText = "Use bundled client",
                ServerId = EditorContext.ServerId,
                IncludeFiles = true
            };

            var ctlPrivateKey = new SlimFormField("Private key path:", this.txtPrivateKeyPath)
            {
                HelpText = "For private key authentication, the password field is ignored and the private key itself must not have a password.",
                ID = "ctlPrivateKey"
            };

            this.Controls.Add(
                new SlimFormField("Repository root URL:", this.txtRepositoryRoot),
                new SlimFormField("SSH:", this.chkUseSSH)
                {
                    HelpText = new LiteralHtml("If your server requires a custom SSH tunnel that is not already configured in the Subversion config file, check the \"Use SSH\" option. For more information, see our <a href=\"http://inedo.com/support/kb/1061/connecting-buildmaster-to-subversion-over-ssh\" target=\"_blank\">knowledge base article</a> on connecting via SSH.", false)
                },
                new SlimFormField("Username:", this.txtUsername),
                new SlimFormField("Password:", this.txtPassword),
                ctlPrivateKey,
                new SlimFormField("Subversion client:", this.txtExePath)
                {
                    HelpText = "When running on Windows agents, a path to a SVN client is not needed and can be left blank. On other platforms, or if you want to use your own SVN client, the path to the SVN client must be specified here."
                }
            );

            this.Controls.BindVisibility(this.chkUseSSH, ctlPrivateKey);
        }

        public override ProviderBase CreateFromForm()
        {
            return new Subversion15Provider
            {
                Username = this.txtUsername.Text,
                Password = this.txtPassword.Text,
                RepositoryRoot = this.txtRepositoryRoot.Text,
                ExePath = this.txtExePath.Text,
                PrivateKeyPath = this.txtPrivateKeyPath.Text,
                UseSSH = this.chkUseSSH.Checked
            };
        }

        public override void BindToForm(ProviderBase provider)
        {
            var svnProvider = (Subversion15Provider)provider;
            this.txtUsername.Text = svnProvider.Username;
            this.txtPassword.Text = svnProvider.Password;
            this.txtRepositoryRoot.Text = svnProvider.RepositoryRoot;
            this.txtExePath.Text = svnProvider.ExePath ?? string.Empty;
            this.chkUseSSH.Checked = svnProvider.UseSSH;
        }
    }
}
