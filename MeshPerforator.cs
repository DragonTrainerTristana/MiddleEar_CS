using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[System.Serializable]
public class PerforationData
{
    [Header("천공 속성")]
    public Vector3 position;            // 월드 좌표
    public float radius;                // 반지름 (미터)
    public float depth;                 // 깊이 (0=표면, 1=완전천공)
    public int perforationID;           // 고유 ID
    
    [Header("물리적 영향")]
    public float acousticLoss;          // dB 손실
    public float frequencyShift;        // 주파수 응답 변화
    public List<int> affectedVertices;  // 영향받는 vertex 인덱스들
    
    [Header("시각적 속성")]
    public Color perforationColor = Color.red;
    public bool showGizmo = true;
    
    public PerforationData()
    {
        affectedVertices = new List<int>();
        perforationColor = Color.red;
    }
}

[System.Serializable]
public class PerforationSettings
{
    [Header("생성 설정")]
    [Range(0.0001f, 0.01f)] public float minRadius = 0.0005f;    // 0.5mm
    [Range(0.001f, 0.02f)] public float maxRadius = 0.005f;      // 5mm
    [Range(1, 20)] public int maxPerforations = 5;
    
    [Header("물리적 모델링")]
    public bool enableAcousticModeling = true;
    public AnimationCurve sizeToLossCurve;                       // 크기별 dB 손실 곡선
    public AnimationCurve positionToLossCurve;                   // 위치별 손실 곡선
    
    [Header("실시간 편집")]
    public bool allowRuntimeEditing = true;
    public KeyCode addPerforationKey = KeyCode.P;
    public KeyCode removePerforationKey = KeyCode.R;
    
    [Header("시각화")]
    public Material perforationMaterial;
    public bool showPerforationGizmos = true;
    public bool showAffectedArea = true;
}

public class MeshPerforator : MonoBehaviour
{
    [Header("Settings")]
    public PerforationSettings settings;
    
    [Header("Current Perforations")]
    public List<PerforationData> perforations = new List<PerforationData>();
    
    [Header("Debug")]
    public bool logPerforationData = false;
    public bool showDebugInfo = true;
    
    // Private variables
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private Mesh originalMesh;
    private Mesh currentMesh;
    private Vector3[] originalVertices;
    private Vector3[] currentVertices;
    private int[] originalTriangles;
    private Vector2[] originalUVs;
    private Color[] vertexColors;
    private TympanicMembrane tympanicMembrane;
    
    // Perforation tracking
    private int nextPerforationID = 1;
    private Camera playerCamera;
    private bool isDirty = false;
    
    void Start()
    {
        InitializeComponents();
        SetupDefaultCurves();
        CreateInitialMesh();
    }
    
    void InitializeComponents()
    {
        // Get required components
        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();
        tympanicMembrane = GetComponent<TympanicMembrane>();
        
        if (meshFilter == null)
        {
            Debug.LogError("MeshPerforator requires a MeshFilter component!");
            enabled = false;
            return;
        }
        
        // Get camera for mouse interaction
        playerCamera = Camera.main;
        if (playerCamera == null)
            playerCamera = FindObjectOfType<Camera>();
            
        // Store original mesh data
        originalMesh = meshFilter.sharedMesh;
        if (originalMesh == null)
        {
            Debug.LogError("No mesh found in MeshFilter!");
            enabled = false;
            return;
        }
        
        originalVertices = originalMesh.vertices;
        originalTriangles = originalMesh.triangles;
        originalUVs = originalMesh.uv;
        
        Debug.Log($"MeshPerforator initialized with {originalVertices.Length} vertices");
    }
    
    void SetupDefaultCurves()
    {
        // 크기별 dB 손실 곡선 (실제 의학 데이터 기반)
        if (settings.sizeToLossCurve == null || settings.sizeToLossCurve.keys.Length == 0)
        {
            Keyframe[] sizeKeys = new Keyframe[]
            {
                new Keyframe(0.0005f, 5f),    // 0.5mm → 5dB 손실
                new Keyframe(0.001f, 10f),    // 1mm → 10dB 손실
                new Keyframe(0.002f, 20f),    // 2mm → 20dB 손실
                new Keyframe(0.003f, 30f),    // 3mm → 30dB 손실
                new Keyframe(0.005f, 45f),    // 5mm → 45dB 손실
                new Keyframe(0.01f, 60f)      // 10mm → 60dB 손실
            };
            settings.sizeToLossCurve = new AnimationCurve(sizeKeys);
        }
        
        // 위치별 손실 곡선 (고막 해부학적 위치 기반)
        if (settings.positionToLossCurve == null || settings.positionToLossCurve.keys.Length == 0)
        {
            Keyframe[] positionKeys = new Keyframe[]
            {
                new Keyframe(0f, 1.0f),       // 중앙부
                new Keyframe(0.3f, 1.2f),     // 중앙-주변부
                new Keyframe(0.6f, 0.8f),     // 주변부
                new Keyframe(1.0f, 0.6f)      // 가장자리
            };
            settings.positionToLossCurve = new AnimationCurve(positionKeys);
        }
    }
    
    void CreateInitialMesh()
    {
        // Create working copy of the mesh
        currentMesh = Instantiate(originalMesh);
        currentVertices = (Vector3[])originalVertices.Clone();
        
        // Initialize vertex colors for visualization
        vertexColors = new Color[originalVertices.Length];
        for (int i = 0; i < vertexColors.Length; i++)
        {
            vertexColors[i] = Color.white;
        }
        
        ApplyMeshChanges();
    }
    
    void Update()
    {
        if (settings.allowRuntimeEditing)
        {
            HandleInput();
        }
        
        if (isDirty)
        {
            UpdatePerforationEffects();
            ApplyMeshChanges();
            isDirty = false;
        }
    }
    
    void HandleInput()
    {
        // 천공 추가
        if (Input.GetKeyDown(settings.addPerforationKey))
        {
            if (Input.GetMouseButton(0)) // 마우스 왼쪽 버튼과 함께
            {
                Vector3 mousePos = Input.mousePosition;
                AddPerforationAtScreenPoint(mousePos);
            }
            else
            {
                // 랜덤 위치에 천공 추가
                AddRandomPerforation();
            }
        }
        
        // 천공 제거
        if (Input.GetKeyDown(settings.removePerforationKey))
        {
            if (perforations.Count > 0)
            {
                RemovePerforation(perforations.Count - 1);
            }
        }
        
        // 마우스 클릭으로 천공 생성 (디버그 모드)
        if (Input.GetMouseButtonDown(1) && Input.GetKey(KeyCode.LeftControl)) // Ctrl + 우클릭
        {
            Vector3 mousePos = Input.mousePosition;
            AddPerforationAtScreenPoint(mousePos);
        }
    }
    
    void AddPerforationAtScreenPoint(Vector3 screenPoint)
    {
        if (playerCamera == null) return;
        
        Ray ray = playerCamera.ScreenPointToRay(screenPoint);
        RaycastHit hit;
        
        if (Physics.Raycast(ray, out hit))
        {
            if (hit.transform == this.transform)
            {
                // Convert world position to local position
                Vector3 localHitPoint = transform.InverseTransformPoint(hit.point);
                
                // Random size within bounds
                float radius = Random.Range(settings.minRadius, settings.maxRadius);
                
                CreatePerforation(localHitPoint, radius, 1.0f); // Full depth
            }
        }
    }
    
    void AddRandomPerforation()
    {
        if (perforations.Count >= settings.maxPerforations) return;
        
        // Random position within mesh bounds
        Bounds bounds = originalMesh.bounds;
        Vector3 randomPos = new Vector3(
            Random.Range(bounds.min.x, bounds.max.x),
            Random.Range(bounds.min.y, bounds.max.y),
            Random.Range(bounds.min.z, bounds.max.z)
        );
        
        float radius = Random.Range(settings.minRadius, settings.maxRadius);
        CreatePerforation(randomPos, radius, Random.Range(0.5f, 1.0f));
    }
    
    public void CreatePerforation(Vector3 localPosition, float radius, float depth = 1.0f)
    {
        if (perforations.Count >= settings.maxPerforations)
        {
            Debug.LogWarning($"Maximum perforations ({settings.maxPerforations}) reached!");
            return;
        }
        
        PerforationData newPerforation = new PerforationData
        {
            position = localPosition,
            radius = Mathf.Clamp(radius, settings.minRadius, settings.maxRadius),
            depth = Mathf.Clamp01(depth),
            perforationID = nextPerforationID++
        };
        
        // Find affected vertices
        FindAffectedVertices(newPerforation);
        
        // Calculate acoustic effects
        CalculateAcousticEffects(newPerforation);
        
        perforations.Add(newPerforation);
        isDirty = true;
        
        if (logPerforationData)
        {
            Debug.Log($"Created perforation {newPerforation.perforationID}: " +
                     $"Position {localPosition}, Radius {radius:F4}m, " +
                     $"Acoustic Loss {newPerforation.acousticLoss:F1}dB, " +
                     $"Affected Vertices: {newPerforation.affectedVertices.Count}");
        }
    }
    
    void FindAffectedVertices(PerforationData perforation)
    {
        perforation.affectedVertices.Clear();
        
        for (int i = 0; i < originalVertices.Length; i++)
        {
            float distance = Vector3.Distance(originalVertices[i], perforation.position);
            
            if (distance <= perforation.radius)
            {
                perforation.affectedVertices.Add(i);
            }
        }
    }
    
    void CalculateAcousticEffects(PerforationData perforation)
    {
        // 크기에 따른 기본 손실 계산
        float sizeLoss = settings.sizeToLossCurve.Evaluate(perforation.radius);
        
        // 위치에 따른 가중치 계산
        Vector3 center = Vector3.zero; // 고막의 중심점
        float distanceFromCenter = Vector3.Distance(perforation.position, center);
        float maxDistance = GetMaxDistanceFromCenter();
        float normalizedDistance = distanceFromCenter / maxDistance;
        
        float positionWeight = settings.positionToLossCurve.Evaluate(normalizedDistance);
        
        // 최종 음향 손실 계산
        perforation.acousticLoss = sizeLoss * positionWeight;
        
        // 주파수 시프트 계산 (큰 천공일수록 저주파 영향 큼)
        perforation.frequencyShift = perforation.radius * 1000f; // 주파수 시프트 (Hz)
    }
    
    float GetMaxDistanceFromCenter()
    {
        Vector3 center = Vector3.zero;
        float maxDistance = 0f;
        
        for (int i = 0; i < originalVertices.Length; i++)
        {
            float distance = Vector3.Distance(originalVertices[i], center);
            if (distance > maxDistance)
                maxDistance = distance;
        }
        
        return maxDistance;
    }
    
    void UpdatePerforationEffects()
    {
        // Reset vertex colors
        for (int i = 0; i < vertexColors.Length; i++)
        {
            vertexColors[i] = Color.white;
        }
        
        // Reset vertices to original positions
        System.Array.Copy(originalVertices, currentVertices, originalVertices.Length);
        
        // Apply each perforation
        foreach (var perforation in perforations)
        {
            ApplyPerforationToMesh(perforation);
        }
        
        // Notify TympanicMembrane of perforations
        if (tympanicMembrane != null)
        {
            List<int> allAffectedVertices = new List<int>();
            foreach (var perforation in perforations)
            {
                allAffectedVertices.AddRange(perforation.affectedVertices);
            }
            tympanicMembrane.SetPerforation(allAffectedVertices.Distinct().ToList());
        }
    }
    
    void ApplyPerforationToMesh(PerforationData perforation)
    {
        foreach (int vertexIndex in perforation.affectedVertices)
        {
            if (vertexIndex >= 0 && vertexIndex < currentVertices.Length)
            {
                Vector3 originalPos = originalVertices[vertexIndex];
                float distanceFromPerfCenter = Vector3.Distance(originalPos, perforation.position);
                float normalizedDistance = distanceFromPerfCenter / perforation.radius;
                
                // Create hole effect (move vertices inward/remove them)
                if (perforation.depth > 0.9f) // Complete perforation
                {
                    // Move vertex significantly inward to create hole
                    Vector3 direction = (originalPos - perforation.position).normalized;
                    float displacement = (1.0f - normalizedDistance) * perforation.radius * 0.5f;
                    currentVertices[vertexIndex] = originalPos - direction * displacement;
                }
                else
                {
                    // Partial indentation
                    Vector3 direction = (originalPos - perforation.position).normalized;
                    float displacement = (1.0f - normalizedDistance) * perforation.radius * perforation.depth * 0.3f;
                    currentVertices[vertexIndex] = originalPos - direction * displacement;
                }
                
                // Color affected vertices
                float colorIntensity = 1.0f - normalizedDistance;
                vertexColors[vertexIndex] = Color.Lerp(Color.white, perforation.perforationColor, colorIntensity);
            }
        }
    }
    
    void ApplyMeshChanges()
    {
        if (currentMesh == null) return;
        
        // Update mesh vertices
        currentMesh.vertices = currentVertices;
        currentMesh.colors = vertexColors;
        
        // Recalculate normals and bounds
        currentMesh.RecalculateNormals();
        currentMesh.RecalculateBounds();
        
        // Apply to mesh filter
        meshFilter.mesh = currentMesh;
    }
    
    public void RemovePerforation(int index)
    {
        if (index >= 0 && index < perforations.Count)
        {
            int removedID = perforations[index].perforationID;
            perforations.RemoveAt(index);
            isDirty = true;
            
            if (logPerforationData)
            {
                Debug.Log($"Removed perforation {removedID}");
            }
        }
    }
    
    public void RemoveAllPerforations()
    {
        perforations.Clear();
        isDirty = true;
        
        if (logPerforationData)
        {
            Debug.Log("Removed all perforations");
        }
    }
    
    public void SetPerforationSize(float normalizedSize)
    {
        if (perforations.Count == 0) return;
        
        // Modify the most recent perforation
        var lastPerforation = perforations[perforations.Count - 1];
        float newRadius = Mathf.Lerp(settings.minRadius, settings.maxRadius, normalizedSize);
        
        lastPerforation.radius = newRadius;
        FindAffectedVertices(lastPerforation);
        CalculateAcousticEffects(lastPerforation);
        
        isDirty = true;
    }
    
    // Public API methods
    public float GetTotalAcousticLoss()
    {
        float totalLoss = 0f;
        foreach (var perforation in perforations)
        {
            totalLoss += perforation.acousticLoss;
        }
        return totalLoss;
    }
    
    public int GetPerforationCount()
    {
        return perforations.Count;
    }
    
    public List<PerforationData> GetPerforations()
    {
        return new List<PerforationData>(perforations);
    }
    
    public float GetPerforationRatio()
    {
        if (originalVertices.Length == 0) return 0f;
        
        HashSet<int> uniqueAffectedVertices = new HashSet<int>();
        foreach (var perforation in perforations)
        {
            foreach (int vertexIndex in perforation.affectedVertices)
            {
                uniqueAffectedVertices.Add(vertexIndex);
            }
        }
        
        return (float)uniqueAffectedVertices.Count / originalVertices.Length;
    }
    
    void OnDrawGizmosSelected()
    {
        if (!settings.showPerforationGizmos || perforations == null) return;
        
        foreach (var perforation in perforations)
        {
            Vector3 worldPos = transform.TransformPoint(perforation.position);
            
            // Draw perforation sphere
            Gizmos.color = perforation.perforationColor;
            Gizmos.DrawWireSphere(worldPos, perforation.radius);
            
            // Draw affected area
            if (settings.showAffectedArea)
            {
                Gizmos.color = new Color(perforation.perforationColor.r, 
                                       perforation.perforationColor.g, 
                                       perforation.perforationColor.b, 0.3f);
                Gizmos.DrawSphere(worldPos, perforation.radius);
            }
            
            // Draw ID label
            if (Application.isEditor)
            {
                UnityEditor.Handles.Label(worldPos, $"P{perforation.perforationID}\n{perforation.acousticLoss:F1}dB");
            }
        }
    }
    
    void OnGUI()
    {
        if (!showDebugInfo || !Application.isEditor) return;
        
        GUILayout.BeginArea(new Rect(10, Screen.height - 200, 400, 190));
        GUILayout.BeginVertical("box");
        
        GUILayout.Label("Mesh Perforator Debug");
        
        GUILayout.Space(5);
        GUILayout.Label($"Perforations: {perforations.Count} / {settings.maxPerforations}");
        GUILayout.Label($"Total Acoustic Loss: {GetTotalAcousticLoss():F1} dB");
        GUILayout.Label($"Perforation Ratio: {GetPerforationRatio():P1}");
        
        GUILayout.Space(5);
        GUILayout.Label("Controls:");
        GUILayout.Label($"'{settings.addPerforationKey}' - Add perforation");
        GUILayout.Label($"'{settings.removePerforationKey}' - Remove perforation");
        GUILayout.Label("Ctrl + Right Click - Add at cursor");
        
        if (GUILayout.Button("Remove All Perforations"))
        {
            RemoveAllPerforations();
        }
        
        GUILayout.EndVertical();
        GUILayout.EndArea();
    }
}