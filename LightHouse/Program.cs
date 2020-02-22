using System;

namespace LightHouse
{
    class Program
    {
        static void Main(string[] args)
        {
            var lighthouseService = new LighthouseService();
            lighthouseService.Start();
            Console.WriteLine("Press Control + C to terminate.");
            Console.CancelKeyPress += async (sender, eventArgs) => { await lighthouseService.StopAsync(); };
            lighthouseService.TerminationHandle.Wait();
        }
    }
}
