using UnityEngine;

namespace Minglu
{
    // A full beard is a facial surface, not a larger moustache. The mouth
    // opening remains registered while the outer columns wrap to the temples
    // and mandibular edge. Coordinates are measured in the original painting.
    public static class BeardContour
    {
        public const int Columns=6, Rows=5, Count=Columns*Rows;
        static readonly float[] X={.10f,.235f,.350f,.470f,.650f,.88f};
        static readonly float[] Y={.22f,.49f,.602f,.75f,.90f};
        static readonly Vector4[][] Cache=new Vector4[16][];
        static readonly Vector2[] Target={
            new Vector2(.273f,.365f),new Vector2(.360f,.401f),new Vector2(.414f,.402f),new Vector2(.487f,.390f),new Vector2(.603f,.370f),new Vector2(.731f,.374f),
            new Vector2(.301f,.464f),new Vector2(.360f,.466f),new Vector2(.414f,.458f),new Vector2(.487f,.437f),new Vector2(.603f,.408f),new Vector2(.726f,.428f),
            new Vector2(.314f,.518f),new Vector2(.360f,.490f),new Vector2(.414f,.492f),new Vector2(.487f,.494f),new Vector2(.603f,.486f),new Vector2(.720f,.490f),
            new Vector2(.349f,.552f),new Vector2(.396f,.563f),new Vector2(.449f,.568f),new Vector2(.520f,.559f),new Vector2(.586f,.548f),new Vector2(.661f,.535f),
            new Vector2(.370f,.578f),new Vector2(.408f,.592f),new Vector2(.464f,.600f),new Vector2(.530f,.584f),new Vector2(.569f,.574f),new Vector2(.615f,.559f)
        };
        public static bool Enabled(Appearance look){return look.sex==0&&look.parts[6]==4;}
        public static Vector4 Vertex(Appearance look,int index)
        {
            var f=PaintedPortraitLayout.Landmarks(look);var p=Target[index];
            float center=Mathf.Lerp(f.mouth.x,f.chin.x,Mathf.Clamp01((p.y-.492f)/(.568f-.492f)));
            float referenceCenter=Mathf.Lerp(.414f,.457f,Mathf.Clamp01((p.y-.492f)/(.568f-.492f)));
            float dx=p.x-referenceCenter;
            float width=dx<0?(f.mouth.x-f.eyeFar.x)/(.414f-.312f):(f.earNear.x-f.mouth.x)/(.758f-.414f);
            float y=p.y<.492f?f.mouth.y+(p.y-.492f)*(f.mouth.y-f.earNear.y)/(.492f-.383f)
                :f.mouth.y+(p.y-.492f)*(f.chin.y-f.mouth.y)/(.568f-.492f);
            return new Vector4(center+dx*width,y,X[index%Columns],Y[index/Columns]);
        }
        public static Vector4[] Vertices(Appearance look)
        {
            int key=look.sex*8+look.parts[0];
            if(Cache[key]==null){var result=new Vector4[Count];for(int i=0;i<Count;i++)result[i]=Vertex(look,i);Cache[key]=result;}
            return Cache[key];
        }
        public static float Cross(Vector2 a,Vector2 b){return a.x*b.y-a.y*b.x;}
        public static bool Triangle(Vector2 p,Vector4 a,Vector4 b,Vector4 c,out Vector2 source)
        {
            var ab=new Vector2(b.x-a.x,b.y-a.y);var ac=new Vector2(c.x-a.x,c.y-a.y);var ap=p-new Vector2(a.x,a.y);
            float det=Cross(ab,ac),u=Cross(ap,ac)/det,v=Cross(ab,ap)/det;
            source=new Vector2(a.z,a.w)+u*new Vector2(b.z-a.z,b.w-a.w)+v*new Vector2(c.z-a.z,c.w-a.w);
            return u>=-.00001f&&v>=-.00001f&&u+v<=1.00001f;
        }
        public static Vector2 Sample(Appearance look,Vector2 p)
        {
            Vector2 q;var vertices=Vertices(look);
            for(int row=0;row<Rows-1;row++)for(int col=0;col<Columns-1;col++)
            {
                int a=row*Columns+col,b=a+1,c=a+Columns,d=c+1;
                if(Triangle(p,vertices[a],vertices[b],vertices[d],out q)||Triangle(p,vertices[a],vertices[d],vertices[c],out q))return q;
            }
            return new Vector2(-1,-1);
        }
    }
}
