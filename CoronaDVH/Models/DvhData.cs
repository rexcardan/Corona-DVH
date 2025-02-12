using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoronaDVH.Models
{
    public class DvhData
    {
        public List<DvhPoint> Points { get; set; } = new List<DvhPoint>();
        public float MaxDose { get; set; }
        public float MinDose { get; set; }
        public float MeanDose { get; set; }
        public double VolumeCC { get; set; }
        public float Volume { get; internal set; }
    }
}
