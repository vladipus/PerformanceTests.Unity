using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public class TestableManager : MonoBehaviour
{
#if UPDATE_MANAGER

    void CloneExisting()
    {
        var all = new List<Testable>(Testable.All);
        foreach (var t in all)
        {
            if (t.isClone) continue;
            for (int i = 0; i < Testable.clonesCount; i++)
            {
                var clone = Instantiate(t);
                clone.isClone = true;
                clone.transform.position = UnityEngine.Random.insideUnitSphere * (t.transform.position - Testable.center).magnitude;
                clone.transform.localRotation = UnityEngine.Random.rotationUniform;
            }
        }
    }

#if USE_BURST
    struct TestableData
    {
        public float mass;
        public float4 position;
        public float4 velocity;
        public float4 acceleration;
        public Quaternion rotation;
    }

    NativeArray<TestableData> input;
    NativeArray<TestableData> output;

    void Start()
    {
        CloneExisting();

        input = new NativeArray<TestableData>(Testable.All.Count, Allocator.Persistent);
        output = new NativeArray<TestableData>(Testable.All.Count, Allocator.Persistent);

        int i = 0;
        foreach (var t in Testable.All)
        {
            var d = new TestableData();
            d.position = new float4(t.transform.position.x, t.transform.position.y, t.transform.position.z, 0);
            d.mass = t.mass;
            d.rotation = t.transform.rotation;
            input[i] = d;
            i += 1;
        }
    }

    void Update()
    {
        var job = new MyJob
        {
            center = new float4(Testable.center.x, Testable.center.y, Testable.center.z, 0),
            deltaTime = Time.deltaTime,
            Input = input,
            Output = output
        };

        job.Schedule().Complete();

        int i = 0;
        foreach (var t in Testable.All)
        {
            var o = output[i];
            t.transform.position = new Vector3(o.position.x, o.position.y, o.position.z);
            t.transform.rotation = o.rotation;
            input[i] = o;
            i += 1;
        }
    }

    void OnDestroy()
    {
        input.Dispose();
        output.Dispose();
    }

    // Using BurstCompile to compile a Job with burst
    // Set CompileSynchronously to true to make sure that the method will not be compiled asynchronously
    // but on the first schedule
    [BurstCompile(CompileSynchronously = true)]
    private struct MyJob : IJob
    {
        [ReadOnly]
        public float deltaTime;

        [ReadOnly]
        public float4 center;

        [ReadOnly]
        public NativeArray<TestableData> Input;

        [WriteOnly]
        public NativeArray<TestableData> Output;

        public void Execute()
        {
            for (int i = 0; i < Input.Length; i++)
            {
                TestableData result = Input[i];

                result.position += result.velocity * deltaTime;
                result.rotation = result.rotation *
                                Quaternion.AngleAxis(30 * deltaTime, Vector3.up) *
                                Quaternion.AngleAxis(45 * deltaTime, Vector3.forward);
                result.velocity += result.acceleration * deltaTime;

                result.acceleration = (center - result.position) * 0.5f;

                for (int j = 0; j < Input.Length; j++)
                {
                    if (i == j) continue;
                    var other = Input[j];
                    var delta = other.position - result.position;
                    var distance = math.length(delta);
                    if (distance <= 0.001f) continue;
                    var dir = delta / distance;
                    result.acceleration -= dir * 0.01f / distance;
                }

                result.acceleration /= result.mass * 10;

                Output[i] = result;
            }
        }
    }
#else // USE_BURST
    void Start()
    {
        CloneExisting();
    }

    // Update is called once per frame
    void Update()
    {
        foreach (var t in Testable.All)
        {
            var position = t.transform.position;
            position += t.velocity * Time.deltaTime;

            position.x = Mathf.Clamp(position.x, -Testable.Max, Testable.Max);
            position.y = Mathf.Clamp(position.y, -Testable.Max, Testable.Max);
            position.z = Mathf.Clamp(position.z, -Testable.Max, Testable.Max);

            t.transform.position = position;
            t.transform.Rotate(Vector3.up, 30 * Time.deltaTime);
            t.transform.Rotate(Vector3.forward, 45 * Time.deltaTime);
            t.velocity += t.acceleration * Time.deltaTime;

            t.acceleration = (Testable.center - position) * 0.5f;

            foreach (var other in Testable.All)
            {
                if (other == this) continue;
                var delta = other.transform.position - position;
                var distance = delta.magnitude;
                if (distance <= 0.001f) continue;
                var dir = delta / distance;
                t.acceleration -= dir * 0.01f / distance;
            }

            t.acceleration /= t.mass * 10;
        }
    }

#endif // !USE_BURST
#endif // UPDATE_MANAGER
}
