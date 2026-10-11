using System;

namespace Minglu
{
    // Options identify authored sprites. There is no scale/tilt deformation
    // and no independently editable ear in the replacement portrait system.
    public static class AppearancePartCatalog
    {
        public static readonly string[] Eyes={"平和","细长","圆润","成熟","锐利","温和","倦眼","长者"};
        public static readonly string[] Brows={"平眉","柔眉","浓眉","纤眉","挑眉","弓眉","粗眉","霜眉"};
        public static readonly string[] Noses={"端正","宽鼻","小鼻","修长","直鼻","高鼻","圆鼻","长者"};
        public static readonly string[] Mouths={"自然","薄唇","丰唇","宽唇","微笑","肃然","微抿","长者"};
        public static string[] Names(int category)
        {
            switch(category){case 2:return Eyes;case 3:return Brows;case 4:return Noses;case 5:return Mouths;default:throw new ArgumentOutOfRangeException(nameof(category));}
        }
    }
}
