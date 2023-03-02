using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityStandardAssets.ImageEffects;

namespace ABS
{
    [RequireComponent(typeof(Camera))]
    public class FrustumGizmo : MonoBehaviour
    {
        public bool show = true;

        void OnDrawGizmos()
        {
            if (!show)
                return;

            Vector3[] nearCorners = new Vector3[4]; //Approx'd nearplane corners
            Vector3[] farCorners = new Vector3[4]; //Approx'd farplane corners
            Plane[] planes = GeometryUtility.CalculateFrustumPlanes(Camera.main); //get planes from matrix

            Plane temp = planes[1]; planes[1] = planes[2]; planes[2] = temp; //swap [1] and [2] so the order is better for the loop
            for (int i = 0; i < 4; i++)
            {
                nearCorners[i] = Plane3Intersect(planes[4], planes[i], planes[(i + 1) % 4]); //near corners on the created projection matrix
                farCorners[i] = Plane3Intersect(planes[5], planes[i], planes[(i + 1) % 4]); //far corners on the created projection matrix
            }

            Gizmos.color = Color.white;
            for (int i = 0; i < 4; i++)
            {
                Gizmos.DrawLine(nearCorners[i], nearCorners[(i + 1) % 4]); //near corners on the created projection matrix
                Gizmos.DrawLine(farCorners[i], farCorners[(i + 1) % 4]); //far corners on the created projection matrix
                Gizmos.DrawLine(nearCorners[i], farCorners[i]); //sides of the created projection matrix
            }
        }

        private Vector3 Plane3Intersect(Plane p1, Plane p2, Plane p3)
        { //get the intersection point of 3 planes
            return ((-p1.distance * Vector3.Cross(p2.normal, p3.normal)) +
                    (-p2.distance * Vector3.Cross(p3.normal, p1.normal)) +
                    (-p3.distance * Vector3.Cross(p1.normal, p2.normal))) /
             Vector3.Dot(p1.normal, Vector3.Cross(p2.normal, p3.normal));
        }
    }
}
