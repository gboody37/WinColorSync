using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

namespace WinColorSync
{
    public static class IconGenerator
    {
        public static void CreateAppIcon(string outputPath)
        {
            try
            {
                using (Bitmap bmp = new Bitmap(128, 128))
                {
                    using (Graphics g = Graphics.FromImage(bmp))
                    {
                        g.SmoothingMode = SmoothingMode.AntiAlias;
                        g.Clear(Color.Transparent);

                        // Draw dark circle background
                        using (Brush bgBrush = new SolidBrush(Color.FromArgb(28, 34, 42)))
                        {
                            g.FillEllipse(bgBrush, 4, 4, 120, 120);
                        }

                        // Draw outer glow border
                        using (Pen borderPen = new Pen(Color.FromArgb(0, 120, 215), 4))
                        {
                            g.DrawEllipse(borderPen, 6, 6, 116, 116);
                        }

                        // Draw 4 color palette segments (Green, Blue, Purple, Orange)
                        using (Brush b1 = new SolidBrush(Color.FromArgb(76, 164, 103))) // Green
                        {
                            g.FillPie(b1, 20, 20, 88, 88, 180, 90);
                        }
                        using (Brush b2 = new SolidBrush(Color.FromArgb(0, 120, 215)))  // Blue
                        {
                            g.FillPie(b2, 20, 20, 88, 88, 270, 90);
                        }
                        using (Brush b3 = new SolidBrush(Color.FromArgb(155, 89, 182))) // Purple
                        {
                            g.FillPie(b3, 20, 20, 88, 88, 0, 90);
                        }
                        using (Brush b4 = new SolidBrush(Color.FromArgb(230, 126, 34))) // Orange
                        {
                            g.FillPie(b4, 20, 20, 88, 88, 90, 90);
                        }

                        // Draw center dark node
                        using (Brush centerBrush = new SolidBrush(Color.FromArgb(24, 24, 28)))
                        {
                            g.FillEllipse(centerBrush, 44, 44, 40, 40);
                        }
                        using (Brush starBrush = new SolidBrush(Color.White))
                        {
                            g.FillEllipse(starBrush, 56, 56, 16, 16);
                        }
                    }

                    // Save as PNG/Icon
                    bmp.Save(outputPath, ImageFormat.Png);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("[IconGenerator] Error: " + ex.Message);
            }
        }
    }
}
