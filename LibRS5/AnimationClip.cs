using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LibRS5
{
    public class AnimationClip
    {
        public string ModelName;
        public string Name;
        public int StartFrame;
        public int NumFrames;
        public float FrameRate;

        public override string ToString()
        {
            return String.Format("name: {0}; frames: {1}-{2}; rate: {3}", Name, StartFrame, StartFrame + NumFrames, FrameRate);
        }
    }
}
