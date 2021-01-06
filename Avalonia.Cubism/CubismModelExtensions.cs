using CubismFramework;

using System;
using System.Collections.Generic;
using System.Text;

namespace Avalonia.Cubism
{
    public static class CubismModelExtensions
    {
        public static double[] PartGroupSwitch(this CubismModel model, string group, string id)
        {
            List<double> opacities = new List<double>();
            int pcount = model.PartCount;
            for (int i = 0; i < pcount; ++i)
            {
                var part = model.GetPart(i);
                if (part.Name.StartsWith(group))
                {
                    opacities.Add(part.CurrentOpacity);
                    if (part.Name.EndsWith(id))
                    {
                        part.CurrentOpacity = 1.0;
                    }
                    else
                    {
                        part.CurrentOpacity = 0.0;
                    }
                }
            }
            return opacities.ToArray();
        }
    }
}
