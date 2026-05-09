using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Resources;
using System.Runtime.CompilerServices;

#nullable disable
namespace hyperlogextractor.Properties;

[GeneratedCode("System.Resources.Tools.StronglyTypedResourceBuilder", "4.0.0.0")]
[DebuggerNonUserCode]
[CompilerGenerated]
internal class Resources
{
  private static ResourceManager resourceMan;
  private static CultureInfo resourceCulture;

  internal Resources()
  {
  }

  [EditorBrowsable(EditorBrowsableState.Advanced)]
  internal static ResourceManager ResourceManager
  {
    get
    {
      if (hyperlogextractor.Properties.Resources.resourceMan == null)
        hyperlogextractor.Properties.Resources.resourceMan = new ResourceManager("hyperlogextractor.Properties.Resources", typeof (hyperlogextractor.Properties.Resources).Assembly);
      return hyperlogextractor.Properties.Resources.resourceMan;
    }
  }

  [EditorBrowsable(EditorBrowsableState.Advanced)]
  internal static CultureInfo Culture
  {
    get => hyperlogextractor.Properties.Resources.resourceCulture;
    set => hyperlogextractor.Properties.Resources.resourceCulture = value;
  }
}
