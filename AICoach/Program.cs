using System;
using System.Windows.Forms;

namespace AICoach;

internal static class Program
{
    [STAThread]
    static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Logger.Instance.Log("Starting AI Coach...");
        Application.Run(new TrayAppContext());
    }
}