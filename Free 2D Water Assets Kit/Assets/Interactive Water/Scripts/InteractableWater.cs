using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(EdgeCollider2D))]
[RequireComponent(typeof(WaterTriggerHandle))]
public class InteractableWater : MonoBehaviour
{
    [Header("Spring")]
    [SerializeField] private float _springConstCount = 1.4f;
    [SerializeField] private float _damping = 1.1f;
    [SerializeField] private float _spread = 6.5f;
    [SerializeField, Range(1, 10)] private int _wavePropogationIterations = 8;
    [SerializeField, Range(0, 20f)] private float _speedMult = 5.5f;

    [Header("Force")]
    public float ForceMultiplier = 0.3f;
    [Range(1f, 50f)] public float MaxForce = 5f;

    [Header("Collision")]
    [SerializeField, Range(1f, 10f)] private float _playerCollisionRadiusMult = 4.15f;

    [Header("Mesh Generation")]
    [Range(2, 500)]
    public int XVerticies = 70;
    public float width = 10f;
    public float height = 4f;
    public Material WaterMaterial;
    private const int YVerticies = 2;

    [Header("Gizmos")]
    public Color GizmoColor = Color.white;

    private Mesh _mesh;
    private MeshRenderer _meshRenderer;
    private MeshFilter _meshFilter;
    private Vector3[] _verticies;
    private int[] _TopVerticiesIndex;

    private EdgeCollider2D _coll;

    private class WaterPoint
    {
        public float velocity, pos, targetHeight;
    }

    private List<WaterPoint> _waterPoints = new List<WaterPoint>();

    private void Start()
    {
        _coll = GetComponent<EdgeCollider2D>();

        GenerateMesh();
        CreateWaterPoints();
    }

    private void FixedUpdate()
    {
        //update all soring positions
        for (int i = 1; i < _waterPoints.Count - 1; i++)
        {
            WaterPoint points = _waterPoints[i];

            float x = points.pos - points.targetHeight;
            float acceleration = -_springConstCount * x - _damping * points.velocity;
            points.pos = points.velocity * _speedMult * Time.fixedDeltaTime;
            _verticies[_TopVerticiesIndex[i]].y = points.pos;
            points.velocity += acceleration * _speedMult * Time.fixedDeltaTime;
        }
        //wave propogation
        for (int j = 0; j < _wavePropogationIterations; j++)
        {
            for (int i = 1; i < _waterPoints.Count - 1; i++)
            {
                float leftDelta = _spread * (_waterPoints[i].pos - _waterPoints[i - 1].pos) * _speedMult * Time.fixedDeltaTime;
                _waterPoints[i - 1].velocity += leftDelta;

                float rightDelta = _spread * (_waterPoints[i].pos - _waterPoints[i + 1].pos) * _speedMult * Time.fixedDeltaTime;
                _waterPoints[i + 1].velocity += rightDelta;
            }
        }
        //update mesh
        _mesh.vertices = _verticies; 

    }

    public void Splash(Collider2D collision, float force)
    {
        float radius = collision.bounds.extents.x * _playerCollisionRadiusMult;
        Vector2 center = collision.transform.position;

        for (int i = 0; i < _waterPoints.Count; i++)
        {
            Vector2 vertexWorldPos = transform.TransformPoint(_verticies[_TopVerticiesIndex[i]]);
            if (IsPointInsideCircle(vertexWorldPos, center, radius))
            {
                _waterPoints[i].velocity = force;
            }
        }

    }

    private bool IsPointInsideCircle(Vector2 point, Vector2 center, float radius)
    {
        float distanceSquared = (point - center).sqrMagnitude;
        return distanceSquared <= radius * radius;
    }
    private void Reset()
    {
        _coll = GetComponent<EdgeCollider2D>();
        _coll.isTrigger = true;
    }

    public void ResetEdgeCollider()
    {
        _coll = GetComponent<EdgeCollider2D>();

        Vector2[] newPoints = new Vector2[2];

        Vector2 firstPoint = new Vector2(_verticies[_TopVerticiesIndex[0]].x, _verticies[_TopVerticiesIndex[0]].y);
        newPoints[0] = firstPoint;

        Vector2 secondPoint = new Vector2(_verticies[_TopVerticiesIndex[_TopVerticiesIndex.Length - 1]].x, _verticies[-_TopVerticiesIndex[_TopVerticiesIndex.Length - 1]].y);
        newPoints[1] = secondPoint;

        _coll.offset = Vector2.zero;
        _coll.points = newPoints;

    }

    public void GenerateMesh()
    {
        _mesh = new Mesh();

        //add verticies

        _verticies = new Vector3[XVerticies * YVerticies];
        _TopVerticiesIndex = new int[XVerticies];

        for (int y = 0; y < YVerticies; y++)
        {
            for (int x = 0; x < XVerticies; x++)
            {
                float xPos = (x / (float)(XVerticies - 1)) * width - width / 2;
                float yPos = (y / (float)(YVerticies - 1)) * height - height / 2;
                _verticies[y * XVerticies + x] = new Vector3(xPos, yPos, 0f);

                if (y == YVerticies - 1)
                {
                    _TopVerticiesIndex[x] = y * XVerticies + x;
                }
            }
        }

        //contruct triangles

        int[] triangles = new int[(XVerticies - 1) * (YVerticies - 1) * 6];
        int index = 0;

        for (int y = 0; y < YVerticies - 1; y++)
        {
            for (int x = 0; x < XVerticies - 1; x++)
            {
                int bottomLeft = y * XVerticies + x;
                int bottomRight = bottomLeft + 1;
                int topLeft = bottomLeft + XVerticies;
                int topRight = topLeft + 1;

                //First triangle
                triangles[index++] = bottomLeft;
                triangles[index++] = topLeft;
                triangles[index++] = bottomRight;

                //Second triangle
                triangles[index++] = bottomRight;
                triangles[index++] = topLeft;
                triangles[index++] = topRight;

            }
        }

        //UVs
        Vector2[] uvs = new Vector2[_verticies.Length];
        for (int i = 0; i < _verticies.Length; i++)
        {
            uvs[i] = new Vector2((_verticies[i].x + width / 2) / width, (_verticies[i].y + height / 2) / height);
        }

        if (_meshRenderer == null)
            _meshRenderer = GetComponent<MeshRenderer>();

        if (_meshFilter == null)
            _meshFilter = GetComponent<MeshFilter>();

        _meshRenderer.material = WaterMaterial;

        _mesh.vertices = _verticies;
        _mesh.triangles = triangles;
        _mesh.uv = uvs;

        _mesh.RecalculateBounds();
        _mesh.RecalculateNormals();

        _meshFilter.mesh = _mesh;

    }

    private void CreateWaterPoints()
    {
        _waterPoints.Clear();

        for (int i = 0; i < _TopVerticiesIndex.Length; i++)
        {
            _waterPoints.Add(new WaterPoint
            {

                pos = _verticies[_TopVerticiesIndex[i]].y,
                targetHeight = _verticies[_TopVerticiesIndex[i]].y
            }); 
        }
    }

}
#if UNITY_EDITOR

[CustomEditor(typeof(InteractableWater))]
public class InteractableWaterEditor : Editor
{
    private InteractableWater _water;

    private void OnEnable()
    {
        _water = (InteractableWater)target;
    }

    public override VisualElement CreateInspectorGUI()
    {
        VisualElement _root = new VisualElement();

        InspectorElement.FillDefaultInspector(_root,serializedObject,this);

        _root.Add(new VisualElement { style = { height = 10 } });

        Button generateMeshButon = new Button(() => _water.GenerateMesh())
        {
            text = "Generate Mesh"
        };
        _root.Add(generateMeshButon);

        Button placeEdgeColliderButon = new Button(() => _water.ResetEdgeCollider())
        {
            text = "Place Edge Collider"
        };
        _root.Add(placeEdgeColliderButon);

        return _root;
    }

    private void ChangeDimensions(ref float width, ref float height, float calculatedWidthMax, float calculatedHeightMax)
    {
        width = Mathf.Max(0.1f, width);
        height = Mathf.Max(0.1f, height);
    }

    private void OnSceneGUI()
    {
        //Draw the wireframe box
        Handles.color = _water.GizmoColor;
        Vector3 center = _water.transform.position;
        Vector3 size = new Vector3(_water.width, _water.height, 0.1f);
        Handles.DrawWireCube(center,size);

        //Handles for width and height
        float handleSize = HandleUtility.GetHandleSize(size) * 0.1f;
        Vector3 snap = Vector3.one * 0.1f;

        // Corner handles
        Vector3[] corners = new Vector3[4];
        corners[0] = center + new Vector3(-_water.width / 2, -_water.height / 2, 0); //Bottom-left
        corners[1] = center + new Vector3(_water.width / 2, -_water.height / 2, 0); //Bottom-right
        corners[2] = center + new Vector3(-_water.width / 2, _water.height / 2, 0); //Top-left
        corners[3] = center + new Vector3(_water.width / 2, _water.height / 2, 0); //Top-right

        // Handle for each corner
        EditorGUI.BeginChangeCheck();
        Vector3 newBotomLeft = Handles.FreeMoveHandle(corners[0], handleSize, snap, Handles.CubeHandleCap);
        if (EditorGUI.EndChangeCheck())
        {
            ChangeDimensions(ref _water.width, ref _water.height, corners[1].x - newBotomLeft.x, corners[3].y - newBotomLeft.y);
            _water.transform.position += new Vector3((newBotomLeft.x - corners[0].x) / 2, (newBotomLeft.y - corners[0].y) / 2, 0);
        }

        EditorGUI.BeginChangeCheck();
        Vector3 newBotomRight = Handles.FreeMoveHandle(corners[1], handleSize, snap, Handles.CubeHandleCap);
        if (EditorGUI.EndChangeCheck())
        {
            ChangeDimensions(ref _water.width, ref _water.height, newBotomRight.x - corners[0].x, corners[3].y - newBotomRight.y);
            _water.transform.position += new Vector3((newBotomRight.x - corners[1].x) / 2, (newBotomRight.y - corners[1].y) / 2, 0);
        }

        EditorGUI.BeginChangeCheck();
        Vector3 newTopLeft = Handles.FreeMoveHandle(corners[2], handleSize, snap, Handles.CubeHandleCap);
        if (EditorGUI.EndChangeCheck())
        {
            ChangeDimensions(ref _water.width, ref _water.height, corners[3].x - newTopLeft.x, newTopLeft.y - corners[0].y);
            _water.transform.position += new Vector3((newTopLeft.x - corners[2].x) / 2, (newTopLeft.y - corners[2].y) / 2, 0);
        }

        EditorGUI.BeginChangeCheck();
        Vector3 newTopRight = Handles.FreeMoveHandle(corners[3], handleSize, snap, Handles.CubeHandleCap);
        if (EditorGUI.EndChangeCheck())
        {
            ChangeDimensions(ref _water.width, ref _water.height, newTopRight.x - corners[2].x, newTopRight.y - corners[1].y);
            _water.transform.position += new Vector3((newTopRight.x - corners[3].x) / 2, (newTopRight.y - corners[3].y) / 2, 0);
        }
        //Update the mesh if the handldes are moved

        if (GUI.changed)
            _water.GenerateMesh();
    }

}

#endif