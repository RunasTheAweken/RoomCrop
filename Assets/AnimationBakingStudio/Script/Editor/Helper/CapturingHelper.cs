using UnityEngine;

namespace ABS
{
    public class CapturingHelper
    {
        public static Texture2D CaptureModelManagingShadow(Model model, Studio studio)
        {
            Texture2D resultTexture;

            if (studio.shadow.type != ShadowType.None)
            {
                if (studio.shadow.shadowOnly)
                {
                    PickOutSimpleShadow(studio);
                    {
                        Vector3 originalPosition = ThrowOutModelFarAway(model);
                        {
                            resultTexture = CaptureModelInCamera(model, Camera.main, studio);
                        }
                        PutModelBackInPlace(model, originalPosition);
                    }
                    PushInSimpleShadow(model, studio);
                }
                else
                {
                    resultTexture = CaptureModelInCamera(model, Camera.main, studio);
                }
            }
            else
            {
                resultTexture = CaptureModelInCamera(model, Camera.main, studio);
            }

            return resultTexture;
        }

        private static void PickOutSimpleShadow(Studio studio)
        {
            if (studio.shadow.type == ShadowType.Simple)
                studio.shadow.obj.transform.parent = null;
        }

        private static void PushInSimpleShadow(Model model, Studio studio)
        {
            if (studio.shadow.type == ShadowType.Simple)
                studio.shadow.obj.transform.parent = model.transform;
        }

        private static Vector3 ThrowOutModelFarAway(Model model)
        {
            Vector3 originalPosition = model.transform.position;
            model.transform.position = CreateFarAwayPosition();
            return originalPosition;
        }

        private static void PutModelBackInPlace(Model model, Vector3 originalPosition)
        {
            model.transform.position = originalPosition;
        }

        private static Vector3 CreateFarAwayPosition()
        {
            return new Vector3(10000f, 0f, 0f);
        }

        public static Texture2D CaptureModelInCamera(Model model, Camera camera, Studio studio, bool isShadow = false)
        {
            if (camera == null || studio.extraction.com == null)
                return Texture2D.whiteTexture;

            RenderTexture.active = camera.targetTexture;

            Texture2D resultTexure = new Texture2D(camera.targetTexture.width, camera.targetTexture.height, TextureFormat.ARGB32, false);

            if (isShadow)
                ExtractionHelper.ExtractInShadowCamera(camera, ref resultTexure);
            else
                studio.extraction.com.Extract(camera, model, ref resultTexure);

            RenderTexture.active = null;

            return resultTexure;
        }

        private static Material normalMapMaterial = null;
        private static Material NormalMapMaterial
        {
            get
            {
                if (normalMapMaterial == null)
                    normalMapMaterial = AssetHelper.FindAsset<Material>("NormalMap", "NormalMap");
                return normalMapMaterial;
            }
        }

        public static Texture2D CaptureModelForNormalMap(Model model, float rotX, float rotY, GameObject shadowObj)
        {
            if (Camera.main == null)
                return Texture2D.whiteTexture;

            model.BackupAllMaterials();

            NormalMapMaterial.SetFloat("_RotX", rotX);
            NormalMapMaterial.SetFloat("_RotY", rotY);
            model.ChangeAllMaterials(NormalMapMaterial);

            if (shadowObj != null)
                shadowObj.SetActive(false);

            RenderTexture.active = Camera.main.targetTexture;

            Texture2D resultTexture = new Texture2D(Camera.main.targetTexture.width, Camera.main.targetTexture.height, TextureFormat.ARGB32, false);
            ExtractionHelper.ExtractOpqaue(ref resultTexture, EngineConstants.NORMALMAP_COLOR32);

            RenderTexture.active = null;

            if (shadowObj != null)
                shadowObj.SetActive(true);

            model.RestoreAllMaterials();

            return resultTexture;
        }
    }
}
