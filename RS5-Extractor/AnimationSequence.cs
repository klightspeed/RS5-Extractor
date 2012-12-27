using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RS5_Extractor
{
    public class AnimationSequence
    {
        public double FrameRate = 24.0;

        public Matrix4 InitialPose = Matrix4.Identity;

        public SortedDictionary<int, Matrix4> Frames = new SortedDictionary<int, Matrix4>();

        public AnimationSequence Clone()
        {
            return new AnimationSequence
            {
                FrameRate = FrameRate,
                InitialPose = InitialPose,
                Frames = new SortedDictionary<int, Matrix4>(Frames)
            };
        }

        public AnimationSequence WithoutAnimation()
        {
            return new AnimationSequence
            {
                FrameRate = FrameRate,
                InitialPose = InitialPose,
                Frames = new SortedDictionary<int, Matrix4>()
            };
        }

        public AnimationSequence Trim(int startframe, int numframes, double framerate)
        {
            int incr = 1;
            int firstframe = startframe;
            if (numframes < 0)
            {
                incr = -1;
                numframes = -numframes;
                firstframe = startframe - numframes + 1;
            }

            AnimationSequence outseq = new AnimationSequence { FrameRate = framerate };

            int numkeyframes = 0;

            Tuple<int, Matrix4> prevframe = null;
            Tuple<int, Matrix4> curframe = null;
            Tuple<int, Matrix4> lastkeyframe = null;

            foreach (KeyValuePair<int, Matrix4> frame_kvp in Frames)
            {
                Tuple<int, Matrix4> nextframe = new Tuple<int, Matrix4>(frame_kvp.Key, frame_kvp.Value);
                    
                if (curframe != null && prevframe != null && curframe.Item1 >= firstframe)
                {
                    if (curframe.Item2 != prevframe.Item2 || curframe.Item2 != nextframe.Item2)
                    {
                        outseq.Frames[(curframe.Item1 - startframe) * incr] = curframe.Item2;
                        lastkeyframe = curframe;
                        numkeyframes++;
                    }
                }

                if (nextframe.Item1 >= firstframe + numframes - 1)
                {
                    if (lastkeyframe != null && nextframe.Item2 != lastkeyframe.Item2)
                    {
                        outseq.Frames[numframes - 1] = nextframe.Item2;
                        numkeyframes++;
                    }
                    break;
                }

                if (nextframe.Item1 > firstframe)
                {
                    if (prevframe == null && curframe != null)
                    {
                        InitialPose = curframe.Item2;
                        outseq.Frames[0] = curframe.Item2;
                        lastkeyframe = curframe;
                        numkeyframes++;
                    }

                    prevframe = curframe;
                }

                curframe = nextframe;
            }

            if (numkeyframes <= 1)
            {
                Frames = new SortedDictionary<int, Matrix4>();
            }

            return outseq;
        }
    }
}
