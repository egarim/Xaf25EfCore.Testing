using DevExpress.ExpressApp;
using DevExpress.ExpressApp.EFCore;
using Xaf25EfCore.Testing.Blazor.Server;
using Xaf25EfCore.Testing.Module.BusinessObjects;

namespace Tests.Infrastructure
{
    public class XafEnvironment : IDisposable
    {
        public IServiceProvider ServiceProvider { get; set; }
        public TestingBlazorApplication XafApplication { get; set; }
        public IObjectSpaceProvider ObjectSpaceProvider { get; set; }

        public void Dispose()
        {
            (ObjectSpaceProvider as IDisposable)?.Dispose();
            XafApplication?.Dispose();

            if (ServiceProvider is IDisposable disposable)
            {
                disposable.Dispose();
                Console.WriteLine("Service provider disposed");
            }
        }
    }
}