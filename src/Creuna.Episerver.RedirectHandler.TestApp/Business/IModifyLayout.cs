using Creuna.Episerver.RedirectHandler.TestApp.Models.ViewModels;

namespace Creuna.Episerver.RedirectHandler.TestApp.Business
{
    /// <summary>
    /// Defines a method which may be invoked by PageContextActionFilter allowing controllers
    /// to modify common layout properties of the view model.
    /// </summary>
    interface IModifyLayout
    {
        void ModifyLayout(LayoutModel layoutModel);
    }
}
