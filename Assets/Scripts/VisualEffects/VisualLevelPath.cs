using UnityEngine;

namespace RhythmRehab
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class VisualLevelPath : UnityEngine.UI.MaskableGraphic
    {
        [SerializeField] private Vector2[] points;
        [SerializeField, Min(.1f)] private float lineWidth = 2f;
        [SerializeField] private bool closed;
        [SerializeField] private bool fillToBottom;
        [SerializeField] private float bottom = -430f;

        protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper vh)
        {
            vh.Clear();if(points==null||points.Length<2)return;
            int segments=closed?points.Length:points.Length-1;
            for(int i=0;i<segments;i++)
            {
                Vector2 a=points[i],b=points[(i+1)%points.Length];
                int start=vh.currentVertCount;
                if(fillToBottom)
                {
                    vh.AddVert(new Vector2(a.x,bottom),color,Vector2.zero);vh.AddVert(a,color,Vector2.zero);
                    vh.AddVert(b,color,Vector2.zero);vh.AddVert(new Vector2(b.x,bottom),color,Vector2.zero);
                }
                else
                {
                    var normal=new Vector2(-(b-a).y,(b-a).x).normalized*lineWidth*.5f;
                    vh.AddVert(a-normal,color,Vector2.zero);vh.AddVert(a+normal,color,Vector2.zero);
                    vh.AddVert(b+normal,color,Vector2.zero);vh.AddVert(b-normal,color,Vector2.zero);
                }
                vh.AddTriangle(start,start+1,start+2);vh.AddTriangle(start+2,start+3,start);
            }
        }
    }
}
