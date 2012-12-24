using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RS5_Extractor
{
    public class AnimationSequence
    {
        public SortedDictionary<int, Matrix4> Frames = new SortedDictionary<int, Matrix4>();

        public AnimationSequence Trim(int startframe, int numframes)
        {
            int incr = 1;
            if (numframes < 0)
            {
                incr = -1;
                numframes = -numframes;
            }

            AnimationSequence outseq = new AnimationSequence();
            outseq.Frames[0] = Frames[startframe];

            for (int i = 1; i < numframes - 1; i++)
            {
                int index = startframe + i * incr;
                if (Frames[index - 1] != Frames[index] || Frames[index + 1] != Frames[index])
                {
                    if (!Frames[index].EtaEqual((Frames[index - 1] + Frames[index + 1]) * 0.5, 1.0 / 1048576))
                    {
                        outseq.Frames[i] = Frames[index];
                    }
                }
            }

            if (numframes != 0)
            {
                outseq.Frames[numframes - 1] = Frames[startframe + (numframes - 1) * incr];
            }

            return outseq;
        }
    }
}
