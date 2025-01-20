using System;
using System.Drawing.Imaging;
using System.Drawing;
using System.Runtime.InteropServices;
using BarRaider.SdTools;
using System.IO;

namespace GoveeLightController {

    public static class ImageTools {

        public static Color GetComplementaryColor(Color originalColor) {
            // Calculate the complementary color
            int complementaryR = 255 - originalColor.R;
            int complementaryG = 255 - originalColor.G;
            int complementaryB = 255 - originalColor.B;

            return Color.FromArgb(originalColor.A, complementaryR, complementaryG, complementaryB);
        }


        public static Bitmap GetBitmapFromFilePath(string filePath) {
            if(!File.Exists(filePath)) {
                Logger.Instance.LogMessage(TracingLevel.ERROR, $"File not found: {filePath}");
                return null;
            }

            return new Bitmap(filePath);
        }

        public static Bitmap ReplaceColor(Bitmap original, Color targetColor, Color replacementColor) {
            // Lock the bitmap's bits for faster processing
            Rectangle rect = new Rectangle(0, 0, original.Width, original.Height);
            BitmapData bmpData = original.LockBits(rect, ImageLockMode.ReadWrite, original.PixelFormat);

            // Get the address of the first line
            IntPtr ptr = bmpData.Scan0;

            // Determine the number of bytes in the bitmap
            int bytesPerPixel = Image.GetPixelFormatSize(original.PixelFormat) / 8;
            int byteCount = bmpData.Stride * original.Height;
            byte[] pixels = new byte[byteCount];

            // Copy the RGB values into the array
            Marshal.Copy(ptr, pixels, 0, byteCount);

            // Target color as bytes
            byte targetR = targetColor.R;
            byte targetG = targetColor.G;
            byte targetB = targetColor.B;

            // Replacement color as bytes
            byte replacementR = replacementColor.R;
            byte replacementG = replacementColor.G;
            byte replacementB = replacementColor.B;

            // Iterate through each pixel
            for(int y = 0; y < original.Height; y++) {
                for(int x = 0; x < original.Width; x++) {
                    int position = y * bmpData.Stride + x * bytesPerPixel;

                    // Check if the pixel matches the target color
                    if(pixels[position] == targetB && pixels[position + 1] == targetG && pixels[position + 2] == targetR) {
                        // Replace with the new color
                        pixels[position] = replacementB;     // Blue
                        pixels[position + 1] = replacementG; // Green
                        pixels[position + 2] = replacementR; // Red

                        if(targetR == 255 && targetG == 255 && targetB == 255) {
                            Logger.Instance.LogMessage(TracingLevel.DEBUG, "White pixel found. Replacing Pixel with: " + replacementR + " " + replacementG + " " + replacementB);
                        }
                    }

                    if(targetR == 255 && targetG == 255 && targetB == 255) {
                        Logger.Instance.LogMessage(TracingLevel.DEBUG, "Searching for White Color " + pixels[position] + " " + pixels[position + 1] + " " + pixels[position + 2]);
                    }
                }
            }

            // Copy the modified RGB values back to the bitmap
            Marshal.Copy(pixels, 0, ptr, byteCount);

            // Unlock the bits
            original.UnlockBits(bmpData);

            return original;
        }

        public static Color FromHSB(float hue, float saturation, float brightness) {
            // Ensure values are in range
            hue = Math.Max(0, Math.Min(360, hue));
            saturation = Math.Max(0, Math.Min(1, saturation));
            brightness = Math.Max(0, Math.Min(1, brightness));

            float chroma = brightness * saturation;
            float x = chroma * (1 - Math.Abs((hue / 60) % 2 - 1));
            float m = brightness - chroma;

            float r = 0, g = 0, b = 0;

            if(hue >= 0 && hue < 60) {
                r = chroma;
                g = x;
                b = 0;
            }
            else if(hue >= 60 && hue < 120) {
                r = x;
                g = chroma;
                b = 0;
            }
            else if(hue >= 120 && hue < 180) {
                r = 0;
                g = chroma;
                b = x;
            }
            else if(hue >= 180 && hue < 240) {
                r = 0;
                g = x;
                b = chroma;
            }
            else if(hue >= 240 && hue < 300) {
                r = x;
                g = 0;
                b = chroma;
            }
            else if(hue >= 300 && hue < 360) {
                r = chroma;
                g = 0;
                b = x;
            }

            // Convert to 0-255 range
            int red = (int) ((r + m) * 255);
            int green = (int) ((g + m) * 255);
            int blue = (int) ((b + m) * 255);

            return Color.FromArgb(red, green, blue);
        }

    }
}
