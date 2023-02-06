using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityVFXLibrary.Runtime
{
    [ExecuteInEditMode]
    public class WindController : MonoBehaviour
    {
        [Range(0.0f, 1.0f)]
        public float WindStrength = 0.35f;
        [Range(0.0f, 3.0f)]
        public float LeafFlutterStrength = 1.0f;
        [Range(0.0f, 3.0f)]
        public float WindSpeed = 1.0f;
        public Texture2D WindMask;
        private Vector3 WindDirection;

        private void Start()
        {
            Shader.SetGlobalTexture("Wind_Mask", WindMask);
        }
        private void Update()
        {
            Shader.SetGlobalFloat("Wind_Strength", WindStrength);
            Shader.SetGlobalFloat("Leaf_Flutter", LeafFlutterStrength);
            Shader.SetGlobalFloat("Wind_Speed", WindSpeed);

            WindDirection = transform.right;
            Shader.SetGlobalVector("Wind_Direction", WindDirection);
        }
    }
}

