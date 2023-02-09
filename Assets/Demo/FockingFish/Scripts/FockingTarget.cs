using UnityEngine;

namespace Fern.VFX.Demo
{
    public class FockingTarget : MonoBehaviour
    {
        private Vector2 prevPosition = Vector2.zero;

        Vector2 GetMousePosition()
        {
            var camera = Camera.main;
            var height = -camera.transform.position.z * Mathf.Tan(camera.fieldOfView * 0.5f * Mathf.PI / 180f) * 2f;
            var aspectRatio = (float)Screen.width / Screen.height;
            var width = height * aspectRatio;
            var normalizedMousePos = (Input.mousePosition / new Vector2(Screen.width, Screen.height)) - new Vector2(0.5f, 0.5f);
            return normalizedMousePos * new Vector2(width, height);
        }
        
        void FixedUpdate()
        {
            var position = GetMousePosition() * 0.1f + prevPosition * 0.9f;
            transform.position = position;
            var deltaPosition = position - prevPosition;
            var velocity = new Vector3(deltaPosition.x, deltaPosition.y, -0.002f) / Time.deltaTime;
            transform.rotation = Quaternion.LookRotation(velocity, Vector3.up);
            prevPosition = position;
        }
    }
}
