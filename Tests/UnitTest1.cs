using DevExpress.EntityFrameworkCore.Security;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.EFCore;
using DevExpress.ExpressApp.Security;
using DevExpress.Map.Native;
using DevExpress.Persistent.BaseImpl.EF.PermissionPolicy;
using Microsoft.EntityFrameworkCore;
using System.Security.AccessControl;
using Xaf25EfCore.Testing.Module;
using Xaf25EfCore.Testing.Module.BusinessObjects;
using Xaf25EfCore.Testing.Module.Controllers;
using Xaf25EfCore.Testing.Module.DatabaseUpdate;
using static System.Net.Mime.MediaTypeNames;

namespace Tests
{
    public class Tests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void Test1()
        {
            objectSpaceProvider = new EFCoreObjectSpaceProvider<TestingEFCoreDbContext>(
                     (builder, _) => builder
                        .UseInMemoryDatabase(Guid.NewGuid().ToString())
                        .UseLazyLoadingProxies()
                        .UseChangeTrackingProxies()
                  );
            //XafTypesInfo.Instance.RegisterEntity(typeof(Customer));
            //XafTypesInfo.Instance.RegisterEntity(typeof(ApplicationUser));
            //XafTypesInfo.Instance.RegisterEntity(typeof(Customer));
            //XafTypesInfo.Instance.RegisterEntity(typeof(ApplicationUser));

          


            var UpdateOs = objectSpaceProvider.CreateObjectSpace() as EFCoreObjectSpace;

            UpdateOs.DbContext.Database.EnsureCreated();

            Exception exception;
            var State = objectSpaceProvider.CheckDatabaseSchemaCompatibility(out exception);

            //Create an instance of the test application (this application is a core application and is not bound to any U.I platform)
            application = new TestApplication();


            TestingModule xafFunctionalTestModule = new TestingModule();
            application.Modules.Add(xafFunctionalTestModule);

            application.Setup("TestApplication", objectSpaceProvider);







            //TODO require register the service provider
            //Create a instance of the updater from the agnostic module
            //Create an object space provider that is not secure
            //var UpdaterObjectSpace = objectSpaceProvider.CreateUpdatingObjectSpace(true);
            //Updater updater = new Updater(UpdaterObjectSpace, null);
            //updater.UpdateDatabaseBeforeUpdateSchema();
            //updater.UpdateDatabaseAfterUpdateSchema();



            var ObjectSpace = objectSpaceProvider.CreateObjectSpace() as EFCoreObjectSpace;

            ObjectSpace.DbContext.Database.EnsureCreated();

            var userAdmin = ObjectSpace.CreateObject<ApplicationUser>();
            userAdmin.UserName = "Admin";
            // Set a password if the standard authentication type is used
            userAdmin.SetPassword("123456");

            // The UserLoginInfo object requires a user object Id (Oid).
            // Commit the user object to the database before you create a UserLoginInfo object. This will correctly initialize the user key property.
            ObjectSpace.CommitChanges(); //This line persists created object(s).
            ((ISecurityUserWithLoginInfo)userAdmin).CreateUserLoginInfo(SecurityDefaults.PasswordAuthentication, ObjectSpace.GetKeyValueAsString(userAdmin));
            // If a role with the Administrators name doesn't exist in the database, create this role.
            PermissionPolicyRole adminRole = ObjectSpace.FirstOrDefault<PermissionPolicyRole>(r => r.Name == "Administrators");
            if (adminRole == null)
            {
                adminRole = ObjectSpace.CreateObject<PermissionPolicyRole>();
                adminRole.Name = "Administrators";
            }
            adminRole.IsAdministrative = true;
            userAdmin.Roles.Add(adminRole);
            ObjectSpace.CommitChanges(); //This line persists created object(s).



            application.Security =  Login("Admin", "123456", ObjectSpace);



            var customerController = application.CreateController<CustomerController>();

            var CustomerOs = application.CreateObjectSpace(typeof(Customer));
            var customer = CustomerOs.CreateObject<Customer>();
            customer.Name = "Test Customer";
            customer.Active = false;
             var CustomerDetailView= application.CreateDetailView(CustomerOs, customer, true);

            customerController.SetView(CustomerDetailView);

            customerController.SetCustomerActive.DoExecute();

            Assert.That(customer.Active);

          
        }
        [TearDown]
        public void TearDown()
        {
            objectSpaceProvider?.Dispose();
            application?.Dispose();
            
        }
        EFCoreObjectSpaceProvider<TestingEFCoreDbContext>? objectSpaceProvider;
        SecuredEFCoreObjectSpaceProvider<TestingEFCoreDbContext> secureObjectSpaceProvider;
        TestApplication application;

        private SecurityStrategyComplex Login(string userName, string password, IObjectSpace LoginObjectSpace)
        {
            
            AuthenticationStandard authentication = new AuthenticationStandard();
            var security = new SecurityStrategyComplex(typeof(ApplicationUser), typeof(PermissionPolicyRole), authentication);
            security.RegisterEFCoreAdapterProviders(objectSpaceProvider);



            authentication.SetLogonParameters(new AuthenticationStandardLogonParameters(userName, password));
            security.Logon(LoginObjectSpace);
            var SecureEfProvider = new SecuredEFCoreObjectSpaceProvider<TestingEFCoreDbContext>(security,
             XafTypesInfo.Instance, null,
             (builder, connectionString) => builder.UseSqlServer(connectionString)
                     );


           
            return security;
        }
    }
}
