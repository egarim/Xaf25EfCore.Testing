# Xaf25EfCore.Testing

## ExampleTest Class Explained

The ExampleTest class in `Tests\UnitTest1.cs` is a unit test designed to validate the functionality of the `CustomerController.SetCustomerActive` action in a DevExpress XAF (eXpressAppFramework) application. This test ensures that when the action is executed, a customer's `Active` property is correctly set to `true`.

### Key Components in the Test

#### 1. Database Setup
- Uses an EF Core in-memory database for testing
- Creates a clean database for each test using a unique GUID
- Configures the database to use lazy loading and change tracking proxies
objectSpaceProvider = new EFCoreObjectSpaceProvider<TestingEFCoreDbContext>(
    (builder, _) => builder
        .UseInMemoryDatabase(Guid.NewGuid().ToString())
        .UseLazyLoadingProxies()
        .UseChangeTrackingProxies()
);
#### 2. Application Setup
- Creates an instance of `TestApplication` (a minimal XAF application)
- Adds the `TestingModule` to the application
- Sets up the application with the object space provider
application = new TestApplication();
TestingModule xafFunctionalTestModule = new TestingModule();
application.Modules.Add(xafFunctionalTestModule);
application.Setup("TestApplication", objectSpaceProvider);
#### 3. Security Configuration
- Creates an admin user with password "123456"
- Creates an "Administrators" role with full administrative privileges
- Configures security with a `SecurityStrategyComplex` implementation
var userAdmin = ObjectSpace.CreateObject<ApplicationUser>();
userAdmin.UserName = "Admin";
user