﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RS5_Extractor
{
    public class Joint
    {
        public string Symbol;
        public string Name;
        public float[,] ReverseBindingMatrix;
        public float[,] InitialPose;
        public int JointNum;
        public int ParentNum;
        public Joint Parent;
        public Joint[] Children;
    }
}
