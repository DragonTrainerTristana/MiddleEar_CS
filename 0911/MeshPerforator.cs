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
    
    // Debug flags (한번만 출력)
    private bool hasLoggedInitialization = false;
    private bool hasLoggedPerforationError = false;
    
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
            // Only update mesh when actually needed
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
        
        // Safety check for screen point
        if (!IsValidVector3(screenPoint)) return;
        
        Ray ray = playerCamera.ScreenPointToRay(screenPoint);
        RaycastHit hit;
        
        if (Physics.Raycast(ray, out hit))
        {
            if (hit.transform == this.transform)
            {
                // Safety check for hit point
                if (!IsValidVector3(hit.point)) return;
                
                // Convert world position to local position
                Vector3 localHitPoint = transform.InverseTransformPoint(hit.point);
                
                // Safety check for transformed point
                if (!IsValidVector3(localHitPoint)) return;
                
                // Random size within bounds with safety checks
                float radius = Random.Range(settings.minRadius, settings.maxRadius);
                if (!float.IsFinite(radius) || radius <= 0f)
                {
                    radius = settings.minRadius; // Fallback
                }
                
                CreatePerforation(localHitPoint, radius, 1.0f); // Full depth
            }
        }
    }
    
    void AddRandomPerforation()
    {
        if (perforations.Count >= settings.maxPerforations) return;
        if (originalMesh == null) return;
        
        // Random position within mesh bounds with safety checks
        Bounds bounds = originalMesh.bounds;
        
        // Safety check for bounds
        if (!IsValidVector3(bounds.min) || !IsValidVector3(bounds.max)) return;
        
        Vector3 randomPos = new Vector3(
            Random.Range(bounds.min.x, bounds.max.x),
            Random.Range(bounds.min.y, bounds.max.y),
            Random.Range(bounds.min.z, bounds.max.z)
        );
        
        // Safety check for generated random position
        if (!IsValidVector3(randomPos)) return;
        
        // Safety check for radius
        float radius = Random.Range(settings.minRadius, settings.maxRadius);
        if (!float.IsFinite(radius) || radius <= 0f)
        {
            radius = settings.minRadius; // Fallback
        }
        
        // Safety check for depth
        float depth = Random.Range(0.5f, 1.0f);
        if (!float.IsFinite(depth))
        {
            depth = 1.0f; // Fallback
        }
        
        CreatePerforation(randomPos, radius, depth);
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
        
        if (logPerforationData && !hasLoggedPerforationError)
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
        
        // Safety check for perforation data
        if (perforation == null || !IsValidVector3(perforation.position)) return;
        
        for (int i = 0; i < originalVertices.Length; i++)
        {
            // Safety check for vertex position
            if (!IsValidVector3(originalVertices[i])) continue;
            
            float distance = Vector3.Distance(originalVertices[i], perforation.position);
            
            // Safety check for distance calculation
            if (!float.IsFinite(distance)) continue;
            
            if (distance <= perforation.radius)
            {
                perforation.affectedVertices.Add(i);
            }
        }
    }
    
    void CalculateAcousticEffects(PerforationData perforation)
    {
        // Safety check for perforation data
        if (perforation == null) return;
        
        // Validate radius
        float safeRadius = Mathf.Clamp(perforation.radius, 0.0001f, 0.1f);
        
        // 크기에 따른 기본 손실 계산 with safety check
        float sizeLoss = 0f;
        if (settings.sizeToLossCurve != null)
        {
            sizeLoss = settings.sizeToLossCurve.Evaluate(safeRadius);
            if (!float.IsFinite(sizeLoss))
                sizeLoss = 10f; // Default reasonable value
        }
        else
        {
            sizeLoss = 10f; // Default if curve is missing
        }
        
        // 위치에 따른 가중치 계산 with safety checks
        Vector3 center = Vector3.zero; // 고막의 중심점
        float distanceFromCenter = IsValidVector3(perforation.position) ? 
            Vector3.Distance(perforation.position, center) : 0f;
            
        float maxDistance = GetMaxDistanceFromCenter();
        
        // Prevent division by zero
        if (maxDistance <= 0.0001f)
        {
            maxDistance = 0.01f; // Default 1cm
        }
        
        float normalizedDistance = Mathf.Clamp01(distanceFromCenter / maxDistance);
        
        float positionWeight = 1.0f;
        if (settings.positionToLossCurve != null)
        {
            positionWeight = settings.positionToLossCurve.Evaluate(normalizedDistance);
            if (!float.IsFinite(positionWeight))
                positionWeight = 1.0f;
        }
        
        // 최종 음향 손실 계산 with bounds check
        perforation.acousticLoss = Mathf.Clamp(sizeLoss * positionWeight, 0f, 100f);
        
        // 주파수 시프트 계산 with safety check
        perforation.frequencyShift = Mathf.Clamp(safeRadius * 1000f, 0f, 20000f);
    }
    
    // Safety check function for Vector3 validity
    bool IsValidVector3(Vector3 v)
    {
        return float.IsFinite(v.x) && float.IsFinite(v.y) && float.IsFinite(v.z);
    }
    
    float GetMaxDistanceFromCenter()
    {
        Vector3 center = Vector3.zero;
        float maxDistance = 0f;
        
        for (int i = 0; i < originalVertices.Length; i++)
        {
            // Safety check for vertex position
            if (!IsValidVector3(originalVertices[i])) continue;
            
            float distance = Vector3.Distance(originalVertices[i], center);
            
            // Safety check for distance calculation
            if (!float.IsFinite(distance)) continue;
            
            if (distance > maxDistance)
                maxDistance = distance;
        }
        
        // Ensure we return a valid positive distance
        return maxDistance > 0.0001f ? maxDistance : 0.01f; // Default 1cm
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
        // Safety checks
        if (perforation == null || !IsValidVector3(perforation.position)) return;
        if (perforation.radius <= 0f || !float.IsFinite(perforation.radius)) return;
        
        foreach (int vertexIndex in perforation.affectedVertices)
        {
            if (vertexIndex >= 0 && vertexIndex < currentVertices.Length)
            {
                Vector3 originalPos = originalVertices[vertexIndex];
                
                // Safety check for vertex position
                if (!IsValidVector3(originalPos)) continue;
                
                float distanceFromPerfCenter = Vector3.Distance(originalPos, perforation.position);
                
                // Safety check for distance calculation
                if (!float.IsFinite(distanceFromPerfCenter)) continue;
                
                // Prevent division by zero
                float normalizedDistance = (perforation.radius > 0.0001f) ? 
                    Mathf.Clamp01(distanceFromPerfCenter / perforation.radius) : 0f;
                
                // Create hole effect (move vertices inward/remove them)
                if (perforation.depth > 0.9f) // Complete perforation
                {
                    // Move vertex significantly inward to create hole
                    Vector3 direction = (originalPos - perforation.position);
                    
                    // Safety check for direction vector
                    if (direction.magnitude > 0.0001f)
                    {
                        direction = direction.normalized;
                        
                        // Safety check for normalized direction
                        if (IsValidVector3(direction))
                        {
                            float displacement = (1.0f - normalizedDistance) * perforation.radius * 0.5f;
                            Vector3 newPos = originalPos - direction * displacement;
                            
                            // Safety check for final position
                            if (IsValidVector3(newPos))
                            {
                                currentVertices[vertexIndex] = newPos;
                            }
                        }
                    }
                }
                else
                {
                    // Partial indentation
                    Vector3 direction = (originalPos - perforation.position);
                    
                    // Safety check for direction vector
                    if (direction.magnitude > 0.0001f)
                    {
                        direction = direction.normalized;
                        
                        // Safety check for normalized direction
                        if (IsValidVector3(direction))
                        {
                            float displacement = (1.0f - normalizedDistance) * perforation.radius * perforation.depth * 0.3f;
                            Vector3 newPos = originalPos - direction * displacement;
                            
                            // Safety check for final position
                            if (IsValidVector3(newPos))
                            {
                                currentVertices[vertexIndex] = newPos;
                            }
                        }
                    }
                }
                
                // Color affected vertices with safety check
                float colorIntensity = Mathf.Clamp01(1.0f - normalizedDistance);
                if (float.IsFinite(colorIntensity))
                {
                    vertexColors[vertexIndex] = Color.Lerp(Color.white, perforation.perforationColor, colorIntensity);
                }
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
            
            // Debug.Log 제거 - 불필요한 반복 로그
        }
    }
    
    public void RemoveAllPerforations()
    {
        perforations.Clear();
        isDirty = true;
        
        // Debug.Log 제거 - 불필요한 반복 로그
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
        // Safety check for division by zero
        if (originalVertices == null || originalVertices.Length == 0) return 0f;
        
        HashSet<int> uniqueAffectedVertices = new HashSet<int>();
        
        // Safety check for perforations list
        if (perforations != null)
        {
            foreach (var perforation in perforations)
            {
                if (perforation?.affectedVertices != null)
                {
                    foreach (int vertexIndex in perforation.affectedVertices)
                    {
                        // Only add valid vertex indices
                        if (vertexIndex >= 0 && vertexIndex < originalVertices.Length)
                        {
                            uniqueAffectedVertices.Add(vertexIndex);
                        }
                    }
                }
            }
        }
        
        float ratio = (float)uniqueAffectedVertices.Count / originalVertices.Length;
        
        // Safety check for final ratio
        return float.IsFinite(ratio) ? Mathf.Clamp01(ratio) : 0f;
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
            
            // Draw ID label (commented out for runtime compatibility)
            #if UNITY_EDITOR
            if (Application.isEditor)
            {
                UnityEditor.Handles.Label(worldPos, $"P{perforation.perforationID}\n{perforation.acousticLoss:F1}dB");
            }
            #endif
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