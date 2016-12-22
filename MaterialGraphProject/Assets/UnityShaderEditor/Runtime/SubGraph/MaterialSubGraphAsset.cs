using System.Linq;
using UnityEngine.Graphing;

namespace UnityEngine.MaterialGraph
{
    public class MaterialSubGraphAsset : ScriptableObject, IMaterialGraphAsset
    {
        [SerializeField]
        private SubGraph m_MaterialSubGraph = new SubGraph();

        public IGraph graph
        {
            get { return m_MaterialSubGraph; }
        }

        public SubGraph subGraph
        {
            get { return m_MaterialSubGraph; }
        }

        public bool shouldRepaint
        {
            get { return graph.GetNodes<AbstractMaterialNode>().OfType<IRequiresTime>().Any(); }
        }

        public ScriptableObject GetScriptableObject()
        {
            return this;
        }

        public void OnEnable()
        {
            graph.OnEnable();
        }

        public void PostCreate()
        {
            m_MaterialSubGraph.PostCreate();
        }

        [SerializeField]
        private GraphDrawingData m_DrawingData = new GraphDrawingData();

        public GraphDrawingData drawingData
        {
            get { return m_DrawingData; }
        }
    }
}
