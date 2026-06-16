using UnityEngine;
using System;
using System.Collections.Generic;

namespace Car.WheelsManagement
{
    public class WheelColliderAutoSetup : MonoBehaviour
    {
        [Serializable]
        public class WheelSetupData
        {
            public string name;
            public Transform wheelMesh;
            public WheelCollider wheelCollider;
            public Wheel wheelComponent;
        }

        public WheelSetupData frontLeft;
        public WheelSetupData frontRight;
        public WheelSetupData rearLeft;
        public WheelSetupData rearRight;
        public Transform wheelCollidersParent;
        public Transform carBodyMesh;

        [Header("Stability Settings")]
        public float carMass = 1500f;
        public float suspensionDistance = 0.2f;
        public float springStiffness = 0.75f; // Increased for better support
        public float damperRatio = 0.3f; // Increased for better stability
        public float drag = 0.05f;
        public float angularDrag = 0.5f;

        [Header("Center of Mass")]
        public Vector3 centerOfMassOffset = new Vector3(0, -0.4f, 0);
        public bool autoCalculateCOM = true;

        public void SetupWheels()
        {
            Rigidbody rb = GetComponentInParent<Rigidbody>();
            if (rb != null)
            {
                rb.mass = carMass;
                rb.drag = drag;
                rb.angularDrag = angularDrag;
                rb.interpolation = RigidbodyInterpolation.Interpolate;
                rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

                if (autoCalculateCOM && carBodyMesh != null)
                {
                    MeshRenderer renderer = carBodyMesh.GetComponentInChildren<MeshRenderer>();
                    if (renderer != null)
                    {
                        // Set COM to the bottom area of the body bounds
                        Vector3 localScale = carBodyMesh.lossyScale;
                        Vector3 localCOM = carBodyMesh.InverseTransformPoint(renderer.bounds.center);
                        localCOM.y -= (renderer.bounds.extents.y * 0.5f); // Move towards bottom
                        rb.centerOfMass = localCOM + centerOfMassOffset;
                    }
                }
                else
                {
                    rb.centerOfMass = centerOfMassOffset;
                }
            }

            SetupWheel(frontLeft, "FrontLeft", rb);
            SetupWheel(frontRight, "FrontRight", rb);
            SetupWheel(rearLeft, "RearLeft", rb);
            SetupWheel(rearRight, "RearRight", rb);

            // Ignore collisions with car body
            if (rb != null)
            {
                Collider[] carColliders = rb.GetComponentsInChildren<Collider>();
                WheelCollider[] wheelColliders = { frontLeft.wheelCollider, frontRight.wheelCollider, rearLeft.wheelCollider, rearRight.wheelCollider };
                
                foreach (var carCol in carColliders)
                {
                    foreach (var wheelCol in wheelColliders)
                    {
                        if (wheelCol != null && carCol != null && carCol != wheelCol)
                        {
                            Physics.IgnoreCollision(wheelCol, carCol);
                        }
                    }
                }
            }
            
            // Set Physics Timestep for better simulation
            Time.fixedDeltaTime = 0.01f; // Recommended for car physics
            
            Debug.Log("Wheel Setup Complete with High-Stability Tweaks! Fixed Timestep set to 0.01s.");
        }

        private void SetupWheel(WheelSetupData data, string defaultName, Rigidbody carRigidbody)
        {
            if (data.wheelMesh == null)
            {
                Debug.LogWarning($"Mesh for {defaultName} is not assigned.");
                return;
            }

            if (Mathf.Abs(data.wheelMesh.lossyScale.x - data.wheelMesh.lossyScale.y) > 0.01f)
            {
                Debug.LogWarning($"{defaultName} has non-uniform scale! This often causes WheelCollider instability.");
            }

          
            float radius = CalculateRadius(data.wheelMesh);
            
        
            if (data.wheelCollider == null)
            {
                if (wheelCollidersParent != null)
                {
                    Transform existing = wheelCollidersParent.Find(defaultName + "_Collider");
                    if (existing != null) data.wheelCollider = existing.GetComponent<WheelCollider>();
                }
            }

            if (data.wheelCollider == null)
            {
                GameObject colliderGo = new GameObject(defaultName + "_Collider");
                
#if UNITY_EDITOR
                UnityEditor.Undo.RegisterCreatedObjectUndo(colliderGo, "Create Wheel Collider");
#endif

                if (wheelCollidersParent != null)
                {
                    colliderGo.transform.SetParent(wheelCollidersParent);
                }
                else
                {
                    colliderGo.transform.SetParent(this.transform);
                }
                data.wheelCollider = colliderGo.AddComponent<WheelCollider>();
            }

            // 3. Align WheelCollider with Mesh
            data.wheelCollider.transform.position = data.wheelMesh.position;
            data.wheelCollider.transform.rotation = this.transform.rotation; 

            // 4. Set Proportions
            data.wheelCollider.radius = radius;
            data.wheelCollider.suspensionDistance = suspensionDistance;

            // 5. Setup Wheel Component
            if (data.wheelComponent == null)
            {
                data.wheelComponent = data.wheelMesh.GetComponent<Wheel>();
                if (data.wheelComponent == null)
                {
                    data.wheelComponent = data.wheelMesh.gameObject.AddComponent<Wheel>();
#if UNITY_EDITOR
                    UnityEditor.Undo.AddComponent<Wheel>(data.wheelMesh.gameObject);
#endif
                }
            }

            data.wheelComponent.collider = data.wheelCollider;
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(data.wheelComponent);
#endif
            
        
            float currentMass = (carRigidbody != null ? carRigidbody.mass : carMass);
            float loadPerWheel = currentMass * 9.81f / 4f;
            
          
            float k = loadPerWheel / (suspensionDistance * 0.3f);
            
            JointSpring spring = data.wheelCollider.suspensionSpring;
            spring.spring = k * springStiffness;
      
            spring.damper = 2f * Mathf.Sqrt(loadPerWheel * spring.spring) * damperRatio;
            
            data.wheelCollider.suspensionSpring = spring;
            
         
            data.wheelCollider.forceAppPointDistance = 0.05f; 
        }

        private float CalculateRadius(Transform meshTransform)
        {
            float radius = 0.35f; 

            // Try to get from MeshFilter's sharedMesh
            MeshFilter filter = meshTransform.GetComponentInChildren<MeshFilter>();
            if (filter != null && filter.sharedMesh != null)
            {
                // Calculate local radius based on bounds
                Vector3 size = filter.sharedMesh.bounds.size;
                Vector3 worldScale = meshTransform.lossyScale;
                
                // Assume the largest cross-section is the diameter
                // Wheels are usually aligned such that Y and Z are the height/width
                float diameter = Mathf.Max(size.y * worldScale.y, size.z * worldScale.z);
                radius = diameter * 0.5f;
            }
            else
            {
                // Fallback to MeshRenderer bounds
                MeshRenderer renderer = meshTransform.GetComponentInChildren<MeshRenderer>();
                if (renderer != null)
                {
                    radius = renderer.bounds.extents.y;
                }
            }
            
            return radius;
        }
    }
}
