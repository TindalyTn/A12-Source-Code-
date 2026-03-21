using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Guna.UI2.WinForms;
using Guna.UI2.WinForms.Enums;
using Guna.UI2.WinForms.Suite;
using Titan.Properties;

namespace Titan;

public class Alert : Form
{
	private bool dragging = false;

	private Point dragCursorPoint;

	private Point dragFormPoint;

	private IContainer components = null;

	private Guna2Elipse guna2Elipse1;

	private Guna2CircleButton guna2CircleButton3;

	private Guna2CircleButton guna2CircleButton2;

	private Guna2CircleButton guna2CircleButton1;

	internal Guna2GradientButton ActivateButton;

	internal Guna2GradientButton guna2GradientButton1;

	internal Label label2;

	public Alert()
	{
		InitializeComponent();
		((Form)this).StartPosition = (FormStartPosition)0;
	}

	private void Alert_Load(object sender, EventArgs e)
	{
		if (((Form)this).Owner != null)
		{
			int x = ((Form)this).Owner.Location.X + (((Control)((Form)this).Owner).Width - ((Control)this).Width) / 2;
			int y = ((Form)this).Owner.Location.Y + 50;
			((Form)this).Location = new Point(x, y);
		}
	}

	protected override void OnMouseDown(MouseEventArgs e)
	{
		((Control)this).OnMouseDown(e);
		dragging = true;
		dragCursorPoint = Cursor.Position;
		dragFormPoint = ((Form)this).Location;
	}

	protected override void OnMouseMove(MouseEventArgs e)
	{
		((Control)this).OnMouseMove(e);
		if (dragging)
		{
			Point pt = Point.Subtract(Cursor.Position, new Size(dragCursorPoint));
			((Form)this).Location = Point.Add(dragFormPoint, new Size(pt));
		}
	}

	protected override void OnMouseUp(MouseEventArgs e)
	{
		((Control)this).OnMouseUp(e);
		dragging = false;
	}

	private void guna2CircleButton1_Click(object sender, EventArgs e)
	{
		((Form)this).DialogResult = (DialogResult)2;
		((Form)this).Close();
	}

	private void ActivateButton_Click(object sender, EventArgs e)
	{
		((Form)this).DialogResult = (DialogResult)1;
		((Form)this).Close();
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing && components != null)
		{
			components.Dispose();
		}
		((Form)this).Dispose(disposing);
	}

	private void InitializeComponent()
	{

		components = new Container();
		ComponentResourceManager componentResourceManager = new ComponentResourceManager(typeof(Alert));
		guna2Elipse1 = new Guna2Elipse(components);
		guna2CircleButton3 = new Guna2CircleButton();
		guna2CircleButton2 = new Guna2CircleButton();
		guna2CircleButton1 = new Guna2CircleButton();
		ActivateButton = new Guna2GradientButton();
		guna2GradientButton1 = new Guna2GradientButton();
		label2 = new Label();
		((Control)this).SuspendLayout();
		guna2Elipse1.BorderRadius = 15;
		guna2Elipse1.TargetControl = (Control)(object)this;
		((Control)guna2CircleButton3).BackColor = Color.Transparent;
		guna2CircleButton3.DisabledState.BorderColor = Color.DarkGray;
		guna2CircleButton3.DisabledState.CustomBorderColor = Color.DarkGray;
		guna2CircleButton3.DisabledState.FillColor = Color.FromArgb(169, 169, 169);
		guna2CircleButton3.DisabledState.ForeColor = Color.FromArgb(141, 141, 141);
		guna2CircleButton3.FillColor = Color.DarkGray;
		((Control)guna2CircleButton3).Font = new Font("Segoe UI", 9f);
		((Control)guna2CircleButton3).ForeColor = Color.White;
		((Control)guna2CircleButton3).Location = new Point(49, 5);
		((Control)guna2CircleButton3).Margin = new Padding(2);
		((Control)guna2CircleButton3).Name = "guna2CircleButton3";
		guna2CircleButton3.ShadowDecoration.Depth = 3;
		guna2CircleButton3.ShadowDecoration.Enabled = true;
		guna2CircleButton3.ShadowDecoration.Mode = (ShadowMode)1;
		((Control)guna2CircleButton3).Size = new Size(13, 14);
		((Control)guna2CircleButton3).TabIndex = 825;
		((Control)guna2CircleButton3).Text = "guna2CircleButton3";
		((Control)guna2CircleButton2).BackColor = Color.Transparent;
		guna2CircleButton2.DisabledState.BorderColor = Color.DarkGray;
		guna2CircleButton2.DisabledState.CustomBorderColor = Color.DarkGray;
		guna2CircleButton2.DisabledState.FillColor = Color.FromArgb(169, 169, 169);
		guna2CircleButton2.DisabledState.ForeColor = Color.FromArgb(141, 141, 141);
		guna2CircleButton2.FillColor = Color.FromArgb(255, 194, 11);
		((Control)guna2CircleButton2).Font = new Font("Segoe UI", 9f);
		((Control)guna2CircleButton2).ForeColor = Color.White;
		((Control)guna2CircleButton2).Location = new Point(27, 5);
		((Control)guna2CircleButton2).Margin = new Padding(2);
		((Control)guna2CircleButton2).Name = "guna2CircleButton2";
		guna2CircleButton2.ShadowDecoration.Depth = 3;
		guna2CircleButton2.ShadowDecoration.Enabled = true;
		guna2CircleButton2.ShadowDecoration.Mode = (ShadowMode)1;
		((Control)guna2CircleButton2).Size = new Size(13, 14);
		((Control)guna2CircleButton2).TabIndex = 824;
		((Control)guna2CircleButton2).Text = "guna2CircleButton2";
		((Control)guna2CircleButton1).BackColor = Color.Transparent;
		guna2CircleButton1.DisabledState.BorderColor = Color.DarkGray;
		guna2CircleButton1.DisabledState.CustomBorderColor = Color.DarkGray;
		guna2CircleButton1.DisabledState.FillColor = Color.FromArgb(169, 169, 169);
		guna2CircleButton1.DisabledState.ForeColor = Color.FromArgb(141, 141, 141);
		guna2CircleButton1.FillColor = Color.Red;
		((Control)guna2CircleButton1).Font = new Font("Segoe UI", 9f);
		((Control)guna2CircleButton1).ForeColor = Color.White;
		((Control)guna2CircleButton1).Location = new Point(6, 5);
		((Control)guna2CircleButton1).Margin = new Padding(2);
		((Control)guna2CircleButton1).Name = "guna2CircleButton1";
		guna2CircleButton1.ShadowDecoration.Depth = 3;
		guna2CircleButton1.ShadowDecoration.Enabled = true;
		guna2CircleButton1.ShadowDecoration.Mode = (ShadowMode)1;
		((Control)guna2CircleButton1).Size = new Size(13, 14);
		((Control)guna2CircleButton1).TabIndex = 823;
		((Control)guna2CircleButton1).Text = "guna2CircleButton1";
		((Control)guna2CircleButton1).Click += guna2CircleButton1_Click;
		ActivateButton.Animated = true;
		((Control)ActivateButton).BackColor = Color.Transparent;
		ActivateButton.BorderColor = Color.Transparent;
		ActivateButton.BorderRadius = 3;
		ActivateButton.BorderStyle = (DashStyle)1;
		((ButtonState)ActivateButton.DisabledState).BorderColor = Color.DarkGray;
		((ButtonState)ActivateButton.DisabledState).CustomBorderColor = Color.DarkGray;
		((ButtonState)ActivateButton.DisabledState).FillColor = Color.FromArgb(169, 169, 169);
		ActivateButton.DisabledState.FillColor2 = Color.FromArgb(169, 169, 169);
		((ButtonState)ActivateButton.DisabledState).ForeColor = Color.FromArgb(141, 141, 141);
		ActivateButton.FillColor = Color.FromArgb(55, 55, 55);
		ActivateButton.FillColor2 = Color.FromArgb(55, 55, 55);
		((Control)ActivateButton).Font = new Font("Lucida Sans Unicode", 9.25f, (FontStyle)1);
		((Control)ActivateButton).ForeColor = Color.GhostWhite;
		ActivateButton.GradientMode = (LinearGradientMode)2;
		ActivateButton.Image = (Image)(object)Titan.Properties.Resources.info_480px3;
		ActivateButton.ImageAlign = (HorizontalAlignment)0;
		ActivateButton.ImageSize = new Size(25, 25);
		ActivateButton.IndicateFocus = true;
		((Control)ActivateButton).Location = new Point(9, 116);
		((Control)ActivateButton).Name = "ActivateButton";
		ActivateButton.PressedColor = Color.Transparent;
		ActivateButton.ShadowDecoration.BorderRadius = 4;
		ActivateButton.ShadowDecoration.Depth = 6;
		ActivateButton.ShadowDecoration.Enabled = true;
		((Control)ActivateButton).Size = new Size(381, 33);
		((Control)ActivateButton).TabIndex = 826;
		((Control)ActivateButton).Text = "Contiue";
		ActivateButton.UseTransparentBackground = true;
		((Control)ActivateButton).Click += ActivateButton_Click;
		guna2GradientButton1.Animated = true;
		((Control)guna2GradientButton1).BackColor = Color.Transparent;
		guna2GradientButton1.BorderColor = Color.Transparent;
		guna2GradientButton1.BorderRadius = 3;
		guna2GradientButton1.BorderStyle = (DashStyle)1;
		((ButtonState)guna2GradientButton1.DisabledState).BorderColor = Color.DarkGray;
		((ButtonState)guna2GradientButton1.DisabledState).CustomBorderColor = Color.DarkGray;
		((ButtonState)guna2GradientButton1.DisabledState).FillColor = Color.FromArgb(169, 169, 169);
		guna2GradientButton1.DisabledState.FillColor2 = Color.FromArgb(169, 169, 169);
		((ButtonState)guna2GradientButton1.DisabledState).ForeColor = Color.FromArgb(141, 141, 141);
		guna2GradientButton1.FillColor = Color.Transparent;
		guna2GradientButton1.FillColor2 = Color.Transparent;
		((Control)guna2GradientButton1).Font = new Font("Lucida Sans Unicode", 9.25f, (FontStyle)1);
		((Control)guna2GradientButton1).ForeColor = SystemColors.WindowText;
		guna2GradientButton1.GradientMode = (LinearGradientMode)2;
		guna2GradientButton1.Image = (Image)(object)Titan.Properties.Resources.wifi_signal1;
		guna2GradientButton1.ImageSize = new Size(35, 35);
		guna2GradientButton1.IndicateFocus = true;
		((Control)guna2GradientButton1).Location = new Point(12, 40);
		((Control)guna2GradientButton1).Name = "guna2GradientButton1";
		guna2GradientButton1.PressedColor = Color.Transparent;
		((Control)guna2GradientButton1).RightToLeft = (RightToLeft)1;
		guna2GradientButton1.ShadowDecoration.BorderRadius = 4;
		guna2GradientButton1.ShadowDecoration.Depth = 2;
		guna2GradientButton1.ShadowDecoration.Enabled = true;
		guna2GradientButton1.ShadowDecoration.Shadow = new Padding(2);
		((Control)guna2GradientButton1).Size = new Size(50, 49);
		((Control)guna2GradientButton1).TabIndex = 827;
		guna2GradientButton1.UseTransparentBackground = true;
		((Control)label2).BackColor = Color.Transparent;
		((Control)label2).Font = new Font("Segoe UI Semibold", 8f, (FontStyle)1);
		((Control)label2).ForeColor = Color.White;
		((Control)label2).Location = new Point(57, 33);
		((Control)label2).Name = "label2";
		((Control)label2).Size = new Size(310, 75);
		((Control)label2).TabIndex = 829;
		((Control)label2).Text = "Your device is supported and authorized\r\nIMPORTANT: Before proceeding, please ensure:\r\n• Your device is connected to WiFi\r\n• WiFi connection is stable\r\n";
		label2.TextAlign = (ContentAlignment)32;
		((ContainerControl)this).AutoScaleDimensions = new SizeF(6f, 13f);
		((ContainerControl)this).AutoScaleMode = (AutoScaleMode)1;
		((Control)this).BackgroundImage = (Image)(object)Titan.Properties.Resources.pgz;
		((Form)this).ClientSize = new Size(402, 158);
		((Control)this).Controls.Add((Control)(object)label2);
		((Control)this).Controls.Add((Control)(object)guna2GradientButton1);
		((Control)this).Controls.Add((Control)(object)ActivateButton);
		((Control)this).Controls.Add((Control)(object)guna2CircleButton3);
		((Control)this).Controls.Add((Control)(object)guna2CircleButton2);
		((Control)this).Controls.Add((Control)(object)guna2CircleButton1);
		((Form)this).FormBorderStyle = (FormBorderStyle)0;
		((Form)this).Icon = (Icon)componentResourceManager.GetObject("$this.Icon");
		((Control)this).Name = "Alert";
		((Control)this).Text = "Alert";
		((Form)this).Load += Alert_Load;
		((Control)this).ResumeLayout(false);
	}
}
