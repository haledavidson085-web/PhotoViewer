namespace PhotoViewer;

static class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        if (UpdateService.TryApplyPendingUpdate(args))
            return;

        UpdateService.ScheduleCleanup(args);
        ApplicationConfiguration.Initialize();
        string? initialPath = args.Length >= 2 && args[0] == "--cleanup-update"
            ? args.ElementAtOrDefault(2)
            : args.FirstOrDefault();
        Application.Run(new MainForm(initialPath));
    }
}
