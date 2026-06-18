using System.Drawing;

using System.Drawing.Drawing2D;

using System.Drawing.Imaging;

using System.IO;

using System.Windows;

using System.Windows.Interop;

using System.Windows.Media;

using System.Windows.Media.Imaging;



namespace VoiceType.Utils;



/// <summary>

/// Draws the VoiceType circular mic badge and exposes tray/window icons.

/// </summary>

public static class AppIcon

{

    private static readonly System.Drawing.Color Orange = System.Drawing.Color.FromArgb(255, 217, 119, 87);

    private static readonly System.Drawing.Color OrangeDeep = System.Drawing.Color.FromArgb(255, 196, 101, 72);

    private static readonly System.Drawing.Color Background = System.Drawing.Color.FromArgb(255, 10, 10, 10);



    public static Icon CreateTrayIcon() => CreateIcon(32);



    public static void ApplyTo(Window window)

    {

        using var icon = CreateIcon(32);

        window.Icon = Imaging.CreateBitmapSourceFromHIcon(

            icon.Handle,

            Int32Rect.Empty,

            BitmapSizeOptions.FromWidthAndHeight(32, 32));

    }



    /// <summary>White mic outline for use on the orange badge.</summary>

    public static Geometry CreateMicGeometry()

    {

        // Pill capsule + U cradle only — no stem/base.

        return Geometry.Parse(

            "M12,6.5 C13.4,6.5 14.5,7.6 14.5,9 V11.5 C14.5,13.4 13.4,14.5 12,14.5 " +

            "C10.6,14.5 9.5,13.4 9.5,11.5 V9 C9.5,7.6 10.6,6.5 12,6.5 Z " +

            "M8.5,12.5 C8.5,14.8 10.1,16.5 12,16.5 C13.9,16.5 15.5,14.8 15.5,12.5");

    }



    public static void SaveIcoFile(string path)

    {

        int[] sizes = [16, 32, 48, 256];

        var pngs = sizes.Select(s =>

        {

            using var bmp = RenderBitmap(s);

            using var ms = new MemoryStream();

            bmp.Save(ms, ImageFormat.Png);

            return ms.ToArray();

        }).ToArray();



        using var fs = File.Create(path);

        using var bw = new BinaryWriter(fs);

        bw.Write((short)0);

        bw.Write((short)1);

        bw.Write((short)pngs.Length);



        var offset = 6 + 16 * pngs.Length;

        for (var i = 0; i < pngs.Length; i++)

        {

            var size = sizes[i];

            var data = pngs[i];

            bw.Write((byte)(size >= 256 ? 0 : size));

            bw.Write((byte)(size >= 256 ? 0 : size));

            bw.Write((byte)0);

            bw.Write((byte)0);

            bw.Write((short)1);

            bw.Write((short)32);

            bw.Write(data.Length);

            bw.Write(offset);

            offset += data.Length;

        }



        foreach (var data in pngs)

            bw.Write(data);

    }



    private static Icon CreateIcon(int size)

    {

        using var bmp = RenderBitmap(size);

        var handle = bmp.GetHicon();

        return Icon.FromHandle(handle);

    }



    private static Bitmap RenderBitmap(int size)

    {

        var bmp = new Bitmap(size, size, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

        using var g = Graphics.FromImage(bmp);

        g.SmoothingMode = SmoothingMode.AntiAlias;

        g.PixelOffsetMode = PixelOffsetMode.HighQuality;

        g.Clear(Background);



        var scale = size / 24f;

        var center = size / 2f;

        var badgeRadius = 9.5f * scale;



        // Orange gradient circle badge.

        var badgeRect = new RectangleF(center - badgeRadius, center - badgeRadius, badgeRadius * 2, badgeRadius * 2);

        using (var brush = new System.Drawing.Drawing2D.LinearGradientBrush(

                   badgeRect,

                   Orange,

                   OrangeDeep,

                   LinearGradientMode.Vertical))

        {

            g.FillEllipse(brush, badgeRect);

        }



        // Subtle ring.

        using (var ring = new System.Drawing.Pen(System.Drawing.Color.FromArgb(90, 255, 255, 255), Math.Max(1f, 1.2f * scale)))

        {

            g.DrawEllipse(ring, badgeRect);

        }



        // White mic outline.

        var penWidth = Math.Max(1.4f, 1.8f * scale);

        using var micPen = new System.Drawing.Pen(System.Drawing.Color.White, penWidth)

        {

            StartCap = LineCap.Round,

            EndCap = LineCap.Round,

            LineJoin = LineJoin.Round

        };



        // Capsule body.

        var capsule = new RectangleF((12 - 2.5f) * scale, 6.5f * scale, 5f * scale, 8f * scale);

        g.DrawEllipse(micPen, capsule);



        // U cradle.

        var arc = new RectangleF((12 - 3.5f) * scale, 10.5f * scale, 7f * scale, 6f * scale);

        g.DrawArc(micPen, arc, 0, 180);



        return bmp;

    }

}


