using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Testable : MonoBehaviour
{
    public const int clonesCount = 128;
    public const int Max = 1000;
    public static Vector3 center = Vector3.zero;

#if !BURST_COMPILE
    public Vector3 acceleration = Vector3.zero;
    public Vector3 velocity = Vector3.zero;
#endif

    public float mass = 1;

    public bool isClone = false;

    public static List<Testable> All = new List<Testable>();

    [System.NonSerialized]
    public new Transform transform;

    [RuntimeInitializeOnLoadMethod]
    static void InitRandom()
    {
        Random.InitState(0);
    }

    void OnEnable()
    {
        All.Add(this);

        transform = base.transform;

        Bounds bounds = default;
        var renderers = GetComponentsInChildren<Renderer>();
        foreach (var r in renderers)
        {
            bounds.Encapsulate(r.bounds);
        }
        mass = bounds.size.x * bounds.size.y * bounds.size.z;
    }

    void OnDisable()
    {
        All.Remove(this);
    }

#if !UPDATE_MANAGER
    void Start()
    {
        var t = this;
        if (isClone) return;
        for (int i = 0; i < clonesCount; i++)
        {
            var clone = Instantiate(t);
            clone.isClone = true;
            clone.transform.position = Random.insideUnitSphere * (t.transform.position - center).magnitude;
            clone.transform.localRotation = Random.rotationUniform;
        }
    }

    void Update()
    {
        var t = this;

        var position = transform.position;
        position += velocity * Time.deltaTime;

        position.x = Mathf.Clamp(position.x, -Max, Max);
        position.y = Mathf.Clamp(position.y, -Max, Max);
        position.z = Mathf.Clamp(position.z, -Max, Max);

        transform.position = position;
        transform.Rotate(Vector3.up, 30 * Time.deltaTime);
        transform.Rotate(Vector3.forward, 45 * Time.deltaTime);
        velocity += acceleration * Time.deltaTime;

        acceleration = (center - position) * 0.5f;

        foreach (var other in Testable.All)
        {
            if (other == this) continue;
            var delta = other.transform.position - position;
            var distance = delta.magnitude;
            if (distance <= 0.001f) continue;
            var dir = delta / distance;
            t.acceleration -= dir * 0.01f / distance;
        }

        acceleration /= mass * 10;
    }
#endif
}
