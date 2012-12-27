using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RS5_Extractor
{
    public class Joint
    {
        public string Symbol;
        public string Name;
        public Matrix4 ReverseBindingMatrix;
        public Matrix4 InitialPose;
        public int JointNum;
        public int ParentNum;
        public Joint Parent;
        public Joint[] Children;
        public AnimationSequence Animation;

        public Joint Clone()
        {
            if (Parent != null)
            {
                throw new InvalidOperationException("Cannot clone child joint");
            }
            return Clone(null);
        }

        protected Joint Clone(Joint parent)
        {
            return new Joint
            {
                Symbol = Symbol,
                Name = Name,
                ReverseBindingMatrix = ReverseBindingMatrix,
                InitialPose = InitialPose,
                JointNum = JointNum,
                ParentNum = ParentNum,
                Parent = parent,
                Children = Children.Select(j => j.Clone(this)).ToArray(),
                Animation = Animation.Clone()
            };
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

        public Joint WithoutAnimation()
        {
            if (Parent != null)
            {
                throw new InvalidOperationException("Cannot clone child joint");
            }
            return WithoutAnimation(null);
        }
        
        protected Joint WithoutAnimation(Joint parent)
        {
            return new Joint
            {
                Symbol = Symbol,
                Name = Name,
                ReverseBindingMatrix = ReverseBindingMatrix,
                InitialPose = InitialPose,
                JointNum = JointNum,
                ParentNum = ParentNum,
                Parent = parent,
                Children = Children.Select(j => j.Clone(this)).ToArray(),
                Animation = Animation.WithoutAnimation()
            };
        }
    }
}
