// TvDemo - Turbo Vision demonstration application
// The canonical demonstration of the Turbo Vision TUI framework

namespace TvDemo;

public static class Program
{
    public static void Main()
    {
        using var app = new TVDemo();
        app.Run();
    }
}
