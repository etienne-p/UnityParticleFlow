using UnityEngine;
using System.Collections;

namespace ParticleFlow
{
    public class Sampler : MonoBehaviour
    {
        virtual public void Sample(Vector3 position, out Vector3 velocity, out Color color)
        {
            velocity = Vector3.zero;
            color = Color.white;
        }
    }
}

