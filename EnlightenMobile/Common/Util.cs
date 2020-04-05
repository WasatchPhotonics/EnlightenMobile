using System;
using Xamarin.Forms;
using EnlightenMobile.Services;

namespace EnlightenMobile
{
    public class Util
    {
        public static void toast(string msg)
        {
            IPlatformUtil platformUtil = DependencyService.Get<IPlatformUtil>();
            platformUtil.toast(msg);
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
    }
}
