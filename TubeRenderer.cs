using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/*
public static class QuaternionExtensions
{
    public static Quaternion Pow(this Quaternion input, float power)
    {
        float inputMagnitude = input.Magnitude();
        Vector3 nHat = new Vector3(input.x, input.y, input.z).normalized;
        Quaternion vectorBit = new Quaternion(nHat.x, nHat.y, nHat.z, 0)
            .ScalarMultiply(power * Mathf.Acos(input.w / inputMagnitude))
                .Exp();
        return vectorBit.ScalarMultiply(Mathf.Pow(inputMagnitude, power));
    }

    public static Quaternion Exp(this Quaternion input)
    {
        float inputA = input.w;
        Vector3 inputV = new Vector3(input.x, input.y, input.z);
        float outputA = Mathf.Exp(inputA) * Mathf.Cos(inputV.magnitude); 
        Vector3 outputV = Mathf.Exp(inputA) * (inputV.normalized * Mathf.Sin(inputV.magnitude));
        return new Quaternion(outputV.x, outputV.y, outputV.z, outputA);
    }

    public static float Magnitude(this Quaternion input)
    {
        return Mathf.Sqrt(input.x * input.x + input.y * input.y + input.z * input.z + input.w * input.w);
    }

    public static Quaternion ScalarMultiply(this Quaternion input, float scalar)
    {
        return new Quaternion(input.x * scalar, input.y * scalar, input.z * scalar, input.w * scalar);
    }
}
*/


  //  [Serializable]
    public class TubeVertex
    {
        public Vector3 point = Vector3.zero;
        public float radius = 0.5f;
        public Color color = Color.white;

        public TubeVertex(Vector3 pt, float r, Color c)
        {
            point = pt;
            radius = r;
            color = c;
        }
    }

[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshFilter))]
public class TubeRenderer : MonoBehaviour
{
    public bool Debug_Draw_End_Caps = false;
    public bool Debug_Draw_Normals = false;
    public bool Debug_Draw_Segment_Directions = false;
    public bool Debug_Draw_All_Segments = false;

    public const int max_verts = 65532; // max number of *actual* verts in the mesh.  Largest array size will be tube_verts * crossSegments * 6.
    public int crossSegments = 4;

    //public ArrayList vertices;
    public Material defaultMaterial;
    public Material xrayMaterial;

    public LinkedList<TubeVertex> tube_vertices;

    private Vector3[] crossPoints;
    private int lastCrossSegments;
    public float flatAtDistance = -1;

    //private Vector3 lastCameraPosition1;
    //private Vector3 lastCameraPosition2;
    public int movePixelsForRebuild = 6;
    public float maxRebuildTime = 0.1f;
    //private float lastRebuildTime = 0.00f;
    private bool _truncated = false;

    private bool rebuild = false;
    public Mesh mesh;
    public MeshRenderer mr;

    private Vector3[] _vertices = null;
    private Vector3[] _normals = null;
    private bool _verts_cached = false;

    private int[] tris;
    private int[] degenerate_tris;


    public static uint num_tube_renderers = 0;
    public TubeRenderer()
    {
        num_tube_renderers++;
    }

    ~TubeRenderer()
    {
        num_tube_renderers--;
    }


    public void XRayMode(bool _value)
    {
        if (_value)
            mr.material = xrayMaterial;
        else
            mr.material = defaultMaterial;
    }


    public void Reset()
    {
        tube_vertices.Clear();

        rebuild = false;
        _truncated = false;
    }


    void Awake()
    {
        tube_vertices = new LinkedList<TubeVertex>();
        mesh = gameObject.GetComponent<MeshFilter>().mesh;
        //_mr = gameObject.GetComponent<MeshRenderer>();
        mr.material = defaultMaterial;
    }


    void Start()
    {
    }


    private void DrawEndcap(int segment_index, Color color)
    {
        if (mesh && mesh.vertices.Length > 0)
        {
            // draw line from the center point to the first vert (so we can see the rotation of the end cap)
            TubeVertex tv = null;
            if (segment_index == 0)
            {
                tv = tube_vertices.First.Value;
            }
            else if (segment_index == tube_vertices.Count - 1)
            {
                tv = tube_vertices.Last.Value;
            }
            else
            {
                // linear search (yuck)
                int i = 0;
                foreach(TubeVertex tvc in tube_vertices)
                {
                    if(i == segment_index)
                    {
                        tv = tvc;
                        break;
                    }
                    i++;
                }
            }
            Debug.DrawLine(this.gameObject.transform.TransformPoint(tv.point), this.gameObject.transform.TransformPoint(mesh.vertices[segment_index * crossSegments]), color);

            // now draw the edges around the end cap
            /*
            for (int c = 0; c < crossSegments; c++)
            {
                int start_index = segment_index * crossSegments + c;
                int end_index = segment_index * crossSegments + ((c + 1) % crossSegments);

                if(start_index > _mesh.vertices.Length || end_index > _mesh.vertices.Length)
                {
                    Debug.LogError("Couldn't draw segment because index is outside the range of _mesh.vertices.Length");
                    break;
                }

                Vector3 start = this.gameObject.transform.TransformPoint(_mesh.vertices[start_index]);
                Vector3 end = this.gameObject.transform.TransformPoint(_mesh.vertices[end_index]);

                Debug.DrawLine(start, end, color);
            }            
            */
        }
    }


    private void DrawSegmentDirections()
    {
        return;
        /*
        for (int p = 0; p < tube_vertices.Count; p++)
        {
            if (p < tube_vertices.Count - 1)
            {
                if (p > 0)
                {

                    Vector3 l = ((TubeVertex)tube_vertices[p - 1]).point;
                    Vector3 m = ((TubeVertex)tube_vertices[p]).point;
                    Vector3 n = ((TubeVertex)tube_vertices[p + 1]).point;

                    Vector3 a = (m - l).normalized;
                    Vector3 b = (n - m).normalized;
                    Quaternion rotation = Quaternion.FromToRotation(Vector3.forward, a + b);

                    m = this.gameObject.transform.TransformPoint(m);
                    n = this.gameObject.transform.TransformPoint(n);
                    float distance = Vector3.Distance(m, n);
                    Debug.DrawLine(m, m + rotation * new Vector3(0, 0, distance / 3.0f), Color.white);
                }
                else
                {
                    Vector3 m = ((TubeVertex)tube_vertices[p]).point;
                    Vector3 n = ((TubeVertex)tube_vertices[p + 1]).point;
                    Quaternion rotation = Quaternion.FromToRotation(Vector3.forward, n - m);


                    m = this.gameObject.transform.TransformPoint(m);
                    n = this.gameObject.transform.TransformPoint(n);
                    float distance = Vector3.Distance(m, n);
                    Debug.DrawLine(m, m + rotation * new Vector3(0, 0, distance / 3.0f), Color.white);
                }
            }
        }
        */
    }

    private void DrawAllSegments()
    {
        for (int i = 1; i < tube_vertices.Count - 1; i++)
        {
            DrawEndcap(i, Color.red);
        }
    }


    

    private void DrawNormals()
    {
        if (!_verts_cached)
        {
            _vertices = mesh.vertices;
            _normals = mesh.normals;
            _verts_cached = true;
        }
        for(int i = 0; i < mesh.vertexCount; i++)
        {
            Debug.DrawLine(this.gameObject.transform.TransformPoint(_vertices[i]), this.gameObject.transform.TransformPoint(_vertices[i] + _normals[i]), Color.blue);            
        }
    }


    public void Untruncate()
    {
        if (!_truncated)
            return;

        if (mesh == null)
        {
            Debug.LogError("Cannot untruncate because mesh is null");
            return;
        }

        UnityEngine.Profiling.Profiler.BeginSample("Untruncate");
        //_mesh.vertices = meshVertices;
        mesh.triangles = tris;
        //_mesh.uvs = uvs;
        //_mesh.colors = colors;
        //_mesh.normals = normals;

        _truncated = false;
        UnityEngine.Profiling.Profiler.EndSample();
    }


    public void Truncate(int truncation_index)
    {
        // the truncation_index is the tube vertex number (not mesh vertex)
        // the subset length needs to represent the output vertex number (the mesh vertex, not the tube vertex)
        int subset_length = (truncation_index + 1) * crossSegments * 6; // simply converting from index to length


        //int[] subsetTris = new int[subset_length * 6];
        //Array.Copy(tris, subsetTris, subset_length * 6);
        //mesh.triangles = subsetTris;
        
        for (int i = 0; i < degenerate_tris.Length; i++)
        {
            if (i < subset_length)
                degenerate_tris[i] = tris[i];
            else
                degenerate_tris[i] = 0; // degenerate triangles
        }
        mesh.triangles = degenerate_tris;

        _truncated = true;
    }


    void LateUpdate()
    {
        if (null == tube_vertices ||
            tube_vertices.Count <= 1)
        {
            mr.enabled = false;
            return;
        }
        mr.enabled = true;

        if (crossSegments != lastCrossSegments)
        {
            crossPoints = new Vector3[crossSegments];
            float theta = 2.0f * Mathf.PI / crossSegments;
            for (int c = 0; c < crossSegments; c++)
            {
                crossPoints[c] = new Vector3(Mathf.Cos(theta * c), Mathf.Sin(theta * c), 0);  // produces a normalized vector
            }
            lastCrossSegments = crossSegments;
            rebuild = true;
        }

        if (rebuild)
        {
            //draw tube

            int num_verts = tube_vertices.Count * crossSegments;
            if (num_verts > max_verts)
            {
                Debug.LogError("Cannot build tube. Maximum number of vertices exceeded.  " + num_verts + " > " + max_verts);
            }
            else
            {
                if (tris != null)
                {
                    Array.Clear(tris, 0, tris.Length);
                    tris = null;
                }

                // data with object lifetime
                Vector3[] meshVertices = new Vector3[tube_vertices.Count * crossSegments];
                tris = new int[tube_vertices.Count * crossSegments * 6];
                degenerate_tris = new int[tube_vertices.Count * crossSegments * 6];

                // data with rebuild lifetime
                //Vector2[] uvs = new Vector2[vertices.Count * crossSegments];
                Vector3[] normals = new Vector3[tube_vertices.Count * crossSegments];
                Color[] colors = new Color[tube_vertices.Count * crossSegments];
                int[] lastVertices = new int[crossSegments];
                int[] theseVertices = new int[crossSegments];
                Quaternion rotation = Quaternion.identity;
                Quaternion proportional_rotation = Quaternion.identity;
                Vector3 radial_adjustment_vector = Vector3.one;

                int tn_index = 0;
                LinkedListNode<TubeVertex> tn = tube_vertices.First;
                while(tn != null)
                {
                    if(tn != tube_vertices.Last)
                    {
                        if (tn == tube_vertices.First)
                        {
                            Vector3 m = tn.Value.point;
                            Vector3 n = tn.Next.Value.point;

                            Vector3 difference = n - m;
                            if (difference == Vector3.zero)
                                rotation = Quaternion.identity;
                            else
                                rotation = Quaternion.LookRotation(difference);
                            proportional_rotation = rotation;
                            radial_adjustment_vector = Vector3.one;
                        }
                        else
                        {
                            Vector3 l = tn.Previous.Value.point;
                            Vector3 m = tn.Value.point;
                            Vector3 n = tn.Next.Value.point;

                            // proportional quantities.  These quantities are proportional to the lengths of the segments involved
                            Vector3 side_proportional_a = m - l;
                            Vector3 side_proportional_b = n - m;

                            Vector3 proportional_sum = side_proportional_a + side_proportional_b;
                            if (proportional_sum == Vector3.zero)
                                proportional_rotation = Quaternion.LookRotation(Vector3.back); // The directions are exactly opposite of each other.  This is necessary because Quaternion.LookRotation(Vector3.zero) fails.
                            else
                                proportional_rotation = Quaternion.LookRotation(proportional_sum);

                            // normalized quantities.  These quantities are normal.  That is, they have equal lengths, irrespective of the lengths of their respective segments.
                            Vector3 a = side_proportional_a.normalized;
                            Vector3 b = side_proportional_b.normalized;

                            Vector3 sum = a + b;
                            if (sum == Vector3.zero) 
                                rotation = Quaternion.LookRotation(Vector3.back); // The directions are exactly opposite of each other.  This is necessary because Quaternion.LookRotation(Vector3.zero) fails.
                            else
                                rotation = Quaternion.LookRotation(sum);

                            float angle_bewteen_radians = Math3d.DotProductAngle(a, b);
                            angle_bewteen_radians = Mathf.Min(angle_bewteen_radians, Mathf.PI * 0.9f); // 2*PI = 360 degrees, PI = 180 degrees, 0.5*PI = 90 degrees

                            float hypotenuse_scale_factor = 1 / Mathf.Cos(angle_bewteen_radians / 2.0f);
                            radial_adjustment_vector = new Vector3(hypotenuse_scale_factor, 1, 1); // used to adjust the the width of the elbow, to maintain the radius of the tube
                        }
                    }

                    for (int c = 0; c < crossSegments; c++)
                    {
                        int vertexIndex = tn_index * crossSegments + c;
                        TubeVertex tv = tn.Value;
                        Vector3 scaled = Vector3.Scale(crossPoints[c], radial_adjustment_vector);
                        if (vertexIndex >= meshVertices.Length)
                        {
                            Debug.LogError("Out of bounds error.  vertexIndex is " + vertexIndex + " while meshVertices.Length is " + meshVertices.Length);
                        }
                        meshVertices[vertexIndex] = tv.point + rotation * scaled * tv.radius;
                        //Vector3 normal = meshVertices[vertexIndex] - tv.point; // This scales the normal a second time.  This will push the average more toward the scaled verts.
                        Vector3 normal = proportional_rotation * Vector3.Scale(crossPoints[c], radial_adjustment_vector) * tv.radius;
                        normals[vertexIndex] = normal;

                        //uvs[vertexIndex] = new Vector2((0.0f + c) / crossSegments, (0.0f + p) / vertices.Count);
                        colors[vertexIndex] = tv.color;

                        lastVertices[c] = theseVertices[c];
                        theseVertices[c] = tn_index * crossSegments + c;
                    }

                    //make triangles
                    if (tn_index > 0)
                    {
                        for (int c = 0; c < crossSegments; c++)
                        {
                            int start = (tn_index * crossSegments + c) * 6;
                            tris[start] = lastVertices[c];
                            tris[start + 1] = lastVertices[(c + 1) % crossSegments];
                            tris[start + 2] = theseVertices[c];
                            tris[start + 3] = tris[start + 2];
                            tris[start + 4] = tris[start + 1];
                            tris[start + 5] = theseVertices[(c + 1) % crossSegments];
                        }
                    }

                    tn_index++;
                    tn = tn.Next;
                }

                mesh.vertices = meshVertices;
                mesh.triangles = tris;
                //mesh.uv = uvs;
                mesh.colors = colors;
                mesh.normals = normals;

                Array.Copy(tris, degenerate_tris, degenerate_tris.Length);

                Array.Clear(lastVertices, 0, lastVertices.Length);
                lastVertices = null;
                Array.Clear(theseVertices, 0, theseVertices.Length);
                theseVertices = null;

                rebuild = false;
            }

        } // if(rebuild)

        if (tube_vertices.Count > 0)
        {
            if (Debug_Draw_End_Caps)
            {
                DrawEndcap(0, Color.green);
                DrawEndcap(tube_vertices.Count - 1, Color.red);
            }
            if (Debug_Draw_Normals)
            {
                DrawNormals();
            }
            if (Debug_Draw_Segment_Directions)
            {
                DrawSegmentDirections();
            }
            if (Debug_Draw_All_Segments)
            {
                DrawAllSegments();
            }
        }
    }


    public void AppendPoint(Vector3 point, float radius, Color col)
    {
        AppendPoint(new TubeVertex(point, radius, col));
    }


    public void AppendPoint(TubeVertex tv)
    {
        tube_vertices.AddLast(tv);
    }


    public void Rebuild()
    {
        if(tube_vertices.Count < 2)
        {
            Debug.LogError("Cannot rebuild TubeRenderer because it has fewer than two TubeVertex's.");
            return;
        }

        //TubeVertex z = ((TubeVertex)vertices[vertices.Count - 1]);        
        //vertices.Add(new TubeVertex(z.point, 0.0f, z.color)); // create the tail point

        rebuild = true;
    }


    public void SetColors(Color[] colors)
    {
        mesh.colors = colors;
    }


}