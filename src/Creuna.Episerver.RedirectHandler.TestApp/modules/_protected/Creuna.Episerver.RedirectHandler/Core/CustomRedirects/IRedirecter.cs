using System;

namespace Creuna.Episerver.RedirectHandler.Core.CustomRedirects
{
    public interface IRedirecter
    {
        RedirectAttempt Redirect(string referer, Uri urlNotFound);
    }
}
