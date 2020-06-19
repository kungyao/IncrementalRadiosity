using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SampleTest : MonoBehaviour
{
    public enum SampleMode
    {
        UniformCircle = 1,
        UniformCircleWithoutOffset = 2,
        Halton = 3,
        HaltonWithoutOffset = 4,
    }
    public bool useToResample = false;
    public SampleMode sampleMode = SampleMode.UniformCircle;
    public float gizmosRadius = 0.1f;
    public float radius = 1.0f;
    public int samples = 128;

    List<Vector2> vpls;
    private void OnValidate()
    {
        if (sampleMode == SampleMode.UniformCircle)
            vpls = UniformCircle.calculatePoint(radius, samples, true, true);
        else if (sampleMode == SampleMode.UniformCircleWithoutOffset)
            vpls = UniformCircle.calculatePoint(radius, samples, false, true);
        else if (sampleMode == SampleMode.Halton)
            vpls = UniformCircle.HaltonGenerator(radius, samples, true);
        else if (sampleMode == SampleMode.HaltonWithoutOffset)
            vpls = UniformCircle.HaltonGenerator(radius, samples, false);
    }
    private void OnDrawGizmos()
    {
        if (vpls == null)
        {
            if (sampleMode == SampleMode.UniformCircle)
                vpls = UniformCircle.calculatePoint(radius, samples, true, true);
            else if (sampleMode == SampleMode.UniformCircleWithoutOffset)
                vpls = UniformCircle.calculatePoint(radius, samples, false, true);
            else if (sampleMode == SampleMode.Halton)
                vpls = UniformCircle.HaltonGenerator(radius, samples, true);
            else if (sampleMode == SampleMode.HaltonWithoutOffset)
                vpls = UniformCircle.HaltonGenerator(radius, samples, false);
        }
        foreach (Vector2 p in vpls)
        {
            Gizmos.DrawSphere(p, gizmosRadius);
        }
    }
}
