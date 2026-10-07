namespace PrintCalc3D;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        // Заранее «прогреваем» пул потоков до 8 (4 ядра / 8 потоков): первая параллельная загрузка цен
        // не ждёт, пока .NET постепенно создаст потоки.
        ThreadPool.GetMinThreads(out int worker, out int io);
        ThreadPool.SetMinThreads(Math.Max(worker, PerfConfig.MaxParallel), Math.Max(io, PerfConfig.MaxParallel));

        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
    }
}
