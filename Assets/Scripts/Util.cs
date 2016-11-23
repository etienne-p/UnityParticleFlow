using UnityEngine;

namespace ParticleFlow
{
    public class Util
    {
        public static bool IsPowerOfTwo(ulong x)
        {
            return (x & (x - 1)) == 0;
        }

        public static Vector3 ComputePerlin(Vector3 position, float scale)
        {
            var nx = Mathf.PerlinNoise(position.y * scale, position.z * scale);
            var ny = Mathf.PerlinNoise(position.x * scale, position.z * scale);
            var nz = Mathf.PerlinNoise(position.x * scale, position.y * scale);
            return new Vector3(nx, ny, nz);
        }

        public static float RayPointDistance(Vector3 rayOrigin, Vector3 rayDir, Vector3 point)
        {
            float distance = Vector3.Distance(rayOrigin, point);
            float angle = Vector3.Angle(rayDir, point - rayOrigin);
            return (distance * Mathf.Sin(angle * Mathf.Deg2Rad));
        }

        public static float RayPointDistance(Ray ray, Vector3 point)
        {
            return RayPointDistance(ray.origin, ray.direction, point);
        }

        public static void CopyTexture2DToRenderTexture(Texture2D texture2D, RenderTexture renderTexture)
        {
            RenderTexture.active = renderTexture;                      
            Graphics.Blit(texture2D, renderTexture); 
            RenderTexture.active = null;
        }

        public static Texture2D CreatePerlinTexture(float scale, int size, Vector2 offset)
        {
            var nPix = size * size;
            var pixels = new Color[nPix];
            var nzScale = scale / (float)size;

            for (var y = 0; y < size; ++y)
            {
                for (var x = 0; x < size; ++x)
                {
                    var pnz = Mathf.PerlinNoise(offset.x + x * nzScale, offset.y + y * nzScale);
                    pixels[x + size * y].r = pnz;
                    pixels[x + size * y].g = pnz; 
                    pixels[x + size * y].b = pnz;
                }
            }

            var perlinTexture = new Texture2D(size, size, TextureFormat.RGB24, false);
            perlinTexture.SetPixels(pixels);
            perlinTexture.Apply();
            return perlinTexture;
        }

        public static Vector3 Hermite(
            Vector3 value1,
            Vector3 tangent1,
            Vector3 value2,
            Vector3 tangent2,
            float ratio)
        {
            var squared = ratio * ratio;
            var cubed = ratio * squared;
            var part1 = ((2.0f * cubed) - (3.0f * squared)) + 1.0f;
            var part2 = (-2.0f * cubed) + (3.0f * squared);
            var part3 = (cubed - (2.0f * squared)) + ratio;
            var part4 = cubed - squared;

            return new Vector3(
                (((value1.x * part1) + (value2.x * part2)) + (tangent1.x * part3)) + (tangent2.x * part4),
                (((value1.y * part1) + (value2.y * part2)) + (tangent1.y * part3)) + (tangent2.y * part4),
                (((value1.z * part1) + (value2.z * part2)) + (tangent1.z * part3)) + (tangent2.z * part4));
        }

        public static Vector3 ClosestInterpolatedPoint(
            Vector3 value1,
            Vector3 tangent1,
            Vector3 value2,
            Vector3 tangent2,
            Vector3 point,
            out float ratio, 
            int steps = 50)
        {
            float minDistance = float.MaxValue;
            Vector3 position = Vector3.zero;
            ratio = .0f;
            for (int i = 0; i < steps; ++i)
            {
                float iratio = (float)i / (float)(steps - 1);
                Vector3 interpolatedPosition = Hermite(value1, tangent1, value2, tangent2, iratio);
                float distance = (point - interpolatedPosition).sqrMagnitude;
                if (distance < minDistance)
                {
                    minDistance = distance;
                    ratio = iratio;
                    position = interpolatedPosition;
                }
            }
            return position;
        }

        public static Vector3 Attractor(Vector3 position, Vector3 center, float radius, float power, float strength)
        {
            var diff = position - center;
            var diffLength = diff.magnitude;
            if (diffLength < radius)
            {
                return diff.normalized * strength * (Mathf.Pow(diffLength / radius, power) / (diffLength / radius) - 1.0f);
            }
            else
            {
                return Vector3.zero;
            }
        }

        public static Vector3 Beam(Vector3 position, Vector3 center, Vector3 direction, float radius, float power, float strength)
        {
            int placeHolder = 0;
            var dist = (position - center).magnitude;
            if (dist < radius)
            {
                var mul = strength *
                          (Mathf.Pow(dist / radius, power) / (dist / radius) - 1.0f);
                return direction * mul; // ray.direction is normalized
            }
            else
            {
                return Vector3.zero;
            }
        }

        public static Vector3 Twirl(Vector3 position, Vector3 center, Vector3 forward, float radius, float power, float strength, float angle)
        {
            var dist = (position - center).magnitude;

            if (dist < radius)
            {
                var ray = new Ray(center, forward);
                var mul = strength * (Mathf.Pow(dist / radius, power) / (dist / radius) - 1.0f);
                var rot = Quaternion.AngleAxis(angle, ray.direction);

                // project position on ray
                var proj = Vector3.Project(position - ray.origin, ray.direction);
                var projPosition = ray.origin + proj;
                var rotatedPosition = rot * (position - projPosition);
                return (rotatedPosition - (position - projPosition)) * mul;
            }
            else
            {
                return Vector3.zero;
            }
        }
    }
}
