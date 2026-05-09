using Bunifu.Framework.UI;
using Ookii.Dialogs.WinForms;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

#nullable disable
namespace hyperlogextractor;

public class LogExtractor : Form
{
  private List<string> selectedFilePaths = new List<string>();
  private bool dragging;
  private Point dragCursorPoint;
  private Point dragFormPoint;
  private IContainer components;
  private RichTextBox Bilgi;
  private RichTextBox searchtext;
  private Label label1;
  private BunifuThinButton2 LoadFile;
  private BunifuThinButton2 Extract;
  private Bunifu.UI.WinForms.BunifuImageButton bunifuImageButton1;
  private Bunifu.UI.WinForms.BunifuImageButton bunifuImageButton2;
  private BunifuThinButton2 bilgilendirme;

  [DllImport("Gdi32.dll")]
  private static extern IntPtr CreateRoundRectRgn(
    int nLeftRect,
    int nTopRect,
    int nRightRect,
    int nBottomRect,
    int nWidthEllipse,
    int nHeightEllipse);

  public LogExtractor()
  {
    this.InitializeComponent();
    this.Load += new EventHandler(this.LogExtractor_Load);
    this.searchtext.GotFocus += new EventHandler(this.RemoveText);
    this.searchtext.LostFocus += new EventHandler(this.AddText);
    this.label1.MouseDown += new MouseEventHandler(this.Label_MouseDown);
    this.label1.MouseMove += new MouseEventHandler(this.Label_MouseMove);
    this.label1.MouseUp += new MouseEventHandler(this.Label_MouseUp);
    this.bunifuImageButton1.Click += (EventHandler) ((sender, e) => this.Close());
    this.bunifuImageButton2.Click += (EventHandler) ((sender, e) => this.WindowState = FormWindowState.Minimized);
    this.FormBorderStyle = FormBorderStyle.None;
    this.Region = Region.FromHrgn(LogExtractor.CreateRoundRectRgn(0, 0, this.Width, this.Height, 30, 30));
  }

  private void LogExtractor_Load(object sender, EventArgs e)
  {
    this.ActiveControl = (Control) this.Bilgi;
    this.AddText((object) null, EventArgs.Empty);
  }

  private void RemoveText(object sender, EventArgs e)
  {
    if (!(this.searchtext.Text == "Aranacak terimleri alt alta yazınız. Örneğin:\nnetflix.com\ndisneyplus.com"))
      return;
    this.searchtext.Text = "";
    this.searchtext.ForeColor = Color.White;
  }

  private void AddText(object sender, EventArgs e)
  {
    if (!string.IsNullOrWhiteSpace(this.searchtext.Text))
      return;
    this.searchtext.Text = "Aranacak terimleri alt alta yazınız. Örneğin:\nnetflix.com\ndisneyplus.com";
    this.searchtext.ForeColor = Color.Gray;
  }

  private void LoadFile_Click(object sender, EventArgs e)
  {
    using (OpenFileDialog openFileDialog = new OpenFileDialog())
    {
      openFileDialog.Filter = "Text Files (*.txt)|*.txt";
      openFileDialog.Multiselect = true;
      if (openFileDialog.ShowDialog() != DialogResult.OK)
        return;
      this.selectedFilePaths = ((IEnumerable<string>) openFileDialog.FileNames).ToList<string>();
      this.UpdateFileInfo();
    }
  }

  private void UpdateFileInfo()
  {
    long num = this.selectedFilePaths.Sum<string>((Func<string, long>) (filePath => new FileInfo(filePath).Length));
    this.Bilgi.Text = $"Seçilen Dosyalar:\n{string.Join("\n", this.selectedFilePaths.Select<string, string>(new Func<string, string>(Path.GetFileName)))}\nToplam Boyut: {num} bytes\n";
  }

  private async void Extract_Click(object sender, EventArgs e)
  {
    if (this.selectedFilePaths.Count == 0 || string.IsNullOrWhiteSpace(this.searchtext.Text))
    {
      int num1 = (int) MessageBox.Show("Lütfen dosya seçin ve arama terimleri girin.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
    }
    else
    {
      Stopwatch stopwatch = Stopwatch.StartNew();
      List<string> searchTerms = ((IEnumerable<string>) this.searchtext.Text.Split(new char[1]
      {
        '\n'
      }, StringSplitOptions.RemoveEmptyEntries)).Select<string, string>((Func<string, string>) (term => term.Trim())).Distinct<string>().ToList<string>();
      using (VistaFolderBrowserDialog dialog = new VistaFolderBrowserDialog())
      {
        dialog.Description = "Çıktı klasörü seçin";
        dialog.UseDescriptionForTitle = true;
        if (dialog.ShowDialog() == DialogResult.OK)
        {
          string outputPath = dialog.SelectedPath;
          bool removeLinks = MessageBox.Show("Linkler kaldırılsın mı?", "Seçim", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;
          this.Bilgi.Text = "Dosya Kaydediliyor. Lütfen Bekleyiniz...";
          await Task.Run((Action) (() => this.ProcessFiles(searchTerms, outputPath, removeLinks)));
          TimeSpan elapsed = stopwatch.Elapsed;
          string report = $"Dosyanın kaydedildiği konum: {outputPath}\n{$"Geçen Süre: {elapsed.Minutes} dakika {elapsed.Seconds} saniye\n"}";
          foreach (string name in searchTerms)
          {
            string str = Path.Combine(outputPath, $"{this.MakeValidFileName(name)}.txt");
            if (File.Exists(str))
            {
              int num2 = this.CountFileLines(str);
              report += $"Bulunan {name} satır sayısı: {num2}\n";
            }
            else
              report += $"Dosya bulunamadı: {str}\n";
          }
          this.Invoke((Delegate) (() => this.Bilgi.Text = report));
        }
      }
    }
  }

  private void ProcessFiles(List<string> searchTerms, string outputPath, bool removeLinks)
  {
    foreach (string searchTerm in searchTerms)
    {
      string str1 = this.MakeValidFileName(searchTerm);
      string str2 = Path.Combine(outputPath, $"{str1}.txt");
      bool flag = true;
      foreach (string selectedFilePath in this.selectedFilePaths)
      {
        using (StreamReader streamReader = new StreamReader((Stream) new FileStream(selectedFilePath, FileMode.Open, FileAccess.Read, FileShare.Read, 65536 /*0x010000*/, FileOptions.SequentialScan)))
        {
          using (StreamWriter streamWriter = new StreamWriter(str2, !flag, Encoding.UTF8, 65536 /*0x010000*/))
          {
            string line;
            while ((line = streamReader.ReadLine()) != null)
            {
              if (line.Contains(searchTerm))
              {
                string str3 = removeLinks ? this.RemoveUrlFromLine(line) : line;
                streamWriter.WriteLine(str3);
              }
            }
          }
        }
        flag = false;
      }
      this.RemoveDuplicatesFromFile(str2);
    }
  }

  private void RemoveDuplicatesFromFile(string filePath)
  {
    List<string> list = ((IEnumerable<string>) File.ReadAllLines(filePath)).Distinct<string>().ToList<string>();
    File.WriteAllLines(filePath, (IEnumerable<string>) list);
  }

  private int CountFileLines(string filePath)
  {
    int num = 0;
    using (StreamReader streamReader = new StreamReader(filePath))
    {
      while (streamReader.ReadLine() != null)
        ++num;
    }
    return num;
  }

  private string RemoveUrlFromLine(string line)
  {
    Match match1 = Regex.Match(line, "(https?:\\/\\/[^\\s|:]+)[\\s|:]+(\\S+)[\\s|:]+(\\S+)");
    if (match1.Success)
      return $"{match1.Groups[2].Value}:{match1.Groups[3].Value}";
    Match match2 = Regex.Match(line, "([a-zA-Z0-9_.-]+\\.[a-zA-Z]{2,})(/[^\\s|:]*)?[\\s|:]+(\\S+)[\\s|:]+(\\S+)");
    if (match2.Success)
      return $"{match2.Groups[3].Value}:{match2.Groups[4].Value}";
    Match match3 = Regex.Match(line, "([a-zA-Z0-9_.-]+\\.[a-zA-Z]{2,})(/[^\\s|:]*)?\\|(\\S+)\\|(\\S+)");
    if (match3.Success)
      return $"{match3.Groups[3].Value}:{match3.Groups[4].Value}";
    Match match4 = Regex.Match(line, "([a-zA-Z0-9_.-]+\\.[a-zA-Z]{2,})(/[^\\s|:]*)?[:\\s]+([a-zA-Z0-9_.-]+@[a-zA-Z0-9_.-]+)[\\s|:]+(\\S+)");
    return match4.Success ? $"{match4.Groups[3].Value}:{match4.Groups[4].Value}" : line;
  }

  private string MakeValidFileName(string name)
  {
    string str = new string(Path.GetInvalidFileNameChars());
    string pattern = $"([{Regex.Escape(str)}]*\\.+$)|([{Regex.Escape(str)}]+)";
    return Regex.Replace(name, pattern, "_");
  }

  private void Label_MouseDown(object sender, MouseEventArgs e)
  {
    this.dragging = true;
    this.dragCursorPoint = Cursor.Position;
    this.dragFormPoint = this.Location;
  }

  private void Label_MouseMove(object sender, MouseEventArgs e)
  {
    if (!this.dragging)
      return;
    this.Location = Point.Add(this.dragFormPoint, new Size(Point.Subtract(Cursor.Position, new Size(this.dragCursorPoint))));
  }

  private void Label_MouseUp(object sender, EventArgs e) => this.dragging = false;

  private void bunifuImageButton1_Click(object sender, EventArgs e) => this.Close();

  private void bunifuImageButton2_Click(object sender, EventArgs e)
  {
    this.WindowState = FormWindowState.Minimized;
  }

  private void label1_Click(object sender, EventArgs e)
  {
  }

  private void Bilgi_TextChanged(object sender, EventArgs e)
  {
  }

  private void searchtext_TextChanged(object sender, EventArgs e)
  {
  }

  private void searchtext_KeyDown(object sender, KeyEventArgs e)
  {
    if (!e.Control || e.KeyCode != Keys.V || !Clipboard.GetDataObject().GetDataPresent(DataFormats.Bitmap))
      return;
    e.SuppressKeyPress = true;
  }

  private void bilgilendirme_Click(object sender, EventArgs e)
  {
    int num = (int) MessageBox.Show("Bu program Hyper tarafından yapılmıştır.\nDiscord: hyperr_0\nTelegram: Hyperr_0", "Bilgilendirme", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
  }

  protected override void Dispose(bool disposing)
  {
    if (disposing && this.components != null)
      this.components.Dispose();
    base.Dispose(disposing);
  }

  private void InitializeComponent()
  {
    ComponentResourceManager componentResourceManager = new ComponentResourceManager(typeof (LogExtractor));
    this.Bilgi = new RichTextBox();
    this.searchtext = new RichTextBox();
    this.label1 = new Label();
    this.LoadFile = new BunifuThinButton2();
    this.Extract = new BunifuThinButton2();
    this.bunifuImageButton1 = new Bunifu.UI.WinForms.BunifuImageButton();
    this.bunifuImageButton2 = new Bunifu.UI.WinForms.BunifuImageButton();
    this.bilgilendirme = new BunifuThinButton2();
    this.SuspendLayout();
    this.Bilgi.BackColor = SystemColors.ActiveCaptionText;
    this.Bilgi.BorderStyle = BorderStyle.None;
    this.Bilgi.DetectUrls = false;
    this.Bilgi.ForeColor = SystemColors.GrayText;
    this.Bilgi.Location = new Point(247, 71);
    this.Bilgi.Name = "Bilgi";
    this.Bilgi.ReadOnly = true;
    this.Bilgi.Size = new Size(503, 307);
    this.Bilgi.TabIndex = 13;
    this.Bilgi.Text = "";
    this.Bilgi.TextChanged += new EventHandler(this.Bilgi_TextChanged);
    this.searchtext.BackColor = Color.Black;
    this.searchtext.BorderStyle = BorderStyle.None;
    this.searchtext.DetectUrls = false;
    this.searchtext.ForeColor = SystemColors.Info;
    this.searchtext.Location = new Point(12, 171);
    this.searchtext.Name = "searchtext";
    this.searchtext.Size = new Size(229, 207);
    this.searchtext.TabIndex = 12;
    this.searchtext.Text = "";
    this.searchtext.TextChanged += new EventHandler(this.searchtext_TextChanged);
    this.label1.AutoSize = true;
    this.label1.BackColor = SystemColors.ActiveCaptionText;
    this.label1.Font = new Font("Segoe UI Black", 24.25f, FontStyle.Bold);
    this.label1.ForeColor = Color.DarkRed;
    this.label1.Location = new Point(12, 12);
    this.label1.Name = "label1";
    this.label1.Size = new Size(415, 45);
    this.label1.TabIndex = 11;
    this.label1.Text = "HYPER LOG EXTRACTOR";
    this.label1.Click += new EventHandler(this.label1_Click);
    this.LoadFile.ActiveBorderThickness = 1;
    this.LoadFile.ActiveCornerRadius = 20;
    this.LoadFile.ActiveFillColor = Color.DarkRed;
    this.LoadFile.ActiveForecolor = Color.Black;
    this.LoadFile.ActiveLineColor = Color.DarkRed;
    this.LoadFile.BackColor = SystemColors.ActiveCaptionText;
    this.LoadFile.BackgroundImage = (Image) componentResourceManager.GetObject("LoadFile.BackgroundImage");
    this.LoadFile.ButtonText = "Load File";
    this.LoadFile.Cursor = Cursors.Hand;
    this.LoadFile.Font = new Font("Microsoft Sans Serif", 12f, FontStyle.Regular, GraphicsUnit.Point, (byte) 0);
    this.LoadFile.ForeColor = Color.DarkRed;
    this.LoadFile.IdleBorderThickness = 1;
    this.LoadFile.IdleCornerRadius = 20;
    this.LoadFile.IdleFillColor = Color.Black;
    this.LoadFile.IdleForecolor = Color.DarkRed;
    this.LoadFile.IdleLineColor = Color.DarkRed;
    this.LoadFile.Location = new Point(12, 71);
    this.LoadFile.Margin = new Padding(5);
    this.LoadFile.Name = "LoadFile";
    this.LoadFile.Size = new Size(224 /*0xE0*/, 41);
    this.LoadFile.TabIndex = 14;
    this.LoadFile.TextAlign = ContentAlignment.MiddleCenter;
    this.LoadFile.Click += new EventHandler(this.LoadFile_Click);
    this.Extract.ActiveBorderThickness = 1;
    this.Extract.ActiveCornerRadius = 20;
    this.Extract.ActiveFillColor = Color.DarkRed;
    this.Extract.ActiveForecolor = Color.Black;
    this.Extract.ActiveLineColor = Color.DarkRed;
    this.Extract.BackColor = SystemColors.ActiveCaptionText;
    this.Extract.BackgroundImage = (Image) componentResourceManager.GetObject("Extract.BackgroundImage");
    this.Extract.ButtonText = "Extract";
    this.Extract.Cursor = Cursors.Hand;
    this.Extract.Font = new Font("Microsoft Sans Serif", 12f, FontStyle.Regular, GraphicsUnit.Point, (byte) 0);
    this.Extract.ForeColor = Color.DarkRed;
    this.Extract.IdleBorderThickness = 1;
    this.Extract.IdleCornerRadius = 20;
    this.Extract.IdleFillColor = Color.Black;
    this.Extract.IdleForecolor = Color.DarkRed;
    this.Extract.IdleLineColor = Color.DarkRed;
    this.Extract.Location = new Point(12, 122);
    this.Extract.Margin = new Padding(5);
    this.Extract.Name = "Extract";
    this.Extract.Size = new Size(224 /*0xE0*/, 41);
    this.Extract.TabIndex = 15;
    this.Extract.TextAlign = ContentAlignment.MiddleCenter;
    this.Extract.Click += new EventHandler(this.Extract_Click);
    this.bunifuImageButton1.ActiveImage = (Image) null;
    this.bunifuImageButton1.AllowAnimations = true;
    this.bunifuImageButton1.AllowBuffering = false;
    this.bunifuImageButton1.AllowToggling = false;
    this.bunifuImageButton1.AllowZooming = true;
    this.bunifuImageButton1.AllowZoomingOnFocus = false;
    this.bunifuImageButton1.BackColor = Color.Transparent;
    this.bunifuImageButton1.DialogResult = DialogResult.None;
    this.bunifuImageButton1.ErrorImage = (Image) componentResourceManager.GetObject("bunifuImageButton1.ErrorImage");
    this.bunifuImageButton1.FadeWhenInactive = false;
    this.bunifuImageButton1.Flip = Bunifu.UI.WinForms.BunifuImageButton.FlipOrientation.Normal;
    this.bunifuImageButton1.Image = (Image) componentResourceManager.GetObject("bunifuImageButton1.Image");
    this.bunifuImageButton1.ImageActive = (Image) null;
    this.bunifuImageButton1.ImageLocation = (string) null;
    this.bunifuImageButton1.ImageMargin = 0;
    this.bunifuImageButton1.ImageSize = new Size(29, 29);
    this.bunifuImageButton1.ImageZoomSize = new Size(30, 30);
    this.bunifuImageButton1.InitialImage = (Image) componentResourceManager.GetObject("bunifuImageButton1.InitialImage");
    this.bunifuImageButton1.Location = new Point(720, 12);
    this.bunifuImageButton1.Name = "bunifuImageButton1";
    this.bunifuImageButton1.Rotation = 0;
    this.bunifuImageButton1.ShowActiveImage = true;
    this.bunifuImageButton1.ShowCursorChanges = true;
    this.bunifuImageButton1.ShowImageBorders = true;
    this.bunifuImageButton1.ShowSizeMarkers = false;
    this.bunifuImageButton1.Size = new Size(30, 30);
    this.bunifuImageButton1.TabIndex = 17;
    this.bunifuImageButton1.ToolTipText = "";
    this.bunifuImageButton1.WaitOnLoad = false;
    this.bunifuImageButton1.Zoom = 0;
    this.bunifuImageButton1.ZoomSpeed = 10;
    this.bunifuImageButton1.Click += new EventHandler(this.bunifuImageButton1_Click);
    this.bunifuImageButton2.ActiveImage = (Image) null;
    this.bunifuImageButton2.AllowAnimations = true;
    this.bunifuImageButton2.AllowBuffering = false;
    this.bunifuImageButton2.AllowToggling = false;
    this.bunifuImageButton2.AllowZooming = true;
    this.bunifuImageButton2.AllowZoomingOnFocus = false;
    this.bunifuImageButton2.BackColor = Color.Transparent;
    this.bunifuImageButton2.DialogResult = DialogResult.None;
    this.bunifuImageButton2.ErrorImage = (Image) componentResourceManager.GetObject("bunifuImageButton2.ErrorImage");
    this.bunifuImageButton2.FadeWhenInactive = false;
    this.bunifuImageButton2.Flip = Bunifu.UI.WinForms.BunifuImageButton.FlipOrientation.Normal;
    this.bunifuImageButton2.Image = (Image) componentResourceManager.GetObject("bunifuImageButton2.Image");
    this.bunifuImageButton2.ImageActive = (Image) null;
    this.bunifuImageButton2.ImageLocation = (string) null;
    this.bunifuImageButton2.ImageMargin = 0;
    this.bunifuImageButton2.ImageSize = new Size(29, 29);
    this.bunifuImageButton2.ImageZoomSize = new Size(30, 30);
    this.bunifuImageButton2.InitialImage = (Image) componentResourceManager.GetObject("bunifuImageButton2.InitialImage");
    this.bunifuImageButton2.Location = new Point(684, 12);
    this.bunifuImageButton2.Name = "bunifuImageButton2";
    this.bunifuImageButton2.Rotation = 0;
    this.bunifuImageButton2.ShowActiveImage = true;
    this.bunifuImageButton2.ShowCursorChanges = true;
    this.bunifuImageButton2.ShowImageBorders = true;
    this.bunifuImageButton2.ShowSizeMarkers = false;
    this.bunifuImageButton2.Size = new Size(30, 30);
    this.bunifuImageButton2.TabIndex = 18;
    this.bunifuImageButton2.ToolTipText = "";
    this.bunifuImageButton2.WaitOnLoad = false;
    this.bunifuImageButton2.Zoom = 0;
    this.bunifuImageButton2.ZoomSpeed = 10;
    this.bunifuImageButton2.Click += new EventHandler(this.bunifuImageButton2_Click);
    this.bilgilendirme.ActiveBorderThickness = 1;
    this.bilgilendirme.ActiveCornerRadius = 20;
    this.bilgilendirme.ActiveFillColor = Color.DarkRed;
    this.bilgilendirme.ActiveForecolor = Color.Black;
    this.bilgilendirme.ActiveLineColor = Color.DarkRed;
    this.bilgilendirme.BackColor = SystemColors.ActiveCaptionText;
    this.bilgilendirme.BackgroundImage = (Image) componentResourceManager.GetObject("bilgilendirme.BackgroundImage");
    this.bilgilendirme.ButtonText = "Bilgilendirme";
    this.bilgilendirme.Cursor = Cursors.Hand;
    this.bilgilendirme.Font = new Font("Microsoft Sans Serif", 12f, FontStyle.Regular, GraphicsUnit.Point, (byte) 0);
    this.bilgilendirme.ForeColor = Color.DarkRed;
    this.bilgilendirme.IdleBorderThickness = 1;
    this.bilgilendirme.IdleCornerRadius = 20;
    this.bilgilendirme.IdleFillColor = Color.Black;
    this.bilgilendirme.IdleForecolor = Color.DarkRed;
    this.bilgilendirme.IdleLineColor = Color.DarkRed;
    this.bilgilendirme.Location = new Point(515, 12);
    this.bilgilendirme.Margin = new Padding(5);
    this.bilgilendirme.Name = "bilgilendirme";
    this.bilgilendirme.Size = new Size(161, 30);
    this.bilgilendirme.TabIndex = 19;
    this.bilgilendirme.TextAlign = ContentAlignment.MiddleCenter;
    this.bilgilendirme.Click += new EventHandler(this.bilgilendirme_Click);
    this.AutoScaleDimensions = new SizeF(6f, 13f);
    this.AutoScaleMode = AutoScaleMode.Font;
    this.BackColor = SystemColors.ActiveCaptionText;
    this.ClientSize = new Size(762, 384);
    this.Controls.Add((Control) this.bilgilendirme);
    this.Controls.Add((Control) this.bunifuImageButton2);
    this.Controls.Add((Control) this.bunifuImageButton1);
    this.Controls.Add((Control) this.Extract);
    this.Controls.Add((Control) this.LoadFile);
    this.Controls.Add((Control) this.Bilgi);
    this.Controls.Add((Control) this.searchtext);
    this.Controls.Add((Control) this.label1);
    this.FormBorderStyle = FormBorderStyle.None;
    this.Icon = (Icon) componentResourceManager.GetObject("$this.Icon");
    this.Name = nameof (LogExtractor);
    this.Text = "Hyper Log Extractor";
    this.ResumeLayout(false);
    this.PerformLayout();
  }
}
