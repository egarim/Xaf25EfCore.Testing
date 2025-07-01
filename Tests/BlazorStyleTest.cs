using DevExpress.EntityFrameworkCore.Security;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.EFCore;
using DevExpress.ExpressApp.Security;
using DevExpress.Persistent.BaseImpl.EF.PermissionPolicy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xaf25EfCore.Testing.Module;
using Xaf25EfCore.Testing.Module.BusinessObjects;
using Xaf25EfCore.Testing.Module.Controllers;
using Xaf25EfCore.Testing.Module.Services;
using DevExpress.Persistent.Validation;
using DevExpress.ExpressApp.Core;
using DevExpress.ExpressApp.Core.Internal;
using Tests.Infrastructure;

namespace Tests
{
    public class BlazorStyleTest
    {
        EFCoreObjectSpaceProvider<TestingEFCoreDbContext>? objectSpaceProvider;
        TestApplication? application;

        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void Test1_BlazorStyle()
        {
            try
            {
                Console.WriteLine("Starting Blazor-style test with proper validation services...");
                
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

                // Configure services similar to Blazor Startup but manually register validation services
                var services = new ServiceCollection();
                
                // Add basic services
                services.AddLogging();
                services.AddScoped<IHelloWorldService, HelloWorldService>();

                // Register validation services manually
                services.AddScoped<IValidator>(provider => new SimpleValidator());
                
                // Register the XAF internal services exactly like in the working ExampleTest
                SharedControllersManagerContainer sharedControllersManagerContainer = new SharedControllersManagerContainer();
                SharedTypesCacheContainer sharedTypesCacheContainer = new SharedTypesCacheContainer();
                IObjectSpaceProviderContainer objectSpaceProviderContainer = new CustomObjectSpaceProviderContainer();

                services.AddSingleton(sharedControllersManagerContainer);
                services.AddSingleton(sharedTypesCacheContainer);
                services.AddSingleton<IObjectSpaceProviderContainer>(objectSpaceProviderContainer);
                
                var serviceProvider = services.BuildServiceProvider();
                Console.WriteLine("Service provider configured with validation and XAF internal services");

                // Create the test application with service provider
                application = new TestApplication(serviceProvider);
                Console.WriteLine("TestApplication created with service provider");

                // Add the original TestingModule (which includes ValidationModule)
                var testingModule = new TestingModule();
                application.Modules.Add(testingModule);
                Console.WriteLine("Original TestingModule added (includes ValidationModule)");

                // Setup the application
                application.Setup("TestApplication", objectSpaceProvider);
                Console.WriteLine("Application setup completed");

                // Test the service
                var helloWorldService = application.ServiceProvider.GetService<IHelloWorldService>();
                var greeting = helloWorldService?.GetGreeting();
                Console.WriteLine($"Service greeting: {greeting}");
                
                var personalGreeting = helloWorldService?.GetGreeting("XAF User");
                Console.WriteLine($"Personal greeting: {personalGreeting}");

                var ObjectSpace = objectSpaceProvider.CreateObjectSpace() as EFCoreObjectSpace;
                ObjectSpace.DbContext.Database.EnsureCreated();

                var userAdmin = ObjectSpace.CreateObject<ApplicationUser>();
                userAdmin.UserName = "Admin";
                userAdmin.SetPassword("123456");

                ObjectSpace.CommitChanges();
                ((ISecurityUserWithLoginInfo)userAdmin).CreateUserLoginInfo(SecurityDefaults.PasswordAuthentication, ObjectSpace.GetKeyValueAsString(userAdmin));
                
                PermissionPolicyRole adminRole = ObjectSpace.FirstOrDefault<PermissionPolicyRole>(r => r.Name == "Administrators");
                if (adminRole == null)
                {
                    adminRole = ObjectSpace.CreateObject<PermissionPolicyRole>();
                    adminRole.Name = "Administrators";
                }
                adminRole.IsAdministrative = true;
                userAdmin.Roles.Add(adminRole);
                ObjectSpace.CommitChanges();

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
                
                Console.WriteLine("✅ Blazor-style test completed successfully with original TestingModule!");
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ Blazor-style test failed with error: " + ex.Message);
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

        private SecurityStrategyComplex Login(string userName, string password, IObjectSpace LoginObjectSpace)
        {
            AuthenticationStandard authentication = new AuthenticationStandard();
            var security = new SecurityStrategyComplex(typeof(ApplicationUser), typeof(PermissionPolicyRole), authentication);
            security.RegisterEFCoreAdapterProviders(objectSpaceProvider);

            authentication.SetLogonParameters(new AuthenticationStandardLogonParameters(userName, password));
            security.Logon(LoginObjectSpace);

            return security;
        }
    }

    // Simple implementation of CustomObjectSpaceProviderContainer for testing
    public class CustomObjectSpaceProviderContainer : IObjectSpaceProviderContainer
    {
        private readonly List<IObjectSpaceProvider> _providers = new();

        public void AddObjectSpaceProvider(IObjectSpaceProvider objectSpaceProvider)
        {
            _providers.Add(objectSpaceProvider);
        }

        public void AddObjectSpaceProviders(IEnumerable<IObjectSpaceProvider> objectSpaceProviders)
        {
            _providers.AddRange(objectSpaceProviders);
        }

        public void Clear()
        {
            _providers.Clear();
        }

        public IObjectSpaceProvider GetObjectSpaceProvider(Type objectType)
        {
            return _providers.FirstOrDefault() ?? throw new InvalidOperationException("No object space provider registered");
        }

        public IEnumerable<IObjectSpaceProvider> GetObjectSpaceProviders()
        {
            return _providers;
        }

        public void Dispose()
        {
            // Dispose implementation
            foreach (var provider in _providers)
            {
                if (provider is IDisposable disposable)
                    disposable.Dispose();
            }
            _providers.Clear();
        }
    }

    // Minimal implementation of IValidator that delegates to the static Validator.RuleSet
    public class SimpleValidator : IValidator
    {
        public IRuleSet RuleSet => Validator.RuleSet;

        public RuleSetValidationResult ValidateTarget(DevExpress.ExpressApp.IObjectSpace objectSpace, object target, ContextIdentifier contextIdentifier)
        {
            return Validator.RuleSet.ValidateTarget(objectSpace, target, contextIdentifier);
        }

        public RuleSetValidationResult ValidateTarget(DevExpress.ExpressApp.IObjectSpace objectSpace, object target, ContextIdentifier contextIdentifier, bool inTransaction)
        {
            // Use the standard ValidateTarget method since the overload with bool doesn't match the RuleSet API
            return Validator.RuleSet.ValidateTarget(objectSpace, target, contextIdentifier);
        }

        public RuleSetValidationResult ValidateAllTargets(DevExpress.ExpressApp.IObjectSpace objectSpace, ContextIdentifier contextIdentifier)
        {
            // Return an empty result since we can't easily map this
            return new RuleSetValidationResult();
        }

        public RuleSetValidationResult ValidateAllTargets(DevExpress.ExpressApp.IObjectSpace objectSpace, ContextIdentifier contextIdentifier, bool inTransaction)
        {
            // Return an empty result
            return new RuleSetValidationResult();
        }

        public void ClearRuleSetValidationResult()
        {
            // Implementation not needed for testing
        }

        public RuleSetValidationResult RuleSetValidationResult 
        { 
            get 
            { 
                // Return an empty validation result
                return new RuleSetValidationResult();
            } 
        }
    }
}