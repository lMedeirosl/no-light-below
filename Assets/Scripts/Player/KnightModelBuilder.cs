using UnityEngine;
using NoLightBelow.Combat;
using NoLightBelow.Core;

namespace NoLightBelow.Player
{
    public static class KnightModelBuilder
    {
        public static (Transform swordPivot, Hitbox hitbox, Transform shieldPivot) BuildKnight(GameObject playerRoot)
        {
            // Remove any old placeholder meshes if present
            for (int i = playerRoot.transform.childCount - 1; i >= 0; i--)
            {
                Transform child = playerRoot.transform.GetChild(i);
                if (child.name.StartsWith("Body") || child.name.StartsWith("Head") || 
                    child.name.StartsWith("Weapon") || child.name.StartsWith("Shield") ||
                    child.name.StartsWith("Knight_Rig"))
                {
                    Object.DestroyImmediate(child.gameObject);
                }
            }

            // Create Materials
            Shader urpShader = Shader.Find("Universal Render Pipeline/Lit");
            if (urpShader == null) urpShader = Shader.Find("Standard");

            Material steelMat = new Material(urpShader) { name = "Mat_KnightSteel" };
            steelMat.color = new Color(0.32f, 0.34f, 0.38f);
            if (steelMat.HasProperty("_Metallic")) steelMat.SetFloat("_Metallic", 0.85f);
            if (steelMat.HasProperty("_Smoothness")) steelMat.SetFloat("_Smoothness", 0.55f);

            Material darkSteelMat = new Material(urpShader) { name = "Mat_KnightDarkSteel" };
            darkSteelMat.color = new Color(0.18f, 0.19f, 0.22f);
            if (darkSteelMat.HasProperty("_Metallic")) darkSteelMat.SetFloat("_Metallic", 0.9f);
            if (darkSteelMat.HasProperty("_Smoothness")) darkSteelMat.SetFloat("_Smoothness", 0.45f);

            Material chainmailMat = new Material(urpShader) { name = "Mat_KnightChainmail" };
            chainmailMat.color = new Color(0.12f, 0.13f, 0.14f);
            if (chainmailMat.HasProperty("_Metallic")) chainmailMat.SetFloat("_Metallic", 0.4f);
            if (chainmailMat.HasProperty("_Smoothness")) chainmailMat.SetFloat("_Smoothness", 0.15f);

            Material leatherMat = new Material(urpShader) { name = "Mat_KnightLeather" };
            leatherMat.color = new Color(0.24f, 0.15f, 0.09f);
            if (leatherMat.HasProperty("_Smoothness")) leatherMat.SetFloat("_Smoothness", 0.2f);

            Material goldMat = new Material(urpShader) { name = "Mat_KnightGoldTrim" };
            goldMat.color = new Color(0.78f, 0.62f, 0.22f);
            if (goldMat.HasProperty("_Metallic")) goldMat.SetFloat("_Metallic", 0.85f);
            if (goldMat.HasProperty("_Smoothness")) goldMat.SetFloat("_Smoothness", 0.65f);

            Material visorMat = new Material(urpShader) { name = "Mat_KnightVisor" };
            visorMat.color = new Color(0.02f, 0.02f, 0.02f);
            if (visorMat.HasProperty("_Smoothness")) visorMat.SetFloat("_Smoothness", 0.9f);

            // Rig Root
            GameObject rigRoot = new GameObject("Knight_Rig");
            rigRoot.transform.SetParent(playerRoot.transform, false);

            // ================= 1. TORSO & BREASTPLATE =================
            GameObject chest = GameObject.CreatePrimitive(PrimitiveType.Cube);
            chest.name = "Armor_Cuirass";
            chest.transform.SetParent(rigRoot.transform, false);
            chest.transform.localPosition = new Vector3(0f, 1.15f, 0f);
            chest.transform.localScale = new Vector3(0.55f, 0.50f, 0.38f);
            chest.GetComponent<Renderer>().sharedMaterial = steelMat;
            RemoveCollider(chest);

            // Torso Ridge / Plackart
            GameObject ridge = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ridge.name = "Armor_CenterRidge";
            ridge.transform.SetParent(chest.transform, false);
            ridge.transform.localPosition = new Vector3(0f, 0.05f, 0.5f);
            ridge.transform.localRotation = Quaternion.Euler(0f, 45f, 0f);
            ridge.transform.localScale = new Vector3(0.35f, 0.95f, 0.35f);
            ridge.GetComponent<Renderer>().sharedMaterial = steelMat;
            RemoveCollider(ridge);

            // Gorget (Neck guard)
            GameObject gorget = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            gorget.name = "Armor_Gorget";
            gorget.transform.SetParent(rigRoot.transform, false);
            gorget.transform.localPosition = new Vector3(0f, 1.42f, 0f);
            gorget.transform.localScale = new Vector3(0.32f, 0.08f, 0.32f);
            gorget.GetComponent<Renderer>().sharedMaterial = darkSteelMat;
            RemoveCollider(gorget);

            // Waist / Belt
            GameObject belt = GameObject.CreatePrimitive(PrimitiveType.Cube);
            belt.name = "Armor_Belt";
            belt.transform.SetParent(rigRoot.transform, false);
            belt.transform.localPosition = new Vector3(0f, 0.88f, 0f);
            belt.transform.localScale = new Vector3(0.48f, 0.12f, 0.34f);
            belt.GetComponent<Renderer>().sharedMaterial = leatherMat;
            RemoveCollider(belt);

            // Belt Buckle
            GameObject buckle = GameObject.CreatePrimitive(PrimitiveType.Cube);
            buckle.name = "Belt_Buckle";
            buckle.transform.SetParent(belt.transform, false);
            buckle.transform.localPosition = new Vector3(0f, 0f, 0.55f);
            buckle.transform.localScale = new Vector3(0.25f, 0.8f, 0.15f);
            buckle.GetComponent<Renderer>().sharedMaterial = goldMat;
            RemoveCollider(buckle);

            // ================= 2. GREATHELM (HELMET) =================
            GameObject helm = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            helm.name = "Knight_Greathelm";
            helm.transform.SetParent(rigRoot.transform, false);
            helm.transform.localPosition = new Vector3(0f, 1.62f, 0.02f);
            helm.transform.localScale = new Vector3(0.34f, 0.22f, 0.36f);
            helm.GetComponent<Renderer>().sharedMaterial = steelMat;
            RemoveCollider(helm);

            // Helm Dome Top
            GameObject helmCap = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            helmCap.name = "Helm_Cap";
            helmCap.transform.SetParent(helm.transform, false);
            helmCap.transform.localPosition = new Vector3(0f, 0.8f, 0f);
            helmCap.transform.localScale = new Vector3(0.96f, 0.45f, 0.96f);
            helmCap.GetComponent<Renderer>().sharedMaterial = steelMat;
            RemoveCollider(helmCap);

            // Eye Visor Slit (T-Slit / Horizontal dark slit)
            GameObject visor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visor.name = "Helm_VisorSlit";
            visor.transform.SetParent(helm.transform, false);
            visor.transform.localPosition = new Vector3(0f, 0.15f, 0.48f);
            visor.transform.localScale = new Vector3(0.72f, 0.12f, 0.12f);
            visor.GetComponent<Renderer>().sharedMaterial = visorMat;
            RemoveCollider(visor);

            // Vertical reinforcement cross on helmet
            GameObject helmCross = GameObject.CreatePrimitive(PrimitiveType.Cube);
            helmCross.name = "Helm_BrassCross";
            helmCross.transform.SetParent(helm.transform, false);
            helmCross.transform.localPosition = new Vector3(0f, 0.15f, 0.49f);
            helmCross.transform.localScale = new Vector3(0.08f, 0.9f, 0.08f);
            helmCross.GetComponent<Renderer>().sharedMaterial = goldMat;
            RemoveCollider(helmCross);

            // ================= 3. PAULDRONS (SHOULDERS) =================
            CreatePuldron(rigRoot.transform, new Vector3(0.36f, 1.34f, 0f), Quaternion.Euler(0f, 0f, -22f), steelMat, goldMat, "Right");
            CreatePuldron(rigRoot.transform, new Vector3(-0.36f, 1.34f, 0f), Quaternion.Euler(0f, 0f, 22f), steelMat, goldMat, "Left");

            // ================= 4. LEGS & ARMORED BOOTS (SABATONS) =================
            CreateLeg(rigRoot.transform, new Vector3(0.15f, 0f, 0f), chainmailMat, steelMat, "Right");
            CreateLeg(rigRoot.transform, new Vector3(-0.15f, 0f, 0f), chainmailMat, steelMat, "Left");

            // Tassets (hip armor plates)
            CreateTasset(rigRoot.transform, new Vector3(0.24f, 0.80f, 0f), Quaternion.Euler(10f, 0f, -15f), steelMat);
            CreateTasset(rigRoot.transform, new Vector3(-0.24f, 0.80f, 0f), Quaternion.Euler(10f, 0f, 15f), steelMat);

            // ================= 5. RIGHT HAND: WEAPON PIVOT & SWORD =================
            GameObject swordPivot = new GameObject("Weapon_Pivot");
            swordPivot.transform.SetParent(playerRoot.transform, false);
            swordPivot.transform.localPosition = new Vector3(0.42f, 1.05f, 0.35f);

            // Arm connection
            GameObject rightArm = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            rightArm.name = "Right_Arm_Gauntlet";
            rightArm.transform.SetParent(swordPivot.transform, false);
            rightArm.transform.localPosition = new Vector3(0f, -0.05f, -0.2f);
            rightArm.transform.localRotation = Quaternion.Euler(75f, 0f, 0f);
            rightArm.transform.localScale = new Vector3(0.13f, 0.22f, 0.13f);
            rightArm.GetComponent<Renderer>().sharedMaterial = steelMat;
            RemoveCollider(rightArm);

            // Sword Grip
            GameObject grip = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            grip.name = "Sword_Grip";
            grip.transform.SetParent(swordPivot.transform, false);
            grip.transform.localPosition = new Vector3(0f, 0f, 0.05f);
            grip.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            grip.transform.localScale = new Vector3(0.055f, 0.14f, 0.055f);
            grip.GetComponent<Renderer>().sharedMaterial = leatherMat;
            RemoveCollider(grip);

            // Pommel
            GameObject pommel = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            pommel.name = "Sword_Pommel";
            pommel.transform.SetParent(swordPivot.transform, false);
            pommel.transform.localPosition = new Vector3(0f, 0f, -0.1f);
            pommel.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
            pommel.GetComponent<Renderer>().sharedMaterial = goldMat;
            RemoveCollider(pommel);

            // Crossguard
            GameObject guard = GameObject.CreatePrimitive(PrimitiveType.Cube);
            guard.name = "Sword_Crossguard";
            guard.transform.SetParent(swordPivot.transform, false);
            guard.transform.localPosition = new Vector3(0f, 0f, 0.20f);
            guard.transform.localScale = new Vector3(0.38f, 0.065f, 0.065f);
            guard.GetComponent<Renderer>().sharedMaterial = darkSteelMat;
            RemoveCollider(guard);

            // Steel Blade
            GameObject blade = GameObject.CreatePrimitive(PrimitiveType.Cube);
            blade.name = "Sword_Blade";
            blade.transform.SetParent(swordPivot.transform, false);
            blade.transform.localPosition = new Vector3(0f, 0f, 0.80f);
            blade.transform.localScale = new Vector3(0.08f, 0.025f, 1.15f);
            blade.GetComponent<Renderer>().sharedMaterial = steelMat;

            var bladeCol = blade.GetComponent<BoxCollider>();
            bladeCol.isTrigger = true;
            bladeCol.size = new Vector3(1.4f, 1.4f, 1.4f); // generous swing hit volume

            var hitbox = blade.AddComponent<Hitbox>();

            // ================= 6. LEFT HAND: SHIELD PIVOT & HEATER SHIELD =================
            GameObject shieldPivot = new GameObject("Shield_Pivot");
            shieldPivot.transform.SetParent(playerRoot.transform, false);
            shieldPivot.transform.localPosition = new Vector3(-0.45f, 0.95f, 0.1f);
            shieldPivot.transform.localRotation = Quaternion.Euler(0f, 15f, 15f);

            // Left Forearm / Gauntlet holding shield
            GameObject leftArm = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            leftArm.name = "Left_Arm_Gauntlet";
            leftArm.transform.SetParent(shieldPivot.transform, false);
            leftArm.transform.localPosition = new Vector3(0.05f, 0f, -0.15f);
            leftArm.transform.localRotation = Quaternion.Euler(70f, 0f, 0f);
            leftArm.transform.localScale = new Vector3(0.13f, 0.22f, 0.13f);
            leftArm.GetComponent<Renderer>().sharedMaterial = steelMat;
            RemoveCollider(leftArm);

            // Heater Shield Body (Main plate)
            GameObject shieldBody = GameObject.CreatePrimitive(PrimitiveType.Cube);
            shieldBody.name = "Shield_Plate";
            shieldBody.transform.SetParent(shieldPivot.transform, false);
            shieldBody.transform.localPosition = new Vector3(0f, 0.05f, 0.15f);
            shieldBody.transform.localScale = new Vector3(0.55f, 0.72f, 0.06f);
            shieldBody.GetComponent<Renderer>().sharedMaterial = darkSteelMat;
            RemoveCollider(shieldBody);

            // Shield Pointed Bottom Tip
            GameObject shieldTip = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            shieldTip.name = "Shield_BottomTip";
            shieldTip.transform.SetParent(shieldBody.transform, false);
            shieldTip.transform.localPosition = new Vector3(0f, -0.45f, 0f);
            shieldTip.transform.localRotation = Quaternion.Euler(0f, 45f, 0f);
            shieldTip.transform.localScale = new Vector3(0.7f, 0.5f, 0.7f);
            shieldTip.GetComponent<Renderer>().sharedMaterial = darkSteelMat;
            RemoveCollider(shieldTip);

            // Shield Steel Boss / Umbo (Center dome)
            GameObject boss = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            boss.name = "Shield_UmboBoss";
            boss.transform.SetParent(shieldBody.transform, false);
            boss.transform.localPosition = new Vector3(0f, 0.05f, 0.55f);
            boss.transform.localScale = new Vector3(0.35f, 0.35f, 0.45f);
            boss.GetComponent<Renderer>().sharedMaterial = goldMat;
            RemoveCollider(boss);

            // Shield Border Rim
            GameObject rimTop = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rimTop.name = "Shield_RimTop";
            rimTop.transform.SetParent(shieldBody.transform, false);
            rimTop.transform.localPosition = new Vector3(0f, 0.5f, 0.1f);
            rimTop.transform.localScale = new Vector3(1.05f, 0.08f, 1.2f);
            rimTop.GetComponent<Renderer>().sharedMaterial = goldMat;
            RemoveCollider(rimTop);

            return (swordPivot.transform, hitbox, shieldPivot.transform);
        }

        private static void CreatePuldron(Transform parent, Vector3 pos, Quaternion rot, Material steel, Material gold, string side)
        {
            GameObject pauldron = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            pauldron.name = $"Armor_Pauldron_{side}";
            pauldron.transform.SetParent(parent, false);
            pauldron.transform.localPosition = pos;
            pauldron.transform.localRotation = rot;
            pauldron.transform.localScale = new Vector3(0.28f, 0.22f, 0.32f);
            pauldron.GetComponent<Renderer>().sharedMaterial = steel;
            RemoveCollider(pauldron);
        }

        private static void CreateLeg(Transform parent, Vector3 xOffset, Material chainmail, Material steel, string side)
        {
            // Upper leg / Cuisse
            GameObject upperLeg = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            upperLeg.name = $"Leg_Upper_{side}";
            upperLeg.transform.SetParent(parent, false);
            upperLeg.transform.localPosition = xOffset + new Vector3(0f, 0.65f, 0f);
            upperLeg.transform.localScale = new Vector3(0.18f, 0.22f, 0.18f);
            upperLeg.GetComponent<Renderer>().sharedMaterial = chainmail;
            RemoveCollider(upperLeg);

            // Knee Poleyn
            GameObject knee = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            knee.name = $"Leg_Knee_{side}";
            knee.transform.SetParent(upperLeg.transform, false);
            knee.transform.localPosition = new Vector3(0f, -0.95f, 0.35f);
            knee.transform.localScale = new Vector3(1.1f, 0.55f, 1.1f);
            knee.GetComponent<Renderer>().sharedMaterial = steel;
            RemoveCollider(knee);

            // Lower Leg / Greave
            GameObject lowerLeg = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            lowerLeg.name = $"Leg_Greave_{side}";
            lowerLeg.transform.SetParent(parent, false);
            lowerLeg.transform.localPosition = xOffset + new Vector3(0f, 0.25f, 0f);
            lowerLeg.transform.localScale = new Vector3(0.16f, 0.24f, 0.16f);
            lowerLeg.GetComponent<Renderer>().sharedMaterial = steel;
            RemoveCollider(lowerLeg);

            // Sabaton (Armored foot/boot)
            GameObject foot = GameObject.CreatePrimitive(PrimitiveType.Cube);
            foot.name = $"Leg_Sabaton_{side}";
            foot.transform.SetParent(parent, false);
            foot.transform.localPosition = xOffset + new Vector3(0f, 0.05f, 0.08f);
            foot.transform.localScale = new Vector3(0.17f, 0.10f, 0.32f);
            foot.GetComponent<Renderer>().sharedMaterial = steel;
            RemoveCollider(foot);
        }

        private static void CreateTasset(Transform parent, Vector3 pos, Quaternion rot, Material steel)
        {
            GameObject tasset = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tasset.name = "Armor_Tasset";
            tasset.transform.SetParent(parent, false);
            tasset.transform.localPosition = pos;
            tasset.transform.localRotation = rot;
            tasset.transform.localScale = new Vector3(0.18f, 0.24f, 0.05f);
            tasset.GetComponent<Renderer>().sharedMaterial = steel;
            RemoveCollider(tasset);
        }

        private static void RemoveCollider(GameObject obj)
        {
            var col = obj.GetComponent<Collider>();
            if (col != null) Object.DestroyImmediate(col);
        }
    }
}
