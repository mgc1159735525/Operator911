using System;
using UnityEngine;

namespace Minglu
{
    // Append-only option IDs: saved portraits and presets keep their chosen parts.
    // Shapes are local deformations of each face's own painted features, avoiding
    // skin-tone seams from pasting a different donor face into the portrait.
    public static class AppearancePartCatalog
    {
        public static readonly string[] Eyes={"原生眼形","细长","圆润","舒展","杏眼","含蓄","微挑","垂眸"};
        public static readonly string[] Brows={"原生眉形","平缓","上扬","修长","短眉","浓眉","纤眉","舒眉"};
        public static readonly string[] Noses={"原生鼻形","窄鼻","宽鼻","修长","短鼻","秀挺","圆鼻","丰挺"};
        public static readonly string[] Mouths={"原生嘴形","收窄","舒展","丰唇","薄唇","小巧","宽薄","饱满"};
        public static readonly string[] Ears={"原生耳形","贴耳","宽耳","修长","小耳","圆耳","窄长","宽短"};
        static Vector4 S(float x=1,float y=1,float tilt=0){return new Vector4(x,y,tilt,0);}
        static readonly Vector4[][] shapes={
            new[]{S(),S(1.09f,.84f),S(.96f,1.17f),S(1.12f,1.05f),S(1.05f,1.12f,.045f),S(.93f,.88f),S(1.04f,.98f,.14f),S(1.02f,.94f,-.12f)},
            new[]{S(),S(1,.82f),S(1,1,.11f),S(1.13f,1),S(.86f,1),S(1.04f,1.18f),S(1.10f,.83f),S(1.08f,1.06f,-.075f)},
            new[]{S(),S(.83f,1),S(1.17f,1),S(.96f,1.11f),S(1,.89f),S(.89f,1.08f),S(1.10f,.91f),S(1.10f,1.08f)},
            new[]{S(),S(.85f,1),S(1.13f,1),S(1,1.19f),S(1,.83f),S(.89f,.91f),S(1.10f,.86f),S(1.08f,1.13f)},
            new[]{S(),S(.84f,1),S(1.15f,1),S(1,1.13f),S(.89f,.87f),S(1.09f,.95f),S(.88f,1.11f),S(1.12f,.87f)}
        };
        static int Row(int category)
        {
            if(category>=2&&category<=5)return category-2;
            if(category==13)return 4;
            throw new ArgumentOutOfRangeException(nameof(category));
        }
        public static string[] Names(int category)
        {
            switch(category){case 2:return Eyes;case 3:return Brows;case 4:return Noses;case 5:return Mouths;case 13:return Ears;default:throw new ArgumentOutOfRangeException(nameof(category));}
        }
        public static Vector4 Shape(int category,int option)
        {
            var row=shapes[Row(category)];
            if(option<0||option>=row.Length)throw new ArgumentOutOfRangeException(nameof(option));
            return row[option];
        }
    }
}
