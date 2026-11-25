using System.Collections.Generic;
using UnityEngine;

namespace ParkMaster.Drawing
{

    public static class PathMathUtility
    {
        public static float SqrDistXZ(Vector3 a, Vector3 b)
        {
            float dx = a.x - b.x;
            float dz = a.z - b.z;
            return dx * dx + dz * dz;
        }

        public static float CalculatePathLengthXZ(IReadOnlyList<Vector3> points)
        {
            if (points == null || points.Count < 2) return 0f;

            float total = 0f;
            for (int i = 1; i < points.Count; i++)
            {
                float dx = points[i].x - points[i - 1].x;
                float dz = points[i].z - points[i - 1].z;
                total += Mathf.Sqrt(dx * dx + dz * dz);
            }
            return total;
        }

        public static Vector3 ClampPointToBoundsXZ(Vector3 point, Bounds bounds, float padding, float yOverride)
        {
            float minX = bounds.min.x + padding;
            float maxX = bounds.max.x - padding;
            float minZ = bounds.min.z + padding;
            float maxZ = bounds.max.z - padding;

            if (minX > maxX) minX = maxX = bounds.center.x;
            if (minZ > maxZ) minZ = maxZ = bounds.center.z;

            return new Vector3(
                Mathf.Clamp(point.x, minX, maxX),
                yOverride,
                Mathf.Clamp(point.z, minZ, maxZ)
            );
        }
    }
}
