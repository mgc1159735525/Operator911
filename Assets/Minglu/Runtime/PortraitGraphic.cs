using UnityEngine;
using UnityEngine.UI;
namespace Minglu
{
    // Original vector layers, all authored in a common 400 x 540 portrait frame.
    public class PortraitGraphic:MaskableGraphic
    {
        public Appearance appearance;
        VertexHelper mesh;float scale,ox,oy;
        Color C(string h){Color c;ColorUtility.TryParseHtmlString("#"+h,out c);return c*color;}
        void Poly(Color c,params float[] xy)
        {
            int first=mesh.currentVertCount;
            for(int i=0;i<xy.Length;i+=2)mesh.AddVert(new Vector3(ox+xy[i]*scale,oy-xy[i+1]*scale,0),c,Vector2.zero);
            for(int i=1;i<xy.Length/2-1;i++)mesh.AddTriangle(first,first+i,first+i+1);
        }
        void Oval(float x,float y,float rx,float ry,Color c)
        {float[] points=new float[64];for(int i=0;i<32;i++){float t=i*Mathf.PI/16;points[i*2]=x+rx*Mathf.Cos(t);points[i*2+1]=y+ry*Mathf.Sin(t);}Poly(c,points);}
        void Stroke(float x,float y,float ex,float ey,float width,Color c)
        {var d=new Vector2(ex-x,ey-y).normalized;float nx=-d.y*width/2,ny=d.x*width/2;Poly(c,x+nx,y+ny,ex+nx,ey+ny,ex-nx,ey-ny,x-nx,y-ny);}
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();mesh=vh;if(appearance==null)return;var p=appearance.parts;
            var r=rectTransform.rect;scale=Mathf.Min(r.width/400,r.height/540);ox=r.center.x-200*scale;oy=r.center.y+270*scale;
            Color bg=C(new[]{"243D3B","2D4053","584437","D4C4A6"}[p[9]]),ink=C("20292A"),skin=C(new[]{"E4C4A2","CDA77D","B98A61","8D6048"}[p[10]]),shade=Color.Lerp(skin,ink,.23f),hair=C(new[]{"24292B","503A30","797976","BFC1B8"}[p[12]]),cloth=C(new[]{"354D65","39594E","87483B","A08956","424548","65506D"}[p[11]]),light=Color.Lerp(cloth,C("DDD0AE"),.23f);
            Poly(bg,0,0,400,0,400,540,0,540);
            Oval(200,210,166,188,Color.Lerp(bg,C("CFB67E"),.13f));
            for(int i=0;i<5;i++){Stroke(20+i*17,540,53+i*24,25,1,Color.Lerp(bg,C("CFB67E"),.15f));}
            if(p[1]==2){Oval(202,245,116,170,hair);Poly(hair,93,232,301,225,327,437,73,437);}
            Poly(ink,32,540,47,408,136,365,265,365,351,408,375,540);
            Poly(cloth,40,540,55,412,143,372,261,372,345,412,366,540);
            Poly(shade,159,299,244,299,250,387,201,435,148,383);
            Poly(skin,171,308,237,308,238,377,200,411,160,379);
            if(p[7]==0){Poly(light,143,365,203,409,166,494,119,389);Poly(light,259,365,277,393,209,509,167,493);Stroke(203,409,168,488,3,ink);}
            if(p[7]==1){Poly(light,148,370,198,402,249,370,257,430,143,430);Stroke(200,404,200,540,4,ink);for(int i=0;i<4;i++)Oval(215,437+i*25,4,4,C("C7AA70"));}
            if(p[7]==2){Poly(light,142,359,200,411,253,359,338,411,285,454,206,431,115,461,61,412);Stroke(201,416,207,536,4,ink);}
            if(p[7]==3){Poly(light,253,370,279,385,145,540,111,540);Poly(ink,60,465,347,465,352,489,55,489);Oval(204,478,15,12,C("B39C70"));}
            Stroke(85,447,66,532,3,light);Stroke(312,451,332,537,3,ink);
            Oval(109,233,20,35,shade);Oval(291,233,20,35,shade);
            float fw=p[0]==0?87:p[0]==1?99:p[0]==2?106:80;
            Poly(shade,200-fw,137,200+fw,137,200+fw,271,255,327,202,352,145,323,200-fw,272);
            Poly(skin,200-fw+5,145,200+fw-7,145,200+fw-9,265,248,316,199,340,153,314,200-fw+7,264);
            Poly(Color.Lerp(skin,shade,.5f),201+fw-10,162,201+fw-9,265,248,316,212,335,249,283,264,203);
            Oval(200,133,104,68,hair);
            if(p[1]==0){Oval(200,56,44,35,hair);Poly(hair,103,137,146,111,139,200,110,214);Poly(hair,247,112,297,137,289,213,264,192);Stroke(165,66,232,66,9,C("B59C6C"));}
            if(p[1]==1){Poly(hair,107,155,226,103,250,130,164,182,104,202);Poly(hair,252,124,299,148,288,222,267,196);}
            if(p[1]==2){Poly(hair,103,146,174,109,149,165,121,290,91,329);Poly(hair,219,110,296,146,302,325,273,280,258,168);}
            if(p[1]==3){Poly(hair,105,139,295,138,286,177,257,155,233,176,211,156,180,178,157,155,116,182);}
            Color brow=hair;float by=p[3]==1?197:200,bw=p[3]==2?3:p[3]==3?9:6;
            Stroke(133,by,176,by+(p[3]==1?-8:2),bw,brow);Stroke(222,by+(p[3]==1?-8:2),267,by,bw,brow);
            float ey=224,eh=p[2]==1?5:p[2]==2?8:7;
            Oval(157,ey,22,eh,C("DED7BB"));Oval(241,ey,22,eh,C("DED7BB"));
            Oval(158,ey,5,p[2]==3?7:5,ink);Oval(240,ey,5,p[2]==3?7:5,ink);
            Stroke(134,ey-3,177,ey-(p[2]==1?8:3),3,ink);Stroke(220,ey-(p[2]==1?8:3),262,ey-3,3,ink);
            float nx=p[4]==2?13:p[4]==3?11:8,ny=p[4]==1?260:p[4]==3?280:272;
            Poly(shade,202,221,202+nx,ny,195-nx,ny+2,196,262);Stroke(188,ny+3,207,ny+3,2,shade);
            float my=p[5]==1?295:298;Color lips=Color.Lerp(skin,C("713F35"),.55f);
            Stroke(174,my,199,my+(p[5]==1?5:0),p[5]==3?6:3,lips);Stroke(199,my+(p[5]==1?5:0),226,my,3,lips);
            if(p[5]==2)Stroke(177,305,222,305,2,shade);
            if(p[6]==1){Poly(hair,156,309,194,324,246,308,229,339,198,351,172,335);}
            if(p[6]==2){Poly(hair,156,306,185,321,209,322,246,306,237,355,203,397,170,356);Stroke(199,337,200,377,2,Color.Lerp(hair,skin,.3f));}
            if(p[6]==3){Poly(hair,192,282,198,289,166,304,169,292);Poly(hair,204,282,231,292,235,304,199,289);}
            if(p[8]==1){Oval(157,227,30,24,C("B59C70"));Oval(241,227,30,24,C("B59C70"));Oval(157,227,27,21,skin);Oval(241,227,27,21,skin);Oval(157,227,5,6,ink);Oval(241,227,5,6,ink);Stroke(185,224,212,224,3,ink);Stroke(107,217,128,222,3,ink);Stroke(270,222,292,217,3,ink);}
            if(p[8]==2){Oval(111,276,6,12,C("CEBA7D"));Oval(289,276,6,12,C("CEBA7D"));}
            if(p[8]==3){Stroke(253,210,237,263,3,C("956955"));Stroke(244,230,252,234,2,shade);}
            Stroke(17,17,383,17,1,C("A08F69"));Stroke(17,523,383,523,1,C("A08F69"));
        }
    }
}
