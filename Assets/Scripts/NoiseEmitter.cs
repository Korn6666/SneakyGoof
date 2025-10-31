using UnityEngine;
using System;

public class NoiseEmitter : MonoBehaviour
{
    [SerializeField] private float baseNoiseLevel = 5f;

    public void MakeNoise(float multiplier = 1f)
    {
        float noise = baseNoiseLevel * multiplier;
        Eye_Behaviour.OnNoiseEmitted?.Invoke(transform.position, noise);
    }
}
