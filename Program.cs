using System;
using System.Windows.Forms;

#nullable disable
namespace hyperlogextractor;

internal static class Program
{
  [STAThread]
  private static void Main()
  {
    Application.EnableVisualStyles();
    Application.SetCompatibleTextRenderingDefault(false);
    Application.Run((Form) new LogExtractor());
  }
}
