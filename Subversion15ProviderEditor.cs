using System;
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
        private SourceControlFileFolderPicker txtPrivateKeyPath;
        private CheckBox chkUseUpdateInsteadOfExport;
        private CheckBox chkUseSSH;
        private SourceControlFileFolderPicker txtExePath;

        public Subversion15ProviderEditor()
        {
            this.ValidateBeforeSave += SubversionProviderEditorBase_ValidateBeforeSave;
        }

        private void SubversionProviderEditorBase_ValidateBeforeSave(object sender, ValidationEventArgs<ProviderBase> e)
        {
            var prov = (Subversion15Provider)e.Extension;
            
            var badRepo = prov.Repositories.FirstOrDefault(repo => repo.SvnRepositoryName.Contains("/"));
            if (badRepo != null)
            {
                e.Message = "Repository " + badRepo.SvnRepositoryName + " is invalid because it contains the " + "/" + " character.";
                e.ValidLevel = ValidationLevels.Error;
                return;
            }

            //var agent = AgentHelper.CreateNewRemoteProxy(this.EditorContext.ServerId);

            //if (!string.IsNullOrEmpty(this.txtExePath.Text) && !agent.FileExists(this.txtExePath.Text))
            //{
            //    e.Message = "An SVN client was not found at " + this.txtExePath.Text + Environment.NewLine + "This may result in build errors.";
            //    e.ValidLevel = ValidationLevels.Warning;
            //    return;
            //}
        }

        protected override void CreateChildControls()
        {
            this.txtUsername = new ValidatingTextBox() { Width = 300 };

            this.txtPassword = new PasswordTextBox() { Width = 250 };

            this.txtPrivateKeyPath = new SourceControlFileFolderPicker()
            {
                ServerId = EditorContext.ServerId,
                DisplayMode = SourceControlBrowser.DisplayModes.FoldersAndFiles
            };

            this.chkUseSSH = new CheckBox() 
            { 
                Text = "Use SSH",
                ID = "chkUseSSH"
            };

            this.txtRepositoryRoot = new ValidatingTextBox() { Width = 300, Required = true };

            this.chkUseUpdateInsteadOfExport = new CheckBox()
            {
                Text = "Use SVN UPDATE instead of EXPORT for this provider"
            };

            this.txtExePath = new SourceControlFileFolderPicker()
            {
                ServerId = EditorContext.ServerId,
                DisplayMode = SourceControlBrowser.DisplayModes.FoldersAndFiles
            };

            var ctlPrivateKey = new StandardFormField("Private Key Path:", this.txtPrivateKeyPath) { ID = "ctlPrivateKey" };

            this.Controls.Add(
                new FormFieldGroup("Subversion Options",
                    "The following fields are used to connect to Subversion. If your server requires a custom SSH tunnel that is not already configured in the Subversion config file, check the \"Use SSH\" option. For more information, see our <a href=\"http://inedo.com/support/kb/1061/connecting-buildmaster-to-subversion-over-ssh\" target=\"_blank\">knowledge base article</a> on connecting via SSH.",
                    false,
                    new StandardFormField("Subversion URL:", this.txtRepositoryRoot),
                    new StandardFormField("", this.chkUseSSH)
                    ),
                new FormFieldGroup("Authentication",
                    "Specify the credentials used to authenticate with the Subversion server, or leave blank for anonymous access. If using private key authentication, the password field is ignored and the private key itself must not have a password.",
                    false,
                    new StandardFormField("Username:", this.txtUsername),
                    new StandardFormField("Password:", this.txtPassword),
                    ctlPrivateKey
                    ),
                new FormFieldGroup("Subversion Client Options",
                    "When running on Windows agents, a path to a SVN client is not needed and can be left blank. On other platforms, or if you want to use your own SVN client, the path to the SVN client must be specified here.",
                    false,
                    new StandardFormField("", this.chkUseUpdateInsteadOfExport),
                    new StandardFormField("Path to svn client:", this.txtExePath)
                    )
                );

            this.Controls.Add(
                new RenderJQueryDocReadyDelegator(w =>
                {
                    w.Write("$('#" + this.chkUseSSH.ClientID + "').change(function(){" +
                           "if ($('#" + this.chkUseSSH.ClientID + "').is(':checked')) { $('#" + ctlPrivateKey.ClientID + "').show(); } else { $('#" + ctlPrivateKey.ClientID + "').hide(); }  " +
                    "}).change();");
                })
            );
            
            base.CreateChildControls();
        }

        public override ProviderBase CreateFromForm()
        {
            EnsureChildControls();

            return new Subversion15Provider() 
            {
                Username = this.txtUsername.Text,
                Password = this.txtPassword.Text,
                RepositoryRoot = this.txtRepositoryRoot.Text,
                ExePath = this.txtExePath.Text,
                UseUpdateInsteadOfExport = this.chkUseUpdateInsteadOfExport.Checked,
                PrivateKeyPath = this.txtPrivateKeyPath.Text,
                UseSSH = this.chkUseSSH.Checked
            };
        }

        public override void BindToForm(ProviderBase provider)
        {
            EnsureChildControls();

            var svnProvider = (Subversion15Provider)provider;
            this.txtUsername.Text = svnProvider.Username;
            this.txtPassword.Text = svnProvider.Password;
            this.txtRepositoryRoot.Text = svnProvider.RepositoryRoot;
            this.chkUseUpdateInsteadOfExport.Checked = svnProvider.UseUpdateInsteadOfExport;
            this.txtExePath.Text = svnProvider.ExePath ?? string.Empty;
            this.chkUseSSH.Checked = svnProvider.UseSSH;
        }
    }
}
