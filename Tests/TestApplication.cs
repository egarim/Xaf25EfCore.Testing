using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Layout;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Tests
{
    class TestApplication : XafApplication
    {
        public TestApplication(IServiceProvider serviceProvider = null)
        {
            ServiceProvider = serviceProvider;
        }

        protected override LayoutManager CreateLayoutManagerCore(bool simple)
        {
            return null;
        }

       
    }
}
