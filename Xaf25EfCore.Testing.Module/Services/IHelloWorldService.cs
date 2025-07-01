namespace Xaf25EfCore.Testing.Module.Services
{
    public interface IHelloWorldService
    {
        string GetGreeting();
        string GetGreeting(string name);
    }
}