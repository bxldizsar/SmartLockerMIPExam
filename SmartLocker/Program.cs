using System;
using System.IO;
using System.Windows.Forms;
using SmartLocker.Data;
using SmartLocker.Forms;
using SmartLocker.Services;

namespace SmartLocker
{
    internal static class Program
    {
        // smartlocker.db and trace.log live in the project folder (next to SmartLocker.csproj),
        // so the db the app uses is the same file that goes into git
        public static readonly string DataFolder = FindProjectFolder();
        public static readonly string DatabasePath = Path.Combine(DataFolder, "smartlocker.db");
        public static readonly string TraceLogPath = Path.Combine(DataFolder, "trace.log");

        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            try
            {
                TraceLogger.Initialize(TraceLogPath);

                string connectionString = "Data Source=" + DatabasePath + ";Version=3;";
                DatabaseInitializer.Initialize(connectionString);

                IParcelRepository repository = new SqliteParcelRepository(connectionString);
                Application.Run(new MainForm(repository));
            }
            catch (Exception ex)
            {
                MessageBox.Show("the app could not start:\n" + ex.Message, "SmartLocker",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                TraceLogger.Close();
            }
        }

        // the exe runs from bin\Debug\, so we go up until we find the folder with the .csproj
        // if there is none (exe copied somewhere else) we just use the exe folder
        private static string FindProjectFolder()
        {
            DirectoryInfo folder = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
            while (folder != null)
            {
                if (folder.GetFiles("*.csproj").Length > 0)
                {
                    return folder.FullName;
                }
                folder = folder.Parent;
            }
            return AppDomain.CurrentDomain.BaseDirectory;
        }
    }
}
