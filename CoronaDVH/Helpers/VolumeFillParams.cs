using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoronaDVH.Helpers
{
    public class VolFillParams<T>
    {
        /// <summary>
        /// The initial value of the inside of the shape
        /// </summary>
        public T InitialInsideValue { get; set; }

        /// <summary>
        /// The initial value of the outside of the shape
        /// </summary>
        public T InitialOutsideValue { get; set; }

        /// <summary>
        /// The value of the outside border of the shape after chamfering
        /// </summary>
        public T OutsideBorderValue { get; set; }

        /// <summary>
        /// The value of the inside border of the shape after chamfering
        /// </summary>
        public T InsideBorderValue { get; set; }

        /// <summary>
        /// A function which determines whether a point is outside the shape
        /// </summary>
        public Func<T, bool> IsOutside { get; set; }

        /// <summary>
        /// A function which determines whether a point is inside the shape
        /// </summary>
        public Func<T, bool> IsInside { get; set; }
    }
}
