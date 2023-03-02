using UnityEngine;

namespace ABS
{
	public class ParticleSampler : Sampler
	{
		private readonly ParticleModel particleModel;

		private readonly Vector3 vecFromCameraToModel;

		public ParticleSampler(Model model, Studio studio): base(model, studio)
		{
			particleModel = model as ParticleModel;

			vecFromCameraToModel = model.transform.position - Camera.main.transform.position;
		}

		protected override void ClearAllFrames()
		{
			particleModel.selectedFrames.Clear();
		}

		protected override float GetTimeForRatio(float ratio)
        {
			return particleModel.GetTimeForRatio(ratio);
        }

		protected override void AnimateModel(Frame frame)
        {
			particleModel.Animate(frame);
        }

		protected override void AddFrame(Frame frame)
		{
			particleModel.selectedFrames.Add(frame);
		}

		protected override void OnInitialize_()
		{
			if (studio.shadow.type == ShadowType.TopDown)
            {
				ObjectHelper.DeleteObject(EditorConstants.DYNAMIC_SHADOW_NAME);
				studio.shadow.obj = ObjectHelper.GetOrCreateObject(EditorConstants.STATIC_SHADOW_NAME, EditorConstants.SHADOW_FOLDER_NAME, Vector3.zero);

				ShadowHelper.LocateShadowToModel(particleModel, studio);

				Camera camera;
				GameObject fieldObj;
				ShadowHelper.GetCameraAndFieldObject(studio.shadow.obj, out camera, out fieldObj);

				CameraHelper.LookAtModel(camera.transform, particleModel);
				ShadowHelper.ScaleShadowField(camera, fieldObj);
			}
		}

		protected override void OnCaptureFrame_()
		{
			if (studio.shadow.type == ShadowType.TopDown)
            {
				Camera camera;
				GameObject fieldObj;
				ShadowHelper.GetCameraAndFieldObject(studio.shadow.obj, out camera, out fieldObj);
				ShadowHelper.BakeStaticShadow(camera, fieldObj, particleModel, studio);
			}
		}

		protected override void Finish_()
		{
			if (studio.shadow.type == ShadowType.TopDown)
			{
				ObjectHelper.DeleteObject(EditorConstants.STATIC_SHADOW_NAME);
				studio.shadow.obj = ObjectHelper.GetOrCreateObject(EditorConstants.DYNAMIC_SHADOW_NAME, EditorConstants.SHADOW_FOLDER_NAME, Vector3.zero);

				ShadowHelper.LocateShadowToModel(particleModel, studio);
			}
		}
	}
}
