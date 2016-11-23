using UnityEngine;
using System.Collections;

namespace ParticleFlow
{
    [ExecuteInEditMode]
    public class Path : Sampler
    {
        Node _interpNode;

        Node interpNode
        {
            get
            {
                if (_interpNode == null)
                {
                    var go = new GameObject();
                    go.hideFlags = HideFlags.HideAndDontSave;
                    _interpNode = go.AddComponent<Node>();
                }
                return _interpNode;
            }
        }

        Node[] nodes;

        [ContextMenu("Reset")]
        void Reset()
        {
            if (_interpNode != null)
            {
                DestroyImmediate(_interpNode.gameObject);
                _interpNode = null;
            }
            CollectNodes();
        }

        void OnEnable()
        {
            Reset();
        }

        void OnTransformChildrenChanged()
        {
            CollectNodes();
            SendMessageUpwards("CollectNodes", null, SendMessageOptions.DontRequireReceiver);
        }

        void CollectNodes()
        {
            nodes = GetComponentsInChildren<Node>();
        }

        override public void Sample(Vector3 position, out Vector3 velocity, out Color color)
        {
            Vector3 rv = Vector3.zero;
            float mSum = 0;            
            Color avgColor = Color.black;
            for (int i = 0; i < nodes.Length - 1; ++i)
            {
                Vector3 v;
                Color c;
                SampleInterpolated(position, nodes[i], nodes[i + 1], out v, out c);
                rv += v;
                float m = v.magnitude;
                mSum += m;
                avgColor += c * m;
            }
            avgColor /= Mathf.Max(.0000001f, mSum); // prevent div by 0
            avgColor.a = Mathf.Clamp01(avgColor.a);
            color = avgColor;
            velocity = rv;
        }

        void SampleInterpolated(Vector3 position, Node node0, Node node1, out Vector3 velocity, out Color color)
        {
            float ratio = .0f;
            Vector3 interpolatedPosition = 
                Util.ClosestInterpolatedPoint(
                    node0.transform.position, node0.transform.forward, 
                    node1.transform.position, node1.transform.forward, position, out ratio);

            const float d = .01f;
            Vector3 positionMin = Util.Hermite(node0.transform.position, node0.transform.forward, node1.transform.position, node1.transform.forward, Mathf.Clamp01(ratio - d));
            Vector3 positionMax = Util.Hermite(node0.transform.position, node0.transform.forward, node1.transform.position, node1.transform.forward, Mathf.Clamp01(ratio + d));
            Vector3 forward = (positionMax - positionMin).normalized;

            var node = interpNode;
            node.transform.localPosition = interpolatedPosition;
            node.transform.forward = forward;
            node.Lerp(node0, node1, ratio);

            node.Sample(transform.TransformPoint(position), out velocity, out color);
        }
    }
}


