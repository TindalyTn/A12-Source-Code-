using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Resources;
using System.Runtime.CompilerServices;

namespace Titan.Resources;

[GeneratedCode("System.Resources.Tools.StronglyTypedResourceBuilder", "17.0.0.0")]
[DebuggerNonUserCode]
[CompilerGenerated]
internal class brokenbaseband
{
	private static ResourceManager resourceMan;

	private static CultureInfo resourceCulture;

	[EditorBrowsable(EditorBrowsableState.Advanced)]
	internal static ResourceManager ResourceManager
	{
		get
		{
			if (resourceMan == null)
			{
				ResourceManager resourceManager = new ResourceManager("Titan.Resources.brokenbaseband", typeof(brokenbaseband).Assembly);
				resourceMan = resourceManager;
			}
			return resourceMan;
		}
	}

	[EditorBrowsable(EditorBrowsableState.Advanced)]
	internal static CultureInfo Culture
	{
		get
		{
			return resourceCulture;
		}
		set
		{
			resourceCulture = value;
		}
	}

	internal static byte[] i12reset
	{
		get
		{
			object @object = ResourceManager.GetObject("i12reset", resourceCulture);
			return (byte[])@object;
		}
	}

	internal static byte[] i12update
	{
		get
		{
			object @object = ResourceManager.GetObject("i12update", resourceCulture);
			return (byte[])@object;
		}
	}

	internal static byte[] i13reset
	{
		get
		{
			object @object = ResourceManager.GetObject("i13reset", resourceCulture);
			return (byte[])@object;
		}
	}

	internal static byte[] i13update
	{
		get
		{
			object @object = ResourceManager.GetObject("i13update", resourceCulture);
			return (byte[])@object;
		}
	}

	internal static byte[] purple
	{
		get
		{
			object @object = ResourceManager.GetObject("purple", resourceCulture);
			return (byte[])@object;
		}
	}

	internal static byte[] SystemStatusServer
	{
		get
		{
			object @object = ResourceManager.GetObject("SystemStatusServer", resourceCulture);
			return (byte[])@object;
		}
	}

	internal brokenbaseband()
	{
	}
}
