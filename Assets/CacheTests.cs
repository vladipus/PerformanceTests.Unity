using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEngine;

public class CacheTests : MonoBehaviour
{
    public const int Count = 1 << 18;

    struct StructData
    {
        public Vector3 position;
        public Vector3 acceleration;
        public Vector3 velocity;
        public Quaternion rotation;
    }

    class ClassData
    {
        public Vector3 position;
        public Vector3 acceleration;
        public Vector3 velocity;
        public Quaternion rotation;
    }

    ClassData[] classes = null;
    NativeArray<StructData> structs;

    void Start()
    {
        classes = new ClassData[Count];
        for (int i = 0; i < classes.Length; i++)
            classes[i] = new ClassData();
        classes = classes.OrderBy(a => Guid.NewGuid()).ToArray();
        structs = new NativeArray<StructData>(Count, Allocator.Persistent);
    }

    void OnDestroy()
    {
        structs.Dispose();
    }

    void Update()
    {
        var deltaTime = Time.deltaTime;
        for (int i = 0; i < Count; i++)
        {
#if USE_STRUCTS
            StructData result = structs[i];
#else
            ClassData result = classes[i];
#endif

            result.position += result.velocity * deltaTime;
            result.rotation = result.rotation *
                            Quaternion.AngleAxis(30 * deltaTime, Vector3.up) *
                            Quaternion.AngleAxis(45 * deltaTime, Vector3.forward);
            result.velocity += result.acceleration * deltaTime;

            result.acceleration = (Vector3.zero - result.position) * 0.5f;

#if !NO_INTERACTION
            for (int j = 0; j < Count; j++)
            {
                if (i == j) continue;
#if USE_STRUCTS
                StructData other = structs[j];
#else
                ClassData other = classes[j];
#endif
                var delta = other.position - result.position;
                var distance = delta.magnitude;
                if (distance <= 0.001f) continue;
                var dir = delta / distance;
                result.acceleration -= dir * 0.01f / distance;
            }
#endif

            result.acceleration /= 1.2f * 10;
#if USE_STRUCTS
            structs[i] = result;
#endif
        }
    }
}
