using UnityEngine;
using System.Collections;

public class Rotate : MonoBehaviour
{
    public float speed = 1.0f;

    void Update()
    {
        transform.rotation = Quaternion.AngleAxis(speed * Time.time, Vector3.up);
    }
}
