using System.Collections.Generic;
using UnityEngine;

namespace Week02
{
    public class RasterizationRenderer : MonoBehaviour
    {
        // scene elements
        public Camera camera;
        public GameObject screen;
        public int screenPixels = 64; // careful, don't make it too large!
        public Light lightSource;
        List<GameObject> allObjects;

        // all objects will have the same color
        public Color objectColor = Color.white;
        // ray misses will have this color
        public Color backgroundColor = Color.black;

        // ambient "light" is used to fake global illumination (discussed later in the course)
        [Range(0.0f, 1.0f)]
        public float ambientIntensity = 0.1f;

        // should we render the debug lines?
        public bool drawDebugLines = true;

        // private objects to handle Unity texture and our buffer of colors (to store our image until we give it to texture)
        Texture2D texture;
        Color32[] colorBuffer;
        float[] depthBuffer;



        // Start is called before the first frame update
        void Start()
        {
            // initialize our private objects
            colorBuffer = new Color32[screenPixels * screenPixels];
            depthBuffer = new float[screenPixels * screenPixels];
            texture = new Texture2D(screenPixels, screenPixels);
            texture.filterMode = FilterMode.Point;

            // set the texture in our screen material
            Material screenMaterial = screen.GetComponent<Renderer>().material;
            screenMaterial.SetTexture("_MainTex", texture);

            // make a list of all objects with a collider and meshfilter
            allObjects = new List<GameObject>();
            Collider[] allColliders = UnityEngine.Object.FindObjectsOfType<Collider>();
            for (int i = 0; i < allColliders.Length; i++)
            {
                MeshFilter meshFilter = allColliders[i].gameObject.GetComponent<MeshFilter>();
                if (meshFilter && allColliders[i].enabled)
                    allObjects.Add(allColliders[i].gameObject);
            }
        }

        // Update is called once per frame
        void Update()
        {
            ClearBuffers();

            Matrix4x4 view = camera.transform.worldToLocalMatrix;
            Matrix4x4 projection = camera.projectionMatrix;

            for (int oi = 0; oi < allObjects.Count; oi++)
            {
                Mesh mesh = allObjects[oi].GetComponent<MeshFilter>().mesh;
                Vector3[] vertices = mesh.vertices;
                Vector3[] normals = mesh.normals;
                int[] indices = mesh.triangles;
                Matrix4x4 model = allObjects[oi].transform.localToWorldMatrix;
                Matrix4x4 MVP = projection * view * model;
                Matrix4x4 modelNormal = (model).transpose.inverse;
                
                for (int vi = 0; vi < vertices.Length; vi++)
                {
                    Vector3 vertexIn = vertices[vi];
                    Vector3 normalIn = normals[vi];

                    // vertex "shader"
                    Vector4 vertexOut = MVP * new Vector4(vertexIn.x, vertexIn.y, vertexIn.z, 1f);
                    
                    // perspective division
                    vertexOut.x /= -vertexOut.w;
                    vertexOut.y /= -vertexOut.w;
                    vertexOut.z /= -vertexOut.w;

                    // to screen coordinates
                    vertexOut.x = (vertexOut.x + 1f) * .5f * (float)screenPixels;
                    vertexOut.y = (vertexOut.y + 1f) * .5f * (float)screenPixels;
                    vertexOut.z = (vertexOut.z + 1f) * .5f * (float)screenPixels; // not necessary, here only for visualization purpuses

                    vertices[vi] = vertexOut; // implicit conversion to Vector3

                    Vector3 normalOut = modelNormal * normalIn;
                    normals[vi] = normalOut;
                }
                //mesh.vertices = vertices;

                for (int vi = 0; vi < vertices.Length; vi++)
                {
                    RasterizePoints(vertices[vi], normals[vi]);
                }

                for (int ti = 0; ti < indices.Length; ti += 3)
                {
                    var v1 = vertices[indices[ti]];
                    var v2 = vertices[indices[ti + 1]];
                    var v3 = vertices[indices[ti + 2]];
                    var n = (normals[indices[ti]] + normals[indices[ti + 1]] + normals[indices[ti + 2]]) / 3f;

                    Debug.DrawLine(v1, v2);
                    Debug.DrawLine(v2, v3);
                    Debug.DrawLine(v3, v1);

                    RasterizeTriangle(v1, v2, v3, n);
                }

            }

            texture.SetPixels32(colorBuffer);
            texture.Apply();
        }

        void RasterizePoints(Vector3 position, Vector3 normal)
        {
            var vtx = Vector2Int.RoundToInt(position); ;
            if (vtx.x >= 0 && vtx.x < screenPixels && vtx.y >= 0 && vtx.y < screenPixels)
            {
                // z/depth buffer testing
                int idx = vtx.y * screenPixels + vtx.x;
                if (depthBuffer[idx] > position.z)
                {
                    depthBuffer[idx] = position.z;
                    colorBuffer[idx] = ComputeLighting(normal);
                }
            }
        }


        void RasterizeTriangle(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 normal)
        {
            // simply using the avarage z is a huge simplification of a triangle distance
            // and is will cause visual artifacts
            float z = (v1.z + v2.z + v3.z) / 3f;
            // delimit the range of pixels to check during rasterization
            Vector2 min = Vector2.Min(v1, Vector2.Min(v2, v3));
            Vector2 max = Vector2.Max(v1, Vector2.Max(v2, v3));
            int xmin = Mathf.Max(0, (int)(min.x -1f)); 
            int xmax = Mathf.Min(screenPixels, (int)(max.x + 1f));
            int ymin = Mathf.Max(0, (int)(min.y -1f)); 
            int ymax = Mathf.Min(screenPixels, (int)(max.y + 1f));

            // iterate all pixels in the square where this triangle may overlap
            for (int x = xmin; x < xmax; x++)
            {
                for (int y = ymin; y < ymax; y++)
                {
                    Vector2 pixelPos = new Vector2(x, y);

                    // check point/triangle intersection
                    float d1 = Sign(pixelPos, v1, v2);
                    float d2 = Sign(pixelPos, v2, v3);
                    float d3 = Sign(pixelPos, v3, v1);

                    bool has_neg = (d1 < 0) || (d2 < 0) || (d3 < 0);
                    bool has_pos = (d1 > 0) || (d2 > 0) || (d3 > 0);
                    bool intersect = !(has_neg && has_pos);

                    if (!intersect) continue;

                    // z/depth buffer testing
                    int idx = y * screenPixels + x;
                    if (depthBuffer[idx] > z)
                    {
                        depthBuffer[idx] = z;
                        colorBuffer[idx] = (Color32)ComputeLighting(normal);
                    }
                }
            }
        }

        float Sign(Vector2 v1, Vector2 v2, Vector2 v3)
        {
            // check if v1 is on the left or right of the vector from v3 to v2
            return (v1.x - v3.x) * (v2.y - v3.y) - (v2.x - v3.x) * (v1.y - v3.y);
        }

        Color ComputeLighting(Vector3 normal)
        {
            // light computation with diffuse and ambient contributions
            float diffuseIntensity = Mathf.Max(0, Vector3.Dot(normal, -lightSource.transform.forward)) * (1f - ambientIntensity);
            Color color = (diffuseIntensity * objectColor * lightSource.color) + (objectColor * ambientIntensity);

            return color;
        }

        void ClearBuffers()
        {
            for (int i = 0; i < colorBuffer.Length; i++)
            {
                colorBuffer[i] = backgroundColor;
                depthBuffer[i] = float.MaxValue;
            }
        }

    }
}
