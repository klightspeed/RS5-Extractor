using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LibRS5
{
    public class Joint
    {
        public readonly string Symbol;
        public readonly string Name;
        public readonly Matrix4 ReverseBindingMatrix;
        public readonly Matrix4 InitialPose;
        public readonly Joint[] Children;
        public readonly AnimationSequence Animation;

        public Joint(string symbol, string name, Matrix4 revbind, Matrix4 initialpose, IEnumerable<Joint> children, AnimationSequence anim)
        {
            Symbol = symbol;
            Name = name;
            ReverseBindingMatrix = revbind;
            InitialPose = initialpose;
            Children = children.Select(j => j.Clone()).ToArray();
            Animation = anim == null ? new AnimationSequence(24.0, new AnimationFrame[0], 0) : anim.Clone();
        }

        public Joint(Joint joint)
            : this(joint.Symbol, joint.Name, joint.ReverseBindingMatrix, joint.InitialPose, joint.Children, joint.Animation)
        {
        }

        public Joint Clone()
        {
            return new Joint(this);
        }

        public IEnumerable<Joint> GetSelfAndDescendents()
        {
            yield return this;
            if (Children != null)
            {
                foreach (Joint child in Children)
                {
                    foreach (Joint descendent in child.GetSelfAndDescendents())
                    {
                        yield return descendent;
                    }
                }
            }
        }

        public Joint WithTrimmedAnimation(int startframe, int numframes, double framerate)
        {
            AnimationSequence seq = Animation.Trim(startframe, numframes, framerate);
            return new Joint(Symbol, Name, ReverseBindingMatrix, seq.InitialPose, Children.Select(j => j.WithTrimmedAnimation(startframe, numframes, framerate)), seq);
        }

        public Joint WithoutAnimation()
        {
            return new Joint(Symbol, Name, ReverseBindingMatrix, InitialPose, Children, Animation.WithoutAnimation());
        }
    }
}
