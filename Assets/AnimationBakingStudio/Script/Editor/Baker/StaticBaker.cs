using System;
using UnityEngine;
using UnityEditor;

namespace ABS
{
	public class StaticBaker : Baker
    {
        private readonly MeshModel meshModel = null;

        private class BakingData
        {
            public string name;
            public IntegerVector pivot;
            public Texture2D modelTexture;
            public Texture2D normalMapTexture;

            public BakingData(string name, IntegerVector pivot, Texture2D modelTexture, Texture2D normalMapTexture = null)
            {
                this.name = name;
                this.pivot = pivot;
                this.modelTexture = modelTexture;
                this.normalMapTexture = normalMapTexture;
            }
        }

        private BakingData[] bakingDataList = null;

        public StaticBaker(Model model, Studio studio, string sIndex, string parentFolderPath)
            : base(model, studio, sIndex, parentFolderPath)
        {
            meshModel = model as MeshModel;

            stateMachine = new StateMachine<BakingState>();
            stateMachine.AddState(BakingState.Initialize, OnInitialize);
            stateMachine.AddState(BakingState.BeginView, OnBeginView);
            stateMachine.AddState(BakingState.CaptureFrame, OnCaptureFrame);
            stateMachine.AddState(BakingState.EndView, OnEndView);
            stateMachine.AddState(BakingState.Finalize, OnFinalize);

            stateMachine.ChangeState(BakingState.Initialize);
        }

        public void OnInitialize()
        {
            try
            {
                if (studio.view.rotationType == RotationType.Model)
                    CameraHelper.LocateMainCameraToModel(model, studio);

                ShadowHelper.LocateShadowToModel(model, studio);

                if (studio.shadow.type == ShadowType.Simple)
                {
                    ShadowHelper.ScaleSimpleShadow(model, studio);
                }
                else if (studio.shadow.type == ShadowType.TopDown)
                {
                    Camera camera;
                    GameObject fieldObj;
                    ShadowHelper.GetCameraAndFieldObject(studio.shadow.obj, out camera, out fieldObj);

                    CameraHelper.LookAtModel(camera.transform, model);
                    ShadowHelper.ScaleShadowField(camera, fieldObj);
                }
                else if (studio.shadow.type == ShadowType.Matte)
                {
                    ShadowHelper.ScaleMatteField(meshModel, studio.shadow.obj, studio.lit);
                }

                if (studio.packing.on || studio.trimming.IsUnified())
                    bakingDataList = new BakingData[studio.view.checkedSubViews.Count];

                if (studio.trimming.IsUnified())
                    unifiedTexBound = new IntegerBound();

                fileBaseName = BuildFileBaseName();
                BuildFolderPathAndCreate(modelName);

                viewIndex = 0;

                stateMachine.ChangeState(BakingState.BeginView);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                Finish();
            }
        }

        public void OnBeginView()
        {
            try
            {
                int viewIndexForProgress = viewIndex + 1;
                float progress = (float)viewIndexForProgress / (float)studio.view.checkedSubViews.Count;

                if (studio.view.checkedSubViews.Count == 0)
                    IsCancelled = EditorUtility.DisplayCancelableProgressBar("Progress...", " (" + ((int)(progress * 100f)) + "%)", progress);
                else
                    IsCancelled = EditorUtility.DisplayCancelableProgressBar("Progress...", "View: " + viewIndexForProgress + " (" + ((int)(progress * 100f)) + "%)", progress);

                if (IsCancelled)
                    throw new Exception("Cancelled");

                studio.view.checkedSubViews[viewIndex].func(model);
                viewName = studio.view.checkedSubViews[viewIndex].name;

                Vector3 screenPos = Camera.main.WorldToScreenPoint(model.GetPivotPosition());
                pivot2D = new IntegerVector(screenPos);

                stateMachine.ChangeState(BakingState.CaptureFrame);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                Finish();
            }
        }

        public void OnCaptureFrame()
        {
            try
            {
                double deltaTime = EditorApplication.timeSinceStartup - prevTime;
                if (deltaTime < studio.frame.delay)
                    return;
                prevTime = EditorApplication.timeSinceStartup;

                Texture2D modelTexture = CapturingHelper.CaptureModelManagingShadow(model, studio);
                Texture2D normalMapTexture = null;
                if (studio.output.normalMapMake)
                {
                    float rotX = studio.view.slopeAngle;
                    float rotY = (studio.view.rotationType == RotationType.Camera ? studio.view.checkedSubViews[viewIndex].angle : 0);
                    normalMapTexture = CapturingHelper.CaptureModelForNormalMap(model, rotX, rotY, studio.shadow.obj);
                }

                IntegerBound texBound = new IntegerBound();
                if (!TextureHelper.CalcTextureBound(modelTexture, pivot2D, texBound))
                {
                    texBound.min.x = pivot2D.x - 1;
                    texBound.max.x = pivot2D.x + 1;
                    texBound.min.y = pivot2D.y - 1;
                    texBound.max.y = pivot2D.y + 1;
                }

                IntegerVector pivotClone = new IntegerVector(pivot2D);

                if (studio.trimming.on)
                {
                    if (studio.trimming.isUnifiedForAllViews)
                    {
                        TextureHelper.MakeUnifiedBound(pivotClone, texBound, unifiedTexBound);
                    }
                    else
                    {
                        pivotClone.SubtractWithMargin(texBound.min, studio.trimming.margin);

                        modelTexture = TextureHelper.TrimTexture(modelTexture, texBound, studio.trimming.margin, EngineConstants.CLEAR_COLOR32);
                        if (studio.output.normalMapMake)
                            normalMapTexture = TextureHelper.TrimTexture(normalMapTexture, texBound, studio.trimming.margin, EngineConstants.NORMALMAP_COLOR32, true);
                    }
                }

                string viewName = studio.view.checkedSubViews[viewIndex].name;
                if (studio.packing.on || studio.trimming.IsUnified())
                {
                    bakingDataList[viewIndex] = new BakingData(viewName, pivotClone, modelTexture, normalMapTexture);
                }
                else // !studio.packing.on && !studio.trim.IsUnified()
                {
                    string materialFullName = BakeIndividuallyReal(ref modelTexture, pivotClone, viewName, "");
                    if (studio.output.normalMapMake)
                        BakeIndividuallyReal(ref normalMapTexture, pivotClone, viewName, "", true);
                    CreateMaterial(materialFullName, modelTexture, normalMapAtlasTexture);
                }

                stateMachine.ChangeState(BakingState.EndView);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                Finish();
            }
        }

        public void OnEndView()
        {
            try
            {
                viewIndex++;

                if (viewIndex < studio.view.checkedSubViews.Count)
                    stateMachine.ChangeState(BakingState.BeginView);
                else
                    stateMachine.ChangeState(BakingState.Finalize);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                Finish();
            }
        }

        public void OnFinalize()
        {
            try
            {
                stateMachine = null;

                if (studio.trimming.IsUnified())
                    TrimToUnifiedSizeAll();

                if (!studio.packing.on && studio.trimming.IsUnified())
                {
                    foreach (BakingData data in bakingDataList)
                    {
                        string materialFullName = BakeIndividuallyReal(ref data.modelTexture, data.pivot, data.name, "");
                        if (studio.output.normalMapMake)
                            BakeIndividuallyReal(ref data.normalMapTexture, data.pivot, data.name, "", true);
                        CreateMaterial(materialFullName, data.modelTexture, data.normalMapTexture);
                    }
                }
                else if (studio.packing.on)
                {
                    IntegerVector[] pivots = new IntegerVector[bakingDataList.Length];
                    string[] spriteNames = new string[bakingDataList.Length];
                    Texture2D[] modelTextures = new Texture2D[bakingDataList.Length];

                    Texture2D[] normalMapTextures = null;
                    if (studio.output.normalMapMake)
                        normalMapTextures = new Texture2D[bakingDataList.Length];

                    for (int i = 0; i < bakingDataList.Length; ++i)
                    {
                        BakingData bakingData = bakingDataList[i];
                        pivots[i] = bakingData.pivot;
                        spriteNames[i] = bakingData.name;
                        modelTextures[i] = bakingData.modelTexture;
                        if (studio.output.normalMapMake)
                            normalMapTextures[i] = bakingData.normalMapTexture;
                    }

                    BakeWithPacking(pivots, "", spriteNames, modelTextures, normalMapTextures);
                }

                if (WillMakePrefab())
                {
                    GameObject obj = PrefabUtility.InstantiatePrefab(model.spritePrefab) as GameObject;
                    if (obj != null)
                    {
                        if (firstSprite != null)
                            model.prefabBuilder.BindFirstSprite(obj, firstSprite);
                        if (firstMaterial != null)
                            model.prefabBuilder.BindFirstMaterial(obj, firstMaterial);

                        SaveAsPrefab(obj, fileBaseName);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            finally
            {
                Finish();
            }
        }

        private void TrimToUnifiedSizeAll()
        {
            try
            {
                for (int i = 0; i < bakingDataList.Length; ++i)
                {
                    BakingData bakingData = bakingDataList[i];

                    bakingData.pivot.SubtractWithMargin(unifiedTexBound.min, studio.trimming.margin);

                    bakingData.modelTexture = TextureHelper.TrimTexture(bakingData.modelTexture,
                        unifiedTexBound, studio.trimming.margin, EngineConstants.CLEAR_COLOR32);
                    if (studio.output.normalMapMake)
                    {
                        bakingData.normalMapTexture = TextureHelper.TrimTexture(bakingData.normalMapTexture,
                            unifiedTexBound, studio.trimming.margin, EngineConstants.NORMALMAP_COLOR32, true);
                    }
                }
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        public override void Finish()
        {
            stateMachine = null;
        }
    }
}
