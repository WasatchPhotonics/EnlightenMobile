using System;
using System.Text;
using Xamarin.Forms;
using EnlightenMobile.Services;

namespace EnlightenMobile
{
    public class Util
    {
        // View is there for iOS (Android doesn't need it)
        public static void toast(string msg, View view = null)
        {
            IPlatformUtil platformUtil = DependencyService.Get<IPlatformUtil>();
            platformUtil.toast(msg, view);
        }

        // only really used by BluetoothView
        public static void updateProgressBar(Xamarin.Forms.ProgressBar pb, double progress)
        {
            progress = Math.Min(1.0, Math.Max(0.0, progress));
            pb.ProgressTo(progress, 500, Easing.Linear);
        }

        public static double[] generateWavelengths(uint pixels, float[] coeffs)
        {            
            double[] wavelengths = new double[pixels];
            for (uint pixel = 0; pixel < pixels; pixel++)
            {   
                wavelengths[pixel] = coeffs[0];
                for (int i = 1; i < coeffs.Length; i++)
                    wavelengths[pixel] += coeffs[i] * Math.Pow(pixel, i);
            }  
            return wavelengths;
        }

        public static double[] wavelengthsToWavenumbers(double laserWavelengthNM, double[] wavelengths)
        {
            const double NM_TO_CM = 1.0 / 10000000.0;
            double LASER_WAVENUMBER = 1.0 / (laserWavelengthNM * NM_TO_CM);

            if (wavelengths == null)
                return null;

            double[] wavenumbers = new double[wavelengths.Length];
            for (int i = 0; i < wavelengths.Length; i++)
            {
                double wavenumber = LASER_WAVENUMBER - (1.0 / (wavelengths[i] * NM_TO_CM));
                if (Double.IsInfinity(wavenumber) || Double.IsNaN(wavenumber))
                    wavenumbers[i] = 0;
                else
                    wavenumbers[i] = wavenumber;
            }
            return wavenumbers;
        }

        // Format a 16-byte array like a standard UUID
        //
        // 00000000-0000-1000-8000-00805F9B34FB
        //  0 1 2 3  4 5  6 7  8 9  a b c d e f
        //
        // You'd think something like this would already be in Plugin.BLE, and
        // probably it is... *shrug*
        public static string formatUUID(byte[] data)
        {
            if (data.Length != 16)
                return "invalid-uuid";
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < 16; i++)
            {
                sb.Append(string.Format("{0:x2}", data[i]));
                if (i == 3 || i == 5 || i == 7 || i == 9)
                    sb.Append("-");
            }
            return sb.ToString();
        }
    }
}
