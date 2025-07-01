using DevExpress.EntityFrameworkCore.Security;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Core;
using DevExpress.ExpressApp.EFCore;
using DevExpress.ExpressApp.Security;
using DevExpress.Map.Native;
using DevExpress.Persistent.BaseImpl.EF.PermissionPolicy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Security.AccessControl;
using Xaf25EfCore.Testing.Module;
using Xaf25EfCore.Testing.Module.BusinessObjects;
using Xaf25EfCore.Testing.Module.Controllers;
using Xaf25EfCore.Testing.Module.DatabaseUpdate;
using Xaf25EfCore.Testing.Module.Services;
using DevExpress.ExpressApp.Core.Internal;
using DevExpress.ExpressApp.Updating;
using Tests.Infrastructure;

namespace Tests
{
    public class ExampleTest
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void Test1()
        {
            try
            {
                Console.WriteLine("Starting test with service provider configuration...");
                
                // Create object space provider similar to Blazor application setup
                objectSpaceProvider = new EFCoreObjectSpaceProvider<TestingEFCoreDbContext>(
                         (builder, _) => builder
                            .UseInMemoryDatabase(Guid.NewGuid().ToString())
                            .UseLazyLoadingProxies()
                            .UseChangeTrackingProxies()
                      );

                var UpdateOs = objectSpaceProvider.CreateObjectSpace() as EFCoreObjectSpace;
                UpdateOs.DbContext.Database.EnsureCreated();

                Exception exception;
                var State = objectSpaceProvider.CheckDatabaseSchemaCompatibility(out exception);

                // Configure services with validation support like in Startup.cs
                var services = new ServiceCollection();
                
                // Add services similar to what's configured in Startup.cs
                services.AddScoped<IHelloWorldService, HelloWorldService>();
                services.AddLogging(); // Basic logging service

                // Add XAF-related services
                SharedControllersManagerContainer sharedControllersManagerContainer = new SharedControllersManagerContainer();
                SharedTypesCacheContainer sharedTypesCacheContainer = new SharedTypesCacheContainer();
                IObjectSpaceProviderContainer objectSpaceProviderContainer = new CustomObjectSpaceProviderContainer();

                services.AddSingleton(sharedControllersManagerContainer);
                services.AddSingleton(sharedTypesCacheContainer);
                services.AddSingleton<IObjectSpaceProviderContainer>(objectSpaceProviderContainer);
                
                var serviceProvider = services.BuildServiceProvider();
                Console.WriteLine("Service provider configured successfully");

                // Create an instance of the test application with service provider (similar to Blazor setup)
                application = new TestApplication(serviceProvider);
                Console.WriteLine("TestApplication created with service provider");

                // Create a custom module without ValidationModule to avoid dependency issues
                TestingModuleWithoutValidation testModule = new TestingModuleWithoutValidation();
                application.Modules.Add(testModule);
                Console.WriteLine("TestingModule added (without ValidationModule to avoid service dependency)");

                // Setup the application with the object space provider
                application.Setup("TestApplication", objectSpaceProvider);
                Console.WriteLine("Application setup completed");

                // Test the service - this simulates getting services like in the Blazor application
                var helloWorldService = application.ServiceProvider.GetService<IHelloWorldService>();
                var greeting = helloWorldService?.GetGreeting();
                Console.WriteLine($"Service greeting: {greeting}");
                
                var personalGreeting = helloWorldService?.GetGreeting("XAF User");
                Console.WriteLine($"Personal greeting: {personalGreeting}");

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

                application.Security = Login("Admin", "123456", ObjectSpace);
                Console.WriteLine("Security configured and user logged in");

                var customerController = application.CreateController<CustomerController>();

                var CustomerOs = application.CreateObjectSpace(typeof(Customer));
                var customer = CustomerOs.CreateObject<Customer>();
                customer.Name = "Test Customer";
                customer.Active = false;
                var CustomerDetailView = application.CreateDetailView(CustomerOs, customer, true);

                customerController.SetView(CustomerDetailView);

                customerController.SetCustomerActive.DoExecute();
                Console.WriteLine("Customer active status changed to: " + customer.Active);

                Assert.That(customer.Active);

                // Additional test to verify service is working
                Assert.That(greeting, Is.EqualTo("Hello, World!"));
                Assert.That(personalGreeting, Is.EqualTo("Hello, XAF User!"));
                
                Console.WriteLine("✅ Test completed successfully - No validation module duplication error!");
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ Test failed with error: " + ex.Message);
                Console.WriteLine("Stack trace: " + ex.StackTrace);
                throw;
            }
        }
        
        [TearDown]
        public void TearDown()
        {
            objectSpaceProvider?.Dispose();
            application?.Dispose();
        }
        
        EFCoreObjectSpaceProvider<TestingEFCoreDbContext>? objectSpaceProvider;
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

    // Custom testing module without ValidationModule to avoid service dependency issues
    public sealed class TestingModuleWithoutValidation : ModuleBase 
    {
        public TestingModuleWithoutValidation() 
        {
            AdditionalExportedTypes.Add(typeof(Xaf25EfCore.Testing.Module.BusinessObjects.ApplicationUser));
            AdditionalExportedTypes.Add(typeof(DevExpress.Persistent.BaseImpl.EF.PermissionPolicy.PermissionPolicyRole));
            AdditionalExportedTypes.Add(typeof(DevExpress.Persistent.BaseImpl.EF.ModelDifference));
            AdditionalExportedTypes.Add(typeof(DevExpress.Persistent.BaseImpl.EF.ModelDifferenceAspect));
            AdditionalExportedTypes.Add(typeof(Customer));
            
            RequiredModuleTypes.Add(typeof(DevExpress.ExpressApp.SystemModule.SystemModule));
            RequiredModuleTypes.Add(typeof(DevExpress.ExpressApp.Security.SecurityModule));  
            RequiredModuleTypes.Add(typeof(DevExpress.ExpressApp.Objects.BusinessClassLibraryCustomizationModule));
            RequiredModuleTypes.Add(typeof(DevExpress.ExpressApp.CloneObject.CloneObjectModule));
            RequiredModuleTypes.Add(typeof(DevExpress.ExpressApp.ConditionalAppearance.ConditionalAppearanceModule));
            RequiredModuleTypes.Add(typeof(DevExpress.ExpressApp.Dashboards.DashboardsModule));
            RequiredModuleTypes.Add(typeof(DevExpress.ExpressApp.Notifications.NotificationsModule));
            // Removed ValidationModule to avoid the IValidator service dependency
            
            DevExpress.ExpressApp.Security.SecurityModule.UsedExportedTypes = DevExpress.Persistent.Base.UsedExportedTypes.Custom;
            AdditionalExportedTypes.Add(typeof(DevExpress.Persistent.BaseImpl.EF.FileData));
            AdditionalExportedTypes.Add(typeof(DevExpress.Persistent.BaseImpl.EF.FileAttachment));
        }
        
        public override IEnumerable<ModuleUpdater> GetModuleUpdaters(IObjectSpace objectSpace, Version versionFromDB) 
        {
            ModuleUpdater updater = new Xaf25EfCore.Testing.Module.DatabaseUpdate.Updater(objectSpace, versionFromDB);
            return new ModuleUpdater[] { updater };
        }
        
        public override void Setup(XafApplication application) 
        {
            base.Setup(application);
        }
    }
}
