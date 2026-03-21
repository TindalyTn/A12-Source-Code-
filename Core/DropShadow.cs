using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Win32;

namespace Core;

public class DropShadow
{
	public struct MARGINS
	{
		public int leftWidth;

		public int rightWidth;

		public int topHeight;

		public int bottomHeight;
	}

	private enum ResizeDirection
	{
		None,
		Left,
		TopLeft,
		Top,
		TopRight,
		Right,
		BottomRight,
		Bottom,
		BottomLeft
	}

	private const int CS_DROPSHADOW = 131072;

	private const int GCL_STYLE = -26;

	private const int DWMWA_NCRENDERING_POLICY = 2;

	private const int DWMNCRP_ENABLED = 2;

	private const int DWMWA_ALLOW_NCPAINT = 4;

	private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

	[DllImport("dwmapi.dll")]
	private static extern int DwmExtendFrameIntoClientArea(IntPtr hWnd, ref MARGINS pMarInset);

	[DllImport("dwmapi.dll")]
	private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

	[DllImport("dwmapi.dll")]
	private static extern int DwmIsCompositionEnabled(ref bool pfEnabled);

	[DllImport("user32.dll", SetLastError = true)]
	private static extern bool SetClassLong(IntPtr hWnd, int nIndex, uint dwNewLong);

	[DllImport("user32.dll", SetLastError = true)]
	private static extern uint GetClassLong(IntPtr hWnd, int nIndex);

	public void ApplyShadowsAndRoundedCorners(Form form, int radius = 0)
	{
		ApplyShadow(form);
	}

	public void ApplyShadow(Form form)
	{
		if (form == null)
		{
			throw new ArgumentNullException("form");
		}
		if (IsCompositionEnabled())
		{
			ApplyModernShadow(form);
		}
		else
		{
			ApplyClassicShadow(form);
		}
	}

	public void RemoveShadow(Form form)
	{
		if (form != null)
		{
			MARGINS mARGINS = default(MARGINS);
			mARGINS.leftWidth = 0;
			mARGINS.rightWidth = 0;
			mARGINS.topHeight = 0;
			mARGINS.bottomHeight = 0;
			MARGINS pMarInset = mARGINS;
			DwmExtendFrameIntoClientArea(((Control)form).Handle, ref pMarInset);
		}
	}

	private void ApplyModernShadow(Form form)
	{
		try
		{
			int attrValue = 2;
			DwmSetWindowAttribute(((Control)form).Handle, 2, ref attrValue, 4);
			MARGINS mARGINS = default(MARGINS);
			mARGINS.bottomHeight = 1;
			mARGINS.leftWidth = 1;
			mARGINS.rightWidth = 1;
			mARGINS.topHeight = 1;
			MARGINS pMarInset = mARGINS;
			DwmExtendFrameIntoClientArea(((Control)form).Handle, ref pMarInset);
			int attrValue2 = 1;
			DwmSetWindowAttribute(((Control)form).Handle, 4, ref attrValue2, 4);
			if (IsWindows10OrGreater())
			{
				ApplyDarkModeSupport(form);
			}
		}
		catch
		{
			ApplyClassicShadow(form);
		}
	}

	private void ApplyClassicShadow(Form form)
	{
		try
		{
			uint classLong = GetClassLong(((Control)form).Handle, -26);
			SetClassLong(((Control)form).Handle, -26, classLong | 0x20000u);
			((Control)form).Refresh();
		}
		catch
		{
		}
	}

	private void ApplyDarkModeSupport(Form form)
	{
		try
		{
			int attrValue = (IsDarkModeEnabled() ? 1 : 0);
			DwmSetWindowAttribute(((Control)form).Handle, 20, ref attrValue, 4);
		}
		catch
		{
		}
	}

	public static bool IsCompositionEnabled()
	{
		if (Environment.OSVersion.Version.Major < 6)
		{
			return false;
		}
		bool pfEnabled = false;
		try
		{
			DwmIsCompositionEnabled(ref pfEnabled);
		}
		catch
		{
			return false;
		}
		return pfEnabled;
	}

	private static bool IsWindows10OrGreater()
	{
		return Environment.OSVersion.Version.Major >= 10;
	}

	private static bool IsDarkModeEnabled()
	{
		try
		{
			using RegistryKey registryKey = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize");
			if (registryKey != null)
			{
				object value = registryKey.GetValue("AppsUseLightTheme");
				if (value != null)
				{
					return (int)value == 0;
				}
			}
		}
		catch
		{
		}
		return false;
	}

	public static void MakeFormDraggable(Form form, Control dragControl = null)
	{

		bool isDragging = false;
		Point lastCursor = Point.Empty;
		Point lastForm = Point.Empty;
		Control val = (Control)(((object)dragControl) ?? ((object)form));
		val.MouseDown += (MouseEventHandler)delegate(object sender, MouseEventArgs e)
		{

			if ((int)e.Button == 1048576)
			{
				isDragging = true;
				lastCursor = Cursor.Position;
				lastForm = form.Location;
			}
		};
		val.MouseMove += (MouseEventHandler)delegate(object sender, MouseEventArgs e)
		{

			if (isDragging && (int)e.Button == 1048576)
			{
				Point pt = Point.Subtract(Cursor.Position, new Size(lastCursor));
				form.Location = Point.Add(lastForm, new Size(pt));
			}
		};
		val.MouseUp += (MouseEventHandler)delegate
		{
			isDragging = false;
		};
	}

	public static void EnableFormResize(Form form, int resizeAreaSize = 10)
	{

		bool isResizing = false;
		ResizeDirection resizeDir = ResizeDirection.None;
		Point lastCursor = Point.Empty;
		Size lastSize = Size.Empty;
		((Control)form).MouseDown += (MouseEventHandler)delegate(object sender, MouseEventArgs e)
		{
			resizeDir = GetResizeDirection(form, e.Location, resizeAreaSize);
			if (resizeDir != 0)
			{
				isResizing = true;
				lastCursor = Cursor.Position;
				lastSize = form.Size;
			}
		};
		((Control)form).MouseMove += (MouseEventHandler)delegate(object sender, MouseEventArgs e)
		{
			if (!isResizing)
			{
				ResizeDirection resizeDirection = GetResizeDirection(form, e.Location, resizeAreaSize);
				SetResizeCursor(form, resizeDirection);
			}
			else
			{
				ResizeForm(form, resizeDir, lastSize, lastCursor);
			}
		};
		((Control)form).MouseUp += (MouseEventHandler)delegate
		{
			isResizing = false;
			resizeDir = ResizeDirection.None;
			((Control)form).Cursor = Cursors.Default;
		};
	}

	private static ResizeDirection GetResizeDirection(Form form, Point point, int areaSize)
	{
		if (point.X <= areaSize)
		{
			if (point.Y <= areaSize)
			{
				return ResizeDirection.TopLeft;
			}
			if (point.Y >= ((Control)form).Height - areaSize)
			{
				return ResizeDirection.BottomLeft;
			}
			return ResizeDirection.Left;
		}
		if (point.X >= ((Control)form).Width - areaSize)
		{
			if (point.Y <= areaSize)
			{
				return ResizeDirection.TopRight;
			}
			if (point.Y >= ((Control)form).Height - areaSize)
			{
				return ResizeDirection.BottomRight;
			}
			return ResizeDirection.Right;
		}
		if (point.Y <= areaSize)
		{
			return ResizeDirection.Top;
		}
		if (point.Y >= ((Control)form).Height - areaSize)
		{
			return ResizeDirection.Bottom;
		}
		return ResizeDirection.None;
	}

	private static void SetResizeCursor(Form form, ResizeDirection direction)
	{
		switch (direction)
		{
		case ResizeDirection.Left:
		case ResizeDirection.Right:
			((Control)form).Cursor = Cursors.SizeWE;
			break;
		case ResizeDirection.Top:
		case ResizeDirection.Bottom:
			((Control)form).Cursor = Cursors.SizeNS;
			break;
		case ResizeDirection.TopLeft:
		case ResizeDirection.BottomRight:
			((Control)form).Cursor = Cursors.SizeNWSE;
			break;
		case ResizeDirection.TopRight:
		case ResizeDirection.BottomLeft:
			((Control)form).Cursor = Cursors.SizeNESW;
			break;
		default:
			((Control)form).Cursor = Cursors.Default;
			break;
		}
	}

	private static void ResizeForm(Form form, ResizeDirection direction, Size lastSize, Point lastCursor)
	{
		Point point = Point.Subtract(Cursor.Position, new Size(lastCursor));
		switch (direction)
		{
		case ResizeDirection.Right:
			((Control)form).Width = lastSize.Width + point.X;
			break;
		case ResizeDirection.Bottom:
			((Control)form).Height = lastSize.Height + point.Y;
			break;
		case ResizeDirection.BottomRight:
			((Control)form).Width = lastSize.Width + point.X;
			((Control)form).Height = lastSize.Height + point.Y;
			break;
		}
		if (((Control)form).Width < ((Control)form).MinimumSize.Width)
		{
			((Control)form).Width = ((Control)form).MinimumSize.Width;
		}
		if (((Control)form).Height < ((Control)form).MinimumSize.Height)
		{
			((Control)form).Height = ((Control)form).MinimumSize.Height;
		}
	}
}
