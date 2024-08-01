using System;
using Snapser.UI;
using UnityEngine;

namespace DefaultNamespace
{
    public class EnvironmentObject : MonoBehaviour
    {
        private Vector3 startTransformPosition;
        private Quaternion startTransformRotation;
        private Rigidbody2D rigidbody2D;
        
        private void Awake()
        {
            startTransformPosition = transform.position;
            startTransformRotation = transform.rotation;
            rigidbody2D = GetComponent<Rigidbody2D>();
            GameUI.OnInitializeUI += InitializeEnvironmentObject;
        }

        private void OnDestroy()
        {
            GameUI.OnInitializeUI -= InitializeEnvironmentObject;
        }

        private void InitializeEnvironmentObject()
        {
            rigidbody2D.velocity = Vector2.zero;
            rigidbody2D.angularVelocity = 0f;
            transform.position = startTransformPosition;
            transform.rotation = startTransformRotation;
        }
    }
}