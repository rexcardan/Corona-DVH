using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoronaDVH.Helpers
{    
    /// <summary>
     /// Herman's chamfer algorithm as described in Shape-based interpolation:1992. Herman et al. http://ieeexplore.ieee.org/document/135915/
     /// </summary>
    public class ChamferTemplates
    {
        public static float[] NEAR_OPTIMAL_PASS1 = new[] { 14f, 10f, 14f, 10f, 0f, float.NaN, float.NaN, float.NaN, float.NaN };
        public static float[] NEAR_OPTIMAL_PASS2 = new[] { float.NaN, float.NaN, float.NaN, float.NaN, 0f, 10f, 14f, 10f, 14f };
    }
}
