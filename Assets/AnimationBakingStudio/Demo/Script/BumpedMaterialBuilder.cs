using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ABS.Demo
{
    public class BumpedMaterialBuilder : MaterialBuilder
    {
        public override void BindTextures(Material mat, Texture2D modelTex, Texture2D normalMapTex = null)
        {
            mat.SetTexture("_MainTex", modelTex);
            mat.SetTexture("_BumpMap", normalMapTex);
        }
    }
}
