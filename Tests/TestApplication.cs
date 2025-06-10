using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Layout;
using System;

namespace Tests
{
    class TestApplication : XafApplication
    {
        protected override LayoutManager CreateLayoutManagerCore(bool simple)
        {
            return null;
        }
    }
}
