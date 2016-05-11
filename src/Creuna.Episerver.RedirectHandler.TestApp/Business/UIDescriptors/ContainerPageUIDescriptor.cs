using EPiServer.Editor;
using EPiServer.Shell;
using Creuna.Episerver.RedirectHandler.TestApp.Models.Pages;

namespace Creuna.Episerver.RedirectHandler.TestApp.Business.UIDescriptors
{
    /// <summary>
    /// Describes how the UI should appear for <see cref="ContainerPage"/> content.
    /// </summary>
    [UIDescriptorRegistration]
    public class ContainerPageUIDescriptor : UIDescriptor<ContainerPage>
    {
        public ContainerPageUIDescriptor()
            : base(ContentTypeCssClassNames.Container)
        {
            DefaultView = CmsViewNames.AllPropertiesView;
        }
    }
}
