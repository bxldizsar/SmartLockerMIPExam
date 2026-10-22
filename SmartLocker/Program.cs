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
