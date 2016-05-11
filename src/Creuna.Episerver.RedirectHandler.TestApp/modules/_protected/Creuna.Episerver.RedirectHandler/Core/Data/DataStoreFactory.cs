using System;
using EPiServer.Data.Dynamic;
using EPiServer.ServiceLocation;

namespace Creuna.Episerver.RedirectHandler.Core.Data
{
    public interface IDataStoreFactory
    {
        DynamicDataStore GetStore(Type t);
    }

    [ServiceConfiguration(typeof(IDataStoreFactory), Lifecycle = ServiceInstanceScope.Singleton)]
    public class DefaultDataStoreFactory : IDataStoreFactory
    {
        public virtual DynamicDataStore GetStore(Type t)
        {
            // GetStore will only return null the first time this method is called for a Type
            // In that case the ?? C# operator will call CreateStore
            // EPiServer.Data.Dynamic.DynamicDataStoreFactory.Instance.DeleteStore(t,true);
            return DynamicDataStoreFactory.Instance.GetStore(t) ??
                   DynamicDataStoreFactory.Instance.CreateStore(t);
        }
    }
}