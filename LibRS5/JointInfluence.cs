using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LibRS5
{
    public class JointInfluence
    {
        public readonly Joint Joint;
        public readonly double Influence;

        public JointInfluence(Joint joint, double influence)
        {
            Joint = joint;
            Influence = influence;
        }
    }
}
