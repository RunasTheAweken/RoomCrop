using UnityEngine;
using UnityEditor;

namespace ABS
{
    [CustomEditor(typeof(ParticleModel)), CanEditMultipleObjects]
    public class ParticleModelEditor : ModelEditor
    {
        private ParticleModel model = null;

        void OnEnable()
        {
            model = target as ParticleModel;
        }

        public override void OnInspectorGUI()
        {
            GUI.changed = false;

            if (targets != null && targets.Length > 1)
                OnInspectorGUI_Multi();
            else if (model != null)
                OnInspectorGUI_Single();
        }

        private void OnInspectorGUI_Single()
        {
            Undo.RecordObject(model, "Particle Model");

            bool isAnyChanged = false;

            EditorGUI.BeginChangeCheck();
            model.mainParticleSystem = EditorGUILayout.ObjectField("Main Particle System",
                model.mainParticleSystem, typeof(ParticleSystem), true) as ParticleSystem;
            if (EditorGUI.EndChangeCheck())
                model.targetChecked = false;

            if (model.mainParticleSystem == null)
                model.TrySetMainParticleSystem();

            if (model.mainParticleSystem != null)
            {
                if (!model.targetChecked)
                    model.CheckModel();
            }

            EditorGUILayout.Space();

            model.isGroundPivot = DrawGroundPivotField(model, out isAnyChanged);

            EditorGUILayout.Space();

            model.isLooping = DrawLoopingField(model, out isAnyChanged);
            if (model.isLooping)
            {
                EditorGUI.indentLevel++;
                model.isPrewarm = DrawPrewarmField(model, out isAnyChanged);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();

            model.spritePrefab = DrawSpritePrefabField(model, out isAnyChanged);
            if (model.spritePrefab != null)
            {
                EditorGUI.indentLevel++;
                model.prefabBuilder = DrawPrefabBuilderField(model, out isAnyChanged);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();

            bool isNameSuffixChanged;
            model.nameSuffix = DrawModelNameSuffix(model, out isNameSuffixChanged);
            if (isNameSuffixChanged)
                PathHelper.CorrectPathString(ref model.nameSuffix);

            EditorGUILayout.Space();

            if (DrawingHelper.DrawWideButton("Add to the model list"))
                AddToModelList(model);
        }

        protected void OnInspectorGUI_Multi()
        {
            EditorGUILayout.HelpBox("Displayed information is of the first selected model,\nbut any change affects all selected models.", MessageType.Info);

            ParticleModel[] models = new ParticleModel[targets.Length];

            for (int i = 0; i < models.Length; ++i)
                models[i] = targets[i] as ParticleModel;

            ParticleModel firstModel = models[0];

            EditorGUILayout.Space();

            bool isGroundPivotChanged;
            bool isGroundPivot = DrawGroundPivotField(firstModel, out isGroundPivotChanged);

            EditorGUILayout.Space();

            bool isLoopingChanged;
            bool isLooping = DrawLoopingField(firstModel, out isLoopingChanged);

            bool isAllLooping = true;
            foreach (ParticleModel model in models)
                isAllLooping &= model.isLooping;

            bool isPrewarmChanged = false;
            bool isPrewarm = false;
            if (isAllLooping)
            {
                EditorGUI.indentLevel++;
                isPrewarm = DrawPrewarmField(firstModel, out isPrewarmChanged);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();

            bool isSpritePrefabChanged;
            GameObject spritePrefab = DrawSpritePrefabField(firstModel, out isSpritePrefabChanged);

            bool hasAllSpritePrefab = true;
            foreach (ParticleModel model in models)
                hasAllSpritePrefab &= (model.spritePrefab != null);

            PrefabBuilder prefabBuilder = null;
            bool isPrefabBuilderChanged = false;
            if (hasAllSpritePrefab)
            {
                EditorGUI.indentLevel++;
                prefabBuilder = DrawPrefabBuilderField(model, out isPrefabBuilderChanged);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();

            bool isNameSuffixChanged;
            string nameSuffix = DrawModelNameSuffix(firstModel, out isNameSuffixChanged);
            if (isNameSuffixChanged)
                PathHelper.CorrectPathString(ref nameSuffix);

            if (isGroundPivotChanged || isLoopingChanged || isPrewarmChanged ||
                isSpritePrefabChanged || isPrefabBuilderChanged || isNameSuffixChanged)
            {
                foreach (ParticleModel model in models)
                {
                    Undo.RecordObject(model, "Particle Model");
                    if (isGroundPivotChanged)
                        model.isGroundPivot = isGroundPivot;
                    if (isLoopingChanged)
                        model.isLooping = isLooping;
                    if (isPrewarmChanged)
                        model.isPrewarm = isPrewarm;
                    if (isSpritePrefabChanged)
                        model.spritePrefab = spritePrefab;
                    if (hasAllSpritePrefab && isPrefabBuilderChanged)
                        model.prefabBuilder = prefabBuilder;
                    if (isNameSuffixChanged)
                        model.nameSuffix = nameSuffix;
                }
            }

            Studio studio = FindObjectOfType<Studio>();
            if (studio == null)
                return;

            EditorGUILayout.Space();

            if (DrawingHelper.DrawWideButton("Add all to the model list"))
            {
                foreach (ParticleModel model in models)
                    AddToModelList(model);
            }
        }

        private bool DrawLoopingField(ParticleModel model, out bool isChanged)
        {
            EditorGUI.BeginChangeCheck();
            bool isLooping = EditorGUILayout.Toggle(new GUIContent("Looping", "generates looping animation clip"), model.isLooping);
            isChanged = EditorGUI.EndChangeCheck();

            return isLooping;
        }

        private bool DrawPrewarmField(ParticleModel model, out bool isChanged)
        {
            EditorGUI.BeginChangeCheck();
            bool isPrewarm = EditorGUILayout.Toggle(new GUIContent("Prewarm",
                "simulates for the final frame at first instead of the zero frame"), model.isPrewarm);
            isChanged = EditorGUI.EndChangeCheck();

            return isPrewarm;
        }
    }
}
