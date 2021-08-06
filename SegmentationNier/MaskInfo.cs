using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SegmentationNier
{
    struct MaskInfo
    {
        public int windowSize;
        public string maskName;
        public MaskInfo(int w, string maskName)
        {
            windowSize = w;
            this.maskName = maskName;
        }
    }
}
