using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RS5_Extractor
{
    public class AnimationFrame
    {
        public readonly int FrameNum;
        public readonly Matrix4 Transform;

        public AnimationFrame(int framenum, Matrix4 matrix)
        {
            FrameNum = framenum;
            Transform = matrix;
        }
    }

    public class AnimationSequence
    {
        public readonly double FrameRate;
        public readonly Matrix4 InitialPose;
        public readonly AnimationFrame[] Frames;
        public readonly int NumFrames;

        protected AnimationSequence(AnimationSequence clonefrom)
            : this(clonefrom.FrameRate, clonefrom.InitialPose, clonefrom.Frames, clonefrom.NumFrames)
        {
        }

        public AnimationSequence(double framerate, IEnumerable<AnimationFrame> inframes, int numframes)
            : this(framerate, Matrix4.Identity, inframes, numframes)
        {
        }

        public AnimationSequence(double framerate, Matrix4 initialpose, IEnumerable<AnimationFrame> inframes, int numframes)
        {
            AnimationFrame[] frames = inframes == null ? new AnimationFrame[0] : inframes.ToArray();
            int maxframe = frames.Length == 0 ? 0 : frames.Select(f => f.FrameNum).Max() + 1;
            this.FrameRate = framerate;
            this.InitialPose = frames.Length == 0 ? initialpose : frames[0].Transform;
            this.Frames = frames.Length > 1 ? frames : new AnimationFrame[0];
            this.NumFrames = Math.Max(numframes, maxframe);
        }

        public AnimationSequence Clone()
        {
            return new AnimationSequence(this);
        }

        public AnimationSequence WithoutAnimation()
        {
            return new AnimationSequence(FrameRate, InitialPose, new AnimationFrame[0], 0);
        }

        public static IEnumerable<AnimationFrame> Trim(IEnumerable<AnimationFrame> inframes, int startframe, int numframes)
        {
            int incr = 1;
            int firstframe = startframe;
            if (numframes < 0)
            {
                incr = -1;
                numframes = -numframes;
                firstframe = startframe - numframes + 1;
            }

            int numkeyframes = 0;

            AnimationFrame prevframe = null;
            AnimationFrame curframe = null;
            AnimationFrame lastkeyframe = null;

            foreach (AnimationFrame frame in inframes)
            {
                AnimationFrame nextframe = frame;
                    
                if (curframe != null && prevframe != null && curframe.FrameNum >= firstframe)
                {
                    if (curframe.Transform != prevframe.Transform || curframe.Transform != nextframe.Transform)
                    {
                        yield return new AnimationFrame((curframe.FrameNum - startframe) * incr, curframe.Transform);
                        lastkeyframe = curframe;
                        numkeyframes++;
                    }
                }

                if (nextframe.FrameNum >= firstframe + numframes - 1)
                {
                    if (lastkeyframe != null && nextframe.Transform != lastkeyframe.Transform)
                    {
                        yield return new AnimationFrame(numframes - 1, nextframe.Transform);
                        numkeyframes++;
                    }
                    break;
                }

                if (nextframe.FrameNum > firstframe)
                {
                    if (prevframe == null && curframe != null)
                    {
                        yield return new AnimationFrame(0, curframe.Transform);
                        lastkeyframe = curframe;
                        numkeyframes++;
                    }

                    prevframe = curframe;
                }

                curframe = nextframe;
            }
        }

        public AnimationSequence Trim(int startframe, int numframes, double framerate)
        {
            return new AnimationSequence(framerate, Trim(this.Frames, startframe, numframes), numframes);
        }
    }
}
