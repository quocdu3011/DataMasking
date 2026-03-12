namespace DataMasking
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();
            
            // Chạy form tra cứu điểm thi sinh viên
            Application.Run(new ScoreLookupForm());
            
            // Ẩn form Main và LoginForm tạm thời
            // Application.Run(new Main());
        }
    }
}