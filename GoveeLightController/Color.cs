using System;

namespace GoveeLightController {

    public class Color {

        public static Color WHITE = new Color(255, 255, 255);
        public static Color YELLOW = new Color(255, 255, 0);
        public static Color ORANGE = new Color(255, 106, 0);
        public static Color BROWN = new Color(127, 51, 0);
        public static Color PURPLE = new Color(255, 0, 255);
        public static Color RED = new Color(255, 0, 0);
        public static Color AQUA = new Color(0, 255, 255);
        public static Color GREEN = new Color(0, 255, 0);
        public static Color BLUE = new Color(0, 0, 255);
        public static Color BLACK = new Color(0, 0, 0);


        private int[] _rgb = new int[3];

        public int R {
            get => _rgb[0];
            set => _rgb[0] = ValidateValue(value);
        }

        public int G {
            get => _rgb[1];
            set => _rgb[1] = ValidateValue(value);
        }

        public int B {
            get => _rgb[2];
            set => _rgb[2] = ValidateValue(value);
        }

        public Color(int r, int g, int b) {
            this.R = ValidateValue(r);
            this.G = ValidateValue(g);
            this.B = ValidateValue(b);
        }

        public Color(int[] rgbList) {
            if(rgbList == null || rgbList.Length != 3)
                throw new ArgumentException("rgbList must have exactly three elements.");

            R = ValidateValue(rgbList[0]);
            G = ValidateValue(rgbList[1]);
            B = ValidateValue(rgbList[2]);
        }

        private int ValidateValue(int value) {
            if(value < 0 || value > 255)
                throw new ArgumentOutOfRangeException(nameof(value), "RGB values must be between 0 and 255.");
            return value;
        }

        public override string ToString() {
            return $"Color(r={R}, g={G}, b={B})";
        }

        // Prevent methods for modifying the array directly.
        public void Append(int value) {
            throw new InvalidOperationException("Cannot append to a Color object. Use properties to modify values.");
        }

        public void Extend(int[] values) {
            throw new InvalidOperationException("Cannot extend a Color object. Use properties to modify values.");
        }

        public void Insert(int index, int value) {
            throw new InvalidOperationException("Cannot insert into a Color object. Use properties to modify values.");
        }
    }
}
