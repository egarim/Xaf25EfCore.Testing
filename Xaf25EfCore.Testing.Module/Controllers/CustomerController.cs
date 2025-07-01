using DevExpress.Data.Filtering;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.Layout;
using DevExpress.ExpressApp.Model.NodeGenerators;
using DevExpress.ExpressApp.SystemModule;
using DevExpress.ExpressApp.Templates;
using DevExpress.ExpressApp.Utils;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.Validation;
using DevExpress.XtraRichEdit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xaf25EfCore.Testing.Module.BusinessObjects;
using Xaf25EfCore.Testing.Module.Services;

namespace Xaf25EfCore.Testing.Module.Controllers
{
    // For more typical usage scenarios, be sure to check out https://docs.devexpress.com/eXpressAppFramework/DevExpress.ExpressApp.ViewController.
    public partial class CustomerController : ViewController
    {
        public IHelloWorldService helloWorldService;
        public SimpleAction SetCustomerActive;
        // Use CodeRush to create Controllers and Actions with a few keystrokes.
        // https://docs.devexpress.com/CodeRushForRoslyn/403133/
        public CustomerController()
        {
            InitializeComponent();

            this.TargetObjectType = typeof(Customer);
            this.TargetViewType = ViewType.DetailView;

            SetCustomerActive = new SimpleAction(this, "SetCustomerActive", "View");
            SetCustomerActive.Execute += SetCustomerActive_Execute;
            
            // Target required Views (via the TargetXXX properties) and create their Actions.
        }

        private void SetCustomerActive_Execute(object sender, SimpleActionExecuteEventArgs e)
        {
            var customer= this.View.CurrentObject as Customer;
            customer.Active = true;
            this.View.ObjectSpace.CommitChanges(); // Persist changes to the database.
            // Execute your business logic (https://docs.devexpress.com/eXpressAppFramework/112737/).
        }
        protected override void OnActivated()
        {
            base.OnActivated();
            this.helloWorldService = this.Application.ServiceProvider.GetService(typeof(IHelloWorldService)) as IHelloWorldService;
            // Perform various tasks depending on the target View.
        }
        protected override void OnViewControlsCreated()
        {
            base.OnViewControlsCreated();
            // Access and customize the target View control.
        }
        protected override void OnDeactivated()
        {
            // Unsubscribe from previously subscribed events and release other references and resources.
            base.OnDeactivated();
        }
    }
}
