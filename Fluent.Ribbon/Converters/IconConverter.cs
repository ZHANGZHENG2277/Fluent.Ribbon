﻿// ReSharper disable once CheckNamespace
namespace Fluent
{
    using System;
    using System.Diagnostics;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Data;
    using System.Windows.Interop;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using Fluent.Converters;
    using Windows.Win32;
    using Windows.Win32.Foundation;
    using Windows.Win32.UI.WindowsAndMessaging;

    /// <summary>
    /// Icon converter provides window or application default icon if user-defined is not present.
    /// </summary>
    [ValueConversion(sourceType: typeof(string), targetType: typeof(Image))]
    [ValueConversion(sourceType: typeof(Uri), targetType: typeof(Image))]
    [ValueConversion(sourceType: typeof(System.Drawing.Icon), targetType: typeof(Image))]
    [ValueConversion(sourceType: typeof(ImageSource), targetType: typeof(Image))]
    [ValueConversion(sourceType: typeof(string), targetType: typeof(ImageSource))]
    [ValueConversion(sourceType: typeof(Uri), targetType: typeof(ImageSource))]
    [ValueConversion(sourceType: typeof(System.Drawing.Icon), targetType: typeof(ImageSource))]
    [ValueConversion(sourceType: typeof(ImageSource), targetType: typeof(ImageSource))]
    public sealed class IconConverter : ObjectToImageConverter
    {
        /// <summary>
        /// Creates a new instance.
        /// </summary>
        /// <param name="iconBinding">The binding to which the converter should be applied to.</param>
        public IconConverter(Binding iconBinding)
            : base(iconBinding, new Size(SystemParameters.SmallIconWidth, SystemParameters.SmallIconHeight))
        {
        }

        /// <summary>
        /// Creates a new instance.
        /// </summary>
        /// <param name="iconBinding">The binding to which the converter should be applied to.</param>
        /// <param name="targetVisualBinding">The target visual on which the image/icon should be shown.</param>
        public IconConverter(Binding iconBinding, Binding targetVisualBinding)
            : base(iconBinding, new Size(SystemParameters.SmallIconWidth, SystemParameters.SmallIconHeight), targetVisualBinding)
        {
        }

        /// <summary>
        /// Creates a new instance.
        /// </summary>
        /// <param name="iconBinding">The binding to which the converter should be applied to.</param>
        /// <param name="desiredSize">The desired size for the image.</param>
        /// <param name="targetVisualBinding">The target visual on which the image/icon should be shown.</param>
        public IconConverter(Binding iconBinding, object desiredSize, Binding targetVisualBinding)
            : base(iconBinding, desiredSize, targetVisualBinding)
        {
        }

        /// <inheritdoc />
        protected override object? GetValueToConvert(object? value, Size desiredSize, Visual? targetVisual)
        {
            if (value is null)
            {
                var defaultIcon = GetDefaultIcon(targetVisual, desiredSize);

                if (defaultIcon is not null)
                {
                    return defaultIcon;
                }
            }

            return base.GetValueToConvert(value, desiredSize, targetVisual);
        }

        private static ImageSource? GetDefaultIcon(DependencyObject? targetVisual, Size desiredSize)
        {
            IntPtr windowHandle;

            if (targetVisual is not null)
            {
                var window = Window.GetWindow(targetVisual);

                if (window is not null
                    && (windowHandle = new WindowInteropHelper(window).Handle) != IntPtr.Zero)
                {
                    return GetDefaultIcon(windowHandle, desiredSize);
                }
            }

            if (Application.Current is not null
                && Application.Current.CheckAccess()
                && Application.Current.MainWindow is not null
                && Application.Current.MainWindow.CheckAccess()
                && (windowHandle = new WindowInteropHelper(Application.Current.MainWindow).Handle) != IntPtr.Zero)
            {
                return GetDefaultIcon(windowHandle, desiredSize);
            }

            using (var p = Process.GetCurrentProcess())
            {
                if (p.MainWindowHandle != IntPtr.Zero)
                {
                    return GetDefaultIcon(p.MainWindowHandle, desiredSize);
                }
            }

            return null;
        }

        private static unsafe ImageSource? GetDefaultIcon(IntPtr hwnd, Size desiredSize)
        {
#pragma warning disable CS0219 // Variable is assigned but its value is never used

            // Retrieve the small icon for the window.
            const int ICON_SMALL = 0;
            // Retrieve the large icon for the window.
            const int ICON_BIG = 1;
            // Retrieves the small icon provided by the application. If the application does not provide one, the system uses the system-generated icon for that window.
            const int ICON_SMALL2 = 2;

            const int IDI_APPLICATION = 0x7f00;

#pragma warning restore CS0219 // Variable is assigned but its value is never used

            try
            {
#pragma warning disable 618
                var iconPtr = IntPtr.Zero;

                if (hwnd != IntPtr.Zero)
                {
                    iconPtr = PInvoke.SendMessage(new HWND(hwnd), 0x007F /*GETICON*/, new(ICON_SMALL2), IntPtr.Zero);

                    if (iconPtr == IntPtr.Zero)
                    {
                        iconPtr = new IntPtr(PInvoke.GetClassLong(new HWND(hwnd), GET_CLASS_LONG_INDEX.GCLP_HICONSM));
                    }
                }

                if (iconPtr == IntPtr.Zero)
                {
                    var lpNameLocal = (char*)IDI_APPLICATION;
                    iconPtr = PInvoke.LoadImage(default, lpNameLocal, GDI_IMAGE_TYPE.IMAGE_ICON, (int)desiredSize.Width, (int)desiredSize.Height, IMAGE_FLAGS.LR_SHARED);
                }

                if (iconPtr != IntPtr.Zero)
                {
                    var bitmapFrame = BitmapFrame.Create(Imaging.CreateBitmapSourceFromHIcon(iconPtr, Int32Rect.Empty, BitmapSizeOptions.FromWidthAndHeight((int)desiredSize.Width, (int)desiredSize.Height)));
                    return bitmapFrame;
                }
#pragma warning restore 618
            }
            catch
            {
                // ignored
            }

            return null;
        }
    }
}