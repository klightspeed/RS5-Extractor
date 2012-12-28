using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RS5_Extractor
{
    public class Joint
    {
        public string Symbol { get; set; }
        public string Name { get; set; }
        public Matrix4 ReverseBindingMatrix { get; set; }
        public Matrix4 InitialPose { get; set; }
        public Joint[] Children { get; set; }
        public AnimationSequence Animation { get; set; }

        public Joint Clone()
        {
            return new Joint
            {
                Symbol = Symbol,
                Name = Name,
                ReverseBindingMatrix = ReverseBindingMatrix,
                InitialPose = InitialPose,
                Children = Children.Select(j => j.Clone()).ToArray(),
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
            return new Joint
            {
                Symbol = Symbol,
                Name = Name,
                ReverseBindingMatrix = ReverseBindingMatrix,
                InitialPose = InitialPose,
                Children = Children.Select(j => j.Clone()).ToArray(),
                Animation = Animation.WithoutAnimation()
            };
        }
    }
}
