using Inedo.BuildMaster.Extensibility.Providers.SourceControl;
using Inedo.BuildMaster.Web.Controls;
using Inedo.BuildMaster.Web.Controls.Extensions;
using Inedo.Web.Controls;

namespace Inedo.BuildMasterExtensions.Subversion
{
    internal sealed class SubversionRepositoryEditor : RepositoryEditorBase
    {
        ValidatingTextBox txtRepositoryPath, txtSvnRepositoryName;
        protected override void CreateChildControls()
        {
            txtRepositoryPath = new ValidatingTextBox{ Width = 300 };
            
            txtSvnRepositoryName = new ValidatingTextBox { ID="txtSvnRepositoryName", Width = 300 };

            PreRender += (s, e) => { CUtil.GetJQuery(Page).IncludeInedoDefaulter = true; };

            CUtil.Add(this,
                new StandardFormField("Relative Repository Path:", txtRepositoryPath),
                new StandardFormField("Repository Name:", txtSvnRepositoryName),
                new RenderJQueryDocReadyDelegator(w =>
                    {
                        w.WriteLine("$('#" + txtSvnRepositoryName.ClientID + "').inedobm_defaulter({defaultText:'optional'});");
                    })
                );
        }
        
        public override void BindToForm(RepositoryBase extension)
        {
            EnsureChildControls();
            txtSvnRepositoryName.Text = ((SubversionRepository)extension).SvnRepositoryName;
            txtRepositoryPath.Text = ((SubversionRepository)extension).RepositoryPath;
        }

        public override string DescriptionHeading { get { return "Repositories (Optional)"; } }
        public override string DescriptionContent 
        { get { 
            return 
                "If multiple repositories are set-up on the Subversion server (and you want to be able to browse these " +
                "repositories through the web interface), each repository must be defined here." +
                "<br /><br /> Repository Name cannot contain a <code>" + "/" + "</code> character."; 
        } }

        public override RepositoryBase CreateFromForm()
        {
            EnsureChildControls(); 
            return new SubversionRepository
            {
                RepositoryPath = txtRepositoryPath.Text,
                SvnRepositoryName = txtSvnRepositoryName.Text
            };
        }
    }
}
