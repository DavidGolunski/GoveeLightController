using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using BarRaider.SdTools;
using System.IO;
using System.ServiceModel.Configuration;

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
                    }
                }
            }

            // Copy the modified RGB values back to the bitmap
            Marshal.Copy(pixels, 0, ptr, byteCount);

            // Unlock the bits
            original.UnlockBits(bmpData);

            return original;
        }

    }
}
