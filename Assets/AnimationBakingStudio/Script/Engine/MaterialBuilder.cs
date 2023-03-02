using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ABS
{
    public abstract class MaterialBuilder : MonoBehaviour
    {
        public abstract void BindTextures(Material mat, Texture2D modelTex, Texture2D normalMapTex = null);
    }
}
