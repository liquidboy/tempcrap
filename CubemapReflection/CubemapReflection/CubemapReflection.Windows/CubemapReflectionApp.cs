namespace CubemapReflection
{
    class CubemapReflectionApp
    {
        static void Main(string[] args)
        {
            // Profiler.EnableAll();
            using (var game = new CubemapReflectionGame())
            {
                game.Run();
            }
        }
    }
}
