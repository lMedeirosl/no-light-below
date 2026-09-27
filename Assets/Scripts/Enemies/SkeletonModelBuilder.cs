using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using NoLightBelow.Environment;

namespace NoLightBelow.Enemies
{
    public static class SkeletonModelBuilder
    {
        public static GameObject CreateSkeleton(Vector3 spawnPosition)
        {
            GameObject root = new GameObject("Crypt_Skeleton_Warrior");
            root.transform.position = spawnPosition;

            // Character Collider
            var col = root.AddComponent<CapsuleCollider>();
            col.height = 1.9f;
            col.radius = 0.42f;
            col.center = new Vector3(0f, 0.95f, 0f);

            // NavMeshAgent
            var nav = root.AddComponent<NavMeshAgent>();
            nav.speed = 3.6f;
            nav.angularSpeed = 240f;
            nav.acceleration = 12f;
            nav.stoppingDistance = 1.8f;
            nav.radius = 0.42f;
            nav.height = 1.9f;

            var ai = root.AddComponent<CryptSkeletonAI>();

            // Materials
            Shader urpShader = DungeonMaterialFactory.GetURPLitShader();
            Material boneMat = new Material(urpShader) { name = "Mat_AgedBone" };
            boneMat.color = new Color(0.74f, 0.71f, 0.63f);
            if (boneMat.HasProperty("_Smoothness")) boneMat.SetFloat("_Smoothness", 0.15f);

            Material rustMat = DungeonMaterialFactory.CreateRustyIronMaterial();

            var boneRenderers = new List<Renderer>();

            // 1. Spine & Pelvis
            GameObject spine = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            spine.name = "Bone_Spine";
            spine.transform.SetParent(root.transform, false);
            spine.transform.localPosition = new Vector3(0f, 0.95f, 0f);
            spine.transform.localScale = new Vector3(0.14f, 0.45f, 0.14f);
            spine.GetComponent<Renderer>().sharedMaterial = boneMat;
            boneRenderers.Add(spine.GetComponent<Renderer>());
            RemoveCollider(spine);

            GameObject pelvis = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pelvis.name = "Bone_Pelvis";
            pelvis.transform.SetParent(root.transform, false);
            pelvis.transform.localPosition = new Vector3(0f, 0.65f, 0f);
            pelvis.transform.localScale = new Vector3(0.42f, 0.16f, 0.28f);
            pelvis.GetComponent<Renderer>().sharedMaterial = boneMat;
            boneRenderers.Add(pelvis.GetComponent<Renderer>());
            RemoveCollider(pelvis);

            // 2. Ribcage
            GameObject ribcage = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ribcage.name = "Bone_Ribcage";
            ribcage.transform.SetParent(root.transform, false);
            ribcage.transform.localPosition = new Vector3(0f, 1.25f, 0.02f);
            ribcage.transform.localScale = new Vector3(0.48f, 0.42f, 0.32f);
            ribcage.GetComponent<Renderer>().sharedMaterial = boneMat;
            boneRenderers.Add(ribcage.GetComponent<Renderer>());
            RemoveCollider(ribcage);

            // 3. Skull
            GameObject skull = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            skull.name = "Bone_Skull";
            skull.transform.SetParent(root.transform, false);
            skull.transform.localPosition = new Vector3(0f, 1.62f, 0.04f);
            skull.transform.localScale = new Vector3(0.32f, 0.36f, 0.34f);
            skull.GetComponent<Renderer>().sharedMaterial = boneMat;
            boneRenderers.Add(skull.GetComponent<Renderer>());
            RemoveCollider(skull);

            // Eye sockets light (spectral red eye glow)
            GameObject eyeLightObj = new GameObject("Skeleton_EyeGlow");
            eyeLightObj.transform.SetParent(skull.transform, false);
            eyeLightObj.transform.localPosition = new Vector3(0f, 0f, 0.45f);

            Light eyeLight = eyeLightObj.AddComponent<Light>();
            eyeLight.type = LightType.Point;
            eyeLight.color = new Color(1.0f, 0.15f, 0.1f);
            eyeLight.intensity = 1.0f;
            eyeLight.range = 3.5f;

            // 4. Legs
            CreateBoneLeg(root.transform, new Vector3(0.14f, 0f, 0f), boneMat, boneRenderers);
            CreateBoneLeg(root.transform, new Vector3(-0.14f, 0f, 0f), boneMat, boneRenderers);

            // 5. Weapon Arm & Rusted Blade
            GameObject weaponPivot = new GameObject("Skeleton_WeaponPivot");
            weaponPivot.transform.SetParent(root.transform, false);
            weaponPivot.transform.localPosition = new Vector3(0.35f, 1.25f, 0.2f);

            GameObject armBone = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            armBone.name = "Arm_Bone";
            armBone.transform.SetParent(weaponPivot.transform, false);
            armBone.transform.localPosition = new Vector3(0f, -0.05f, -0.15f);
            armBone.transform.localRotation = Quaternion.Euler(75f, 0f, 0f);
            armBone.transform.localScale = new Vector3(0.08f, 0.2f, 0.08f);
            armBone.GetComponent<Renderer>().sharedMaterial = boneMat;
            boneRenderers.Add(armBone.GetComponent<Renderer>());
            RemoveCollider(armBone);

            GameObject blade = GameObject.CreatePrimitive(PrimitiveType.Cube);
            blade.name = "Rusted_Falchion";
            blade.transform.SetParent(weaponPivot.transform, false);
            blade.transform.localPosition = new Vector3(0f, 0f, 0.55f);
            blade.transform.localScale = new Vector3(0.06f, 0.16f, 0.95f);
            blade.GetComponent<Renderer>().sharedMaterial = rustMat;
            RemoveCollider(blade);

            ai.SetComponents(weaponPivot.transform, boneRenderers.ToArray(), eyeLight);

            return root;
        }

        private static void CreateBoneLeg(Transform parent, Vector3 offset, Material mat, List<Renderer> renderers)
        {
            GameObject upper = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            upper.name = "Bone_Femur";
            upper.transform.SetParent(parent, false);
            upper.transform.localPosition = offset + new Vector3(0f, 0.45f, 0f);
            upper.transform.localScale = new Vector3(0.09f, 0.2f, 0.09f);
            upper.GetComponent<Renderer>().sharedMaterial = mat;
            renderers.Add(upper.GetComponent<Renderer>());
            RemoveCollider(upper);

            GameObject lower = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            lower.name = "Bone_Tibia";
            lower.transform.SetParent(parent, false);
            lower.transform.localPosition = offset + new Vector3(0f, 0.15f, 0f);
            lower.transform.localScale = new Vector3(0.08f, 0.18f, 0.08f);
            lower.GetComponent<Renderer>().sharedMaterial = mat;
            renderers.Add(lower.GetComponent<Renderer>());
            RemoveCollider(lower);
        }

        private static void RemoveCollider(GameObject obj)
        {
            var col = obj.GetComponent<Collider>();
            if (col != null) Object.DestroyImmediate(col);
        }
    }
}
