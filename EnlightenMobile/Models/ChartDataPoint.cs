using System;
namespace EnlightenMobile.Models
{
    public class ChartDataPoint
    {
        public double intensity { get; set; }
        public double pixel { get; set; }
        public double wavelength { get; set; }
        public double wavenumber { get; set; }

        public ChartDataPoint()
        {
        }
    }
}
