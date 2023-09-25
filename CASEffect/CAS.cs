using System;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Drawing;
using System.Threading;
using System.Reflection;
using System.Drawing.Text;
using System.Windows.Forms;
using System.IO.Compression;
using System.Drawing.Drawing2D;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Registry = Microsoft.Win32.Registry;
using RegistryKey = Microsoft.Win32.RegistryKey;
using PaintDotNet;
using PaintDotNet.AppModel;
using PaintDotNet.Effects;
using PaintDotNet.Clipboard;
using PaintDotNet.IndirectUI;
using PaintDotNet.Collections;
using PaintDotNet.PropertySystem;
using PaintDotNet.Rendering;
using ColorWheelControl = PaintDotNet.ColorBgra;
using AngleControl = System.Double;
using PanSliderControl = PaintDotNet.Rendering.Vector2Double;
using FolderControl = System.String;
using FilenameControl = System.String;
using ReseedButtonControl = System.Byte;
using RollControl = PaintDotNet.Rendering.Vector3Double;
using IntSliderControl = System.Int32;
using CheckboxControl = System.Boolean;
using TextboxControl = System.String;
using DoubleSliderControl = System.Double;
using ListBoxControl = System.Byte;
using RadioButtonControl = System.Byte;
using MultiLineTextboxControl = System.String;
using LabelComment = System.String;

[assembly: AssemblyTitle("CAS plugin for Paint.NET")]
[assembly: AssemblyDescription("Reshade port of AMD FidelityFX Contrast Adaptive Sharpening")]
[assembly: AssemblyConfiguration("contrast adaptive sharpening")]
[assembly: AssemblyCompany("duschno")]
[assembly: AssemblyProduct("CAS")]
[assembly: AssemblyCopyright("Copyright ©2023 by duschno")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]
[assembly: ComVisible(false)]
[assembly: AssemblyVersion("1.0.*")]
[assembly: AssemblyMetadata("BuiltByCodeLab", "Version=6.9.8664.24624")]
[assembly: SupportedOSPlatform("Windows")]

namespace CASEffect
{
    public class CASSupportInfo : IPluginSupportInfo
    {
        public string Author => base.GetType().Assembly.GetCustomAttribute<AssemblyCopyrightAttribute>().Copyright;
        public string Copyright => base.GetType().Assembly.GetCustomAttribute<AssemblyDescriptionAttribute>().Description;
        public string DisplayName => base.GetType().Assembly.GetCustomAttribute<AssemblyProductAttribute>().Product;
        public Version Version => base.GetType().Assembly.GetName().Version;
        public Uri WebsiteUri => new Uri("https://github.com/duschno");
    }

    [PluginSupportInfo<CASSupportInfo>(DisplayName = "Contrast Adaptive Sharpening")]
    public class CASEffectPlugin : PropertyBasedEffect
    {
        public static string StaticName => "Contrast Adaptive Sharpening";
        public static Image StaticIcon => new Bitmap(typeof(CASEffectPlugin), "CAS.png");
        public static string SubmenuName => SubmenuNames.Photo;

        public CASEffectPlugin()
            : base(StaticName, StaticIcon, SubmenuName, new EffectOptions { Flags = EffectFlags.Configurable })
        {
        }

        public enum PropertyNames
        {
            Sharpening,
            Contrast
        }


        protected override PropertyCollection OnCreatePropertyCollection()
        {
            List<Property> props = new List<Property>();

            props.Add(new DoubleProperty(PropertyNames.Sharpening, 1, 0, 1));
            props.Add(new DoubleProperty(PropertyNames.Contrast, 0, 0, 1));

            return new PropertyCollection(props);
        }

        protected override ControlInfo OnCreateConfigUI(PropertyCollection props)
        {
            ControlInfo configUI = CreateDefaultConfigUI(props);

            configUI.SetPropertyControlValue(PropertyNames.Sharpening, ControlInfoPropertyNames.DisplayName, "Sharpening intensity");
            configUI.SetPropertyControlValue(PropertyNames.Sharpening, ControlInfoPropertyNames.SliderLargeChange, 0.25);
            configUI.SetPropertyControlValue(PropertyNames.Sharpening, ControlInfoPropertyNames.SliderSmallChange, 0.05);
            configUI.SetPropertyControlValue(PropertyNames.Sharpening, ControlInfoPropertyNames.UpDownIncrement, 0.01);
            configUI.SetPropertyControlValue(PropertyNames.Sharpening, ControlInfoPropertyNames.DecimalPlaces, 3);
            configUI.SetPropertyControlValue(PropertyNames.Sharpening, ControlInfoPropertyNames.ShowHeaderLine, false);
            configUI.SetPropertyControlValue(PropertyNames.Contrast, ControlInfoPropertyNames.DisplayName, "Contrast adaptation");
            configUI.SetPropertyControlValue(PropertyNames.Contrast, ControlInfoPropertyNames.SliderLargeChange, 0.25);
            configUI.SetPropertyControlValue(PropertyNames.Contrast, ControlInfoPropertyNames.SliderSmallChange, 0.05);
            configUI.SetPropertyControlValue(PropertyNames.Contrast, ControlInfoPropertyNames.UpDownIncrement, 0.01);
            configUI.SetPropertyControlValue(PropertyNames.Contrast, ControlInfoPropertyNames.DecimalPlaces, 3);
            configUI.SetPropertyControlValue(PropertyNames.Contrast, ControlInfoPropertyNames.ShowHeaderLine, false);

            return configUI;
        }

        protected override void OnCustomizeConfigUIWindowProperties(PropertyCollection props)
        {
            // Change the effect's window title
            props[ControlInfoPropertyNames.WindowTitle].Value = "Contrast Adaptive Sharpening";
            // Add help button to effect UI
            props[ControlInfoPropertyNames.WindowHelpContentType].Value = WindowHelpContentType.PlainText;
            props[ControlInfoPropertyNames.WindowHelpContent].Value = "Reshade port of AMD FidelityFX Contrast Adaptive Sharpening v1.0\nhttps://github.com/duschno";
            base.OnCustomizeConfigUIWindowProperties(props);
        }

        protected override void OnSetRenderInfo(PropertyBasedEffectConfigToken token, RenderArgs dstArgs, RenderArgs srcArgs)
        {
            Sharpening = token.GetProperty<DoubleProperty>(PropertyNames.Sharpening).Value;
            Contrast = token.GetProperty<DoubleProperty>(PropertyNames.Contrast).Value;

            PreRender(dstArgs.Surface, srcArgs.Surface);

            base.OnSetRenderInfo(token, dstArgs, srcArgs);
        }

        protected override unsafe void OnRender(Rectangle[] rois, int startIndex, int length)
        {
            if (length == 0) return;
            for (int i = startIndex; i < startIndex + length; ++i)
            {
                Render(DstArgs.Surface,SrcArgs.Surface,rois[i]);
            }
        }

        #region User Entered Code
        // Name: Contrast Adaptive Sharpening
        // Submenu: Photo
        // Author: duschno
        // Title: Contrast Adaptive Sharpening
        // Version: 1.0
        // Desc: Reshade port of AMD FidelityFX Contrast Adaptive Sharpening
        // Keywords:
        // URL: https://github.com/duschno
        // Help: Reshade port of AMD FidelityFX Contrast Adaptive Sharpening v1.0\nhttps://github.com/duschno
        #region UICode
        DoubleSliderControl Sharpening = 1; // [0,1] Sharpening intensity
        DoubleSliderControl Contrast = 0; // [0,1] Contrast adaptation
        #endregion

        // This single-threaded function is called after the UI changes and before the Render function is called
        // The purpose is to prepare anything you'll need in the Render function
        void PreRender(Surface dst, Surface src)
        {
        }

        // Here is the main multi-threaded render function
        // The dst canvas is broken up into rectangles and
        // your job is to write to each pixel of that rectangle
        void Render(Surface dst, Surface src, Rectangle rect)
        {
        	// Step through each row of the current rectangle
        	for (int y = rect.Top; y < rect.Bottom; y++)
        	{
        		if (IsCancelRequested) return;
        		// Step through each pixel on the current row of the rectangle
        		for (int x = rect.Left; x < rect.Right; x++)
        		{
        			// TODO: Add additional pixel processing code here
        			if (x == 0 || y == 0 || x == rect.Right - 1 || y == rect.Bottom - 1)
        			{
        				dst[x, y] = src[x, y];
        				continue;
        			}

        			// fetch a 3x3 neighborhood around the pixel 'e',
        			// a b c
        			// d(e)f
        			// g h i
        			Vector3Double a, b, c, d, e, f, g, h, i;
        			a = ToVector3(src[x - 1, y - 1]);
        			b = ToVector3(src[x, y - 1]);
        			c = ToVector3(src[x + 1, y - 1]);
        			d = ToVector3(src[x - 1, y]);
        			e = ToVector3(src[x, y]);
        			f = ToVector3(src[x + 1, y]);
        			g = ToVector3(src[x - 1, y + 1]);
        			h = ToVector3(src[x, y + 1]);
        			i = ToVector3(src[x + 1, y + 1]);

        			// Soft min and max.
        			// a b c			 b
        			// d e f * 0.5  +  d e f * 0.5
        			// g h i			 h
        			// These are 2.0x bigger (factored out the extra multiply).
        			Vector3Double mnRGB = Min(Min(Min(d, e), Min(f, b)), h);
        			Vector3Double mnRGB2 = Min(mnRGB, Min(Min(a, c), Min(g, i)));
        			mnRGB += mnRGB2;

        			Vector3Double mxRGB = Max(Max(Max(d, e), Max(f, b)), h);
        			Vector3Double mxRGB2 = Max(mxRGB, Max(Max(a, c), Max(g, i)));
        			mxRGB += mxRGB2;

        			// Smooth Minimum distance to signal limit divided by smooth Max.
        			Vector3Double rcpMRGB = Rcp(mxRGB);
        			Vector3Double ampRGB = Saturate(Mult(Min(mnRGB, new Vector3Double(2) - mxRGB), rcpMRGB));

        			// Shaping amount of sharpening.
        			ampRGB = Rsqrt(ampRGB);

        			double peak = -3.0f * Contrast + 8.0f;
        			Vector3Double wRGB = -Rcp(ampRGB * peak);

        			Vector3Double rcpWeightRGB = Rcp(4f * wRGB + new Vector3Double(1));

        			//						  0 w 0
        			//  Filter shape:		  w 1 w
        			//						  0 w 0
        			Vector3Double window = (b + d) + (f + h);
        			Vector3Double outColor = Saturate(Mult((Mult(window, wRGB) + e), rcpWeightRGB));

        			dst[x, y] = ToColorBgra(Lerp(e, outColor, Sharpening), src[x, y].A);
        		}
        	}
        }

        private Vector3Double Mult(Vector3Double left, Vector3Double right)
        {
        	return new Vector3Double(left.X * right.X, left.Y * right.Y, left.Z * right.Z);
        }
        private Vector3Double Min(Vector3Double value1, Vector3Double value2)
        {
        	return new Vector3Double((value1.X < value2.X) ? value1.X : value2.X, (value1.Y < value2.Y) ? value1.Y : value2.Y, (value1.Z < value2.Z) ? value1.Z : value2.Z);
        }
        private Vector3Double Max(Vector3Double value1, Vector3Double value2)
        {
        	return new Vector3Double((value1.X > value2.X) ? value1.X : value2.X, (value1.Y > value2.Y) ? value1.Y : value2.Y, (value1.Z > value2.Z) ? value1.Z : value2.Z);
        }
        private Vector3Double Lerp(Vector3Double value1, Vector3Double value2, double amount)
        {
        	Vector3Double vector = value1 * (1f - amount);
        	Vector3Double vector2 = value2 * amount;
        	return vector + vector2;
        }
        private Vector3Double Saturate(Vector3Double value)
        {
        	if (value.X > 1)
        		value.X = 1;
        	if (value.Y > 1)
        		value.Y = 1;
        	if (value.Z > 1)
        		value.Z = 1;

        	if (value.X < 0)
        		value.X = 0;
        	if (value.Y < 0)
        		value.Y = 0;
        	if (value.Z < 0)
        		value.Z = 0;

        	return value;
        }
        private Vector3Double Rcp(Vector3Double value)
        {
        	value.X = value.X == 0 ? 1 : 1 / value.X;
        	value.Y = value.Y == 0 ? 1 : 1 / value.Y;
        	value.Z = value.Z == 0 ? 1 : 1 / value.Z;
        	return value;
        }
        private Vector3Double Rsqrt(Vector3Double value)
        {
        	return Rcp(new Vector3Double(Math.Sqrt(value.X), Math.Sqrt(value.Y), Math.Sqrt(value.Z)));
        }
        private Vector3Double ToVector3(Color color)
        {
        	return new Vector3Double(color.R / 255f, color.G / 255f, color.B / 255f);
        }
        private ColorBgra ToColorBgra(Vector3Double value, byte srcA)
        {
        	return new ColorBgra()
        	{
        		R = (byte)(value.X * 255),
        		G = (byte)(value.Y * 255),
        		B = (byte)(value.Z * 255),
        		A = srcA
        	};
        }
        #endregion
    }
}
