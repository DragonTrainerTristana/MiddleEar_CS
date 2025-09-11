using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;

[System.Serializable]
public class MembranePhysics
{
    [Header("물리 속성")]
    [Range(0.0001f, 0.01f)] public float mass = 0.00001f;           // 실제 고막 질량 ~0.014mg
    [Range(0.1f, 2.0f)] public float tension = 0.5f;               // 막의 장력
    [Range(0.1f, 1.0f)] public float damping = 0.8f;               // 공기 저항
    [Range(0.1f, 1.0f)] public float stiffness = 0.3f;             // 탄성
    
    [Header("진동 응답")]
    public AnimationCurve frequencyResponse;                        // 주파수별 응답
    public float maxDisplacement = 0.001f;                         // 최대 변위 (1mm)
    
    [Header("모드별 진동")]
    [Range(0f, 1f)] public float mode1Weight = 1.0f;              // 첫 번째 모드 (피스톤)
    [Range(0f, 1f)] public float mode2Weight = 0.5f;              // 두 번째 모드 
    [Range(0f, 1f)] public float mode3Weight = 0.3f;              // 세 번째 모드
}

public class TympanicMembrane : MonoBehaviour
{
    [Header("Components")]
    public MembranePhysics physics;
    
    [Header("Mesh References")]
    public MeshFilter meshFilter;
    public MeshRenderer meshRenderer;
    public Collider membraneCollider;
    
    [Header("Test Controls (Runtime)")]
    [Range(0f, 1f)] public float testAmplitude = 0.05f;
    [Range(20f, 2000f)] public float testFrequency = 440f;
    public bool enableTestMode = true;
    
    [Header("🔥 고막 천공 시뮬레이션 (Perforation Simulation)")]
    [Tooltip("고막 천공 여부 - 체크하면 구멍이 뚫림")]
    public bool enablePerforation = false;
    
    [Tooltip("천공 크기 (0~1) - 클수록 큰 구멍")]
    [Range(0f, 1f)] public float perforationSize = 0.1f;
    
    [Tooltip("천공 위치 X (-1~1)")]
    [Range(-1f, 1f)] public float perforationX = 0f;
    
    [Tooltip("천공 위치 Y (-1~1)")]
    [Range(-1f, 1f)] public float perforationY = 0f;
    
    [Tooltip("천공으로 인한 전달 손실 (%) - 높을수록 소리 전달 저하")]
    [Range(0f, 80f)] public float perforationHearingLoss = 30f;
    
    // Private variables
    private Mesh originalMesh;
    private Mesh currentMesh; // 실시간 변형용 메쉬
    private Vector3[] originalVertices;
    private Vector3[] currentVertices;
    private Vector3[] velocities;
    private float[] vertexMasses;
    private int[] originalTriangles;
    private Vector2[] originalUVs;
    private Color[] vertexColors;
    
    // Current state
    private float currentVibration = 0f;
    private float inputFrequency = 440f;
    private float inputAmplitude = 0f;
    private Vector3 centerPoint;
    
    // Performance optimization
    private float lastUpdateTime = 0f;
    private const float UPDATE_INTERVAL = 0.016f; // 60 FPS limit
    private bool meshNeedsUpdate = false;
    
    // Perforation data
    private List<int> perforatedVertices = new List<int>();
    private bool hasPerforation = false;
    
    // Debug flags (한번만 출력)
    private bool hasLoggedInitialization = false;
    private bool hasLoggedMeshError = false;
    private bool hasLoggedComponentError = false;
    
    // Performance monitoring
    private float frameTimeAccumulator = 0f;
    private int frameCount = 0;
    private float lastPerformanceLog = 0f;
    
    void Start()
    {
        InitializeMembrane();
        SetupSoftBodyPhysics();
        
        // 테스트용 - 자동으로 진동 시작 (디버그 모드)
        if (Application.isPlaying && inputAmplitude == 0f)
        {
            StartTestVibration();
        }
    }
    
    void StartTestVibration()
    {
        // 기본 테스트 진동 (440Hz, 작은 진폭)
        inputFrequency = 440f;
        inputAmplitude = 0.05f; // 작은 진동으로 시작
        
        if (!hasLoggedInitialization)
        {
            Debug.Log("TympanicMembrane: Starting test vibration (440Hz, 5% amplitude)");
            hasLoggedInitialization = true;
        }
    }
    
    void InitializeMembrane()
    {
        // Get mesh components
        if (meshFilter == null)
            meshFilter = GetComponent<MeshFilter>();
        if (meshRenderer == null)
            meshRenderer = GetComponent<MeshRenderer>();
        if (membraneCollider == null)
            membraneCollider = GetComponent<Collider>();
            
        // Store original mesh data with safety checks
        originalMesh = meshFilter.sharedMesh; // Use sharedMesh for read-only access
        if (originalMesh == null)
        {
            if (!hasLoggedMeshError)
            {
                Debug.LogError("TympanicMembrane: No mesh found in MeshFilter!");
                hasLoggedMeshError = true;
            }
            enabled = false;
            return;
        }
        
        // Try to access mesh data with error handling
        try
        {
            originalVertices = originalMesh.vertices;
            originalTriangles = originalMesh.triangles;
            originalUVs = originalMesh.uv;
            
            currentVertices = new Vector3[originalVertices.Length];
            velocities = new Vector3[originalVertices.Length];
            vertexMasses = new float[originalVertices.Length];
        }
        catch (System.Exception e)
        {
            if (!hasLoggedMeshError)
            {
                Debug.LogError($"TympanicMembrane: Cannot access mesh data. Please enable 'Read/Write' in mesh import settings for '{originalMesh.name}'. Error: {e.Message}");
                hasLoggedMeshError = true;
            }
            
            // Create fallback simple mesh
            CreateFallbackMesh();
            return;
        }
        
        // Copy original vertices
        System.Array.Copy(originalVertices, currentVertices, originalVertices.Length);
        
        // Initialize vertex masses and find center with safety checks
        centerPoint = Vector3.zero;
        for (int i = 0; i < originalVertices.Length; i++)
        {
            // Ensure valid vertex position
            if (!IsValidVector3(originalVertices[i]))
            {
                Debug.LogWarning($"TympanicMembrane: Invalid vertex at index {i}, using fallback");
                originalVertices[i] = Vector3.zero;
            }
            
            vertexMasses[i] = Mathf.Max(physics.mass / originalVertices.Length, 0.0001f);
            centerPoint += originalVertices[i];
        }
        
        if (originalVertices.Length > 0)
        {
            centerPoint /= originalVertices.Length;
        }
        
        // Validate center point
        if (!IsValidVector3(centerPoint))
        {
            centerPoint = Vector3.zero;
            Debug.LogWarning("TympanicMembrane: Invalid center point calculated, using origin");
        }
        
        // Initialize frequency response curve if empty
        if (physics.frequencyResponse == null || physics.frequencyResponse.keys.Length == 0)
        {
            CreateDefaultFrequencyResponse();
        }
    }
    
    void CreateDefaultFrequencyResponse()
    {
        Keyframe[] keys = new Keyframe[]
        {
            new Keyframe(20f, 0.1f),      // 저주파
            new Keyframe(100f, 0.3f),     
            new Keyframe(500f, 1.0f),     // 최적 주파수
            new Keyframe(1000f, 0.8f),    
            new Keyframe(2000f, 0.6f),    
            new Keyframe(4000f, 0.4f),    
            new Keyframe(8000f, 0.2f),    // 고주파
            new Keyframe(20000f, 0.1f)
        };
        
        physics.frequencyResponse = new AnimationCurve(keys);
    }
    
    void CreateFallbackMesh()
    {
        // Create a simple quad mesh as fallback
        originalVertices = new Vector3[]
        {
            new Vector3(-0.005f, -0.005f, 0f), // Bottom left
            new Vector3(0.005f, -0.005f, 0f),  // Bottom right  
            new Vector3(-0.005f, 0.005f, 0f),  // Top left
            new Vector3(0.005f, 0.005f, 0f)    // Top right
        };
        
        originalTriangles = new int[] { 0, 2, 1, 1, 2, 3 }; // Two triangles
        originalUVs = new Vector2[]
        {
            new Vector2(0, 0), new Vector2(1, 0),
            new Vector2(0, 1), new Vector2(1, 1)
        };
        
        currentVertices = new Vector3[originalVertices.Length];
        velocities = new Vector3[originalVertices.Length];
        vertexMasses = new float[originalVertices.Length];
        
        System.Array.Copy(originalVertices, currentVertices, originalVertices.Length);
        
        if (!hasLoggedMeshError)
        {
            Debug.Log("TympanicMembrane: Using fallback quad mesh (1cm x 1cm)");
            hasLoggedMeshError = true;
        }
    }
    
    void SetupSoftBodyPhysics()
    {
        // Add Rigidbody if not present
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }
        
        // Configure rigidbody
        rb.mass = physics.mass;
        rb.linearDamping = physics.damping;
        rb.angularDamping = physics.damping;
        rb.useGravity = false;
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        
        // Fix Mesh Collider issue
        SetupCollider(rb);
    }
    
    void SetupCollider(Rigidbody rb)
    {
        // MeshCollider는 반드시 필요! 올바른 방법으로 설정
        MeshCollider meshCol = GetComponent<MeshCollider>();
        if (meshCol == null)
        {
            meshCol = gameObject.AddComponent<MeshCollider>();
        }
        
        // 핵심: Kinematic Rigidbody + MeshCollider 조합
        rb.isKinematic = true; // 이게 핵심! 물리 시뮬레이션은 스크립트로
        
        // MeshCollider 설정
        meshCol.sharedMesh = originalMesh;
        meshCol.convex = false; // Kinematic이므로 concave 가능!
        meshCol.isTrigger = false;
        
        membraneCollider = meshCol;
        
        if (!hasLoggedComponentError)
        {
            Debug.Log("TympanicMembrane: Setup MeshCollider with Kinematic Rigidbody (proper solution)");
            hasLoggedComponentError = true;
        }
    }
    
    void Update()
    {
        if (!Application.isPlaying) return;
        
        // Performance monitoring
        float frameStartTime = Time.realtimeSinceStartup;
        
        // Limit update frequency to improve performance
        float currentTime = Time.time;
        if (currentTime - lastUpdateTime < UPDATE_INTERVAL) return;
        
        lastUpdateTime = currentTime;
        
        // Update input from test controls if enabled
        if (enableTestMode)
        {
            inputAmplitude = testAmplitude;
            inputFrequency = testFrequency;
        }
        
        // 천공 상태 업데이트
        UpdatePerforationState();
        
        // Only update if there's meaningful input
        if (inputAmplitude > 0.001f)
        {
            UpdateSoftBodyPhysics();
            meshNeedsUpdate = true;
        }
        
        // Apply mesh changes only when needed
        if (meshNeedsUpdate)
        {
            ApplyMeshDeformation();
            meshNeedsUpdate = false;
        }
        
        // Update performance stats
        UpdatePerformanceStats(frameStartTime);
    }
    
    void UpdatePerformanceStats(float frameStartTime)
    {
        float frameTime = (Time.realtimeSinceStartup - frameStartTime) * 1000f; // ms
        frameTimeAccumulator += frameTime;
        frameCount++;
        
        // Log every 2 seconds
        if (Time.time - lastPerformanceLog > 2f)
        {
            float avgFrameTime = frameTimeAccumulator / frameCount;
            if (avgFrameTime > 5f) // Only log if performance is concerning
            {
                Debug.Log($"TympanicMembrane performance: {avgFrameTime:F2}ms avg ({1000f/avgFrameTime:F0} FPS), {originalVertices?.Length ?? 0} vertices");
            }
            
            frameTimeAccumulator = 0f;
            frameCount = 0;
            lastPerformanceLog = Time.time;
        }
    }
    
    void UpdateSoftBodyPhysics()
    {
        float deltaTime = UPDATE_INTERVAL; // Use fixed timestep for stability
        float maxDistanceFromCenter = GetMaxDistanceFromCenter(); // Cache this value
        
        // Safety check for division by zero
        if (maxDistanceFromCenter <= 0.0001f)
        {
            maxDistanceFromCenter = 0.01f; // Default 1cm
        }
        
        Vector3 forwardDir = transform.forward; // Cache transform access
        
        // Pre-calculate common values with safety checks
        float stiffnessValue = Mathf.Clamp(physics.stiffness, 0.001f, 100f);
        float dampingValue = Mathf.Clamp(physics.damping, 0.001f, 10f);
        float tensionValue = Mathf.Clamp(physics.tension, 0.001f, 10f);
        float maxDisplacementSqr = physics.maxDisplacement * physics.maxDisplacement;
        
        // Process vertices in batches to reduce overhead
        int vertexCount = currentVertices.Length;
        for (int i = 0; i < vertexCount; i++)
        {
            // Skip perforated vertices (use HashSet for O(1) lookup)
            if (hasPerforation && perforatedVertices.Contains(i))
                continue;
                
            Vector3 vertex = currentVertices[i];
            Vector3 originalPos = originalVertices[i];
            
            // Fast distance calculation using sqrMagnitude
            Vector3 centerOffset = originalPos - centerPoint;
            float distanceFromCenter = centerOffset.magnitude;
            float normalizedDistance = distanceFromCenter / maxDistanceFromCenter;
            
            // Simplified vibration calculation
            float vibrationAmount = CalculateVibrationAtVertex(i, normalizedDistance);
            
            // Optimized force calculation
            Vector3 displacement = vertex - originalPos;
            Vector3 springForce = -stiffnessValue * displacement;
            Vector3 dampingForce = -dampingValue * velocities[i];
            Vector3 vibrationForce = forwardDir * (vibrationAmount * tensionValue);
            
            Vector3 totalForce = springForce + dampingForce + vibrationForce;
            
            // Update with fixed mass (avoid division) with safety checks
            float mass = Mathf.Max(vertexMasses[i], 0.0001f); // Prevent division by zero
            Vector3 acceleration = totalForce / mass;
            
            // Safety check for NaN/Infinity
            if (!IsValidVector3(acceleration))
            {
                acceleration = Vector3.zero;
            }
            
            // Semi-implicit Euler (more stable)
            velocities[i] += acceleration * deltaTime;
            
            // Clamp velocity to prevent runaway values
            velocities[i] = ClampVector3(velocities[i], -10f, 10f);
            
            Vector3 newPosition = vertex + velocities[i] * deltaTime;
            
            // Safety check for final position
            if (!IsValidVector3(newPosition))
            {
                newPosition = originalPos; // Reset to original if invalid
                velocities[i] = Vector3.zero;
            }
            
            // Fast displacement limiting using sqrMagnitude
            Vector3 finalDisplacement = newPosition - originalPos;
            float displacementMagnitude = finalDisplacement.sqrMagnitude;
            
            if (displacementMagnitude > maxDisplacementSqr && displacementMagnitude > 0f)
            {
                finalDisplacement = finalDisplacement.normalized * physics.maxDisplacement;
                newPosition = originalPos + finalDisplacement;
            }
            
            // Final safety check
            if (IsValidVector3(newPosition))
            {
                currentVertices[i] = newPosition;
            }
            else
            {
                currentVertices[i] = originalPos; // Fallback to original position
            }
        }
    }
    
    float CalculateVibrationAtVertex(int vertexIndex, float normalizedDistance)
    {
        if (inputAmplitude <= 0f) return 0f;
        
        // Get frequency response
        float frequencyMultiplier = physics.frequencyResponse.Evaluate(inputFrequency);
        
        // Calculate different vibration modes
        float mode1 = CalculateMode1Vibration(normalizedDistance); // 피스톤 모드
        float mode2 = CalculateMode2Vibration(normalizedDistance); // 첫 번째 고유모드
        float mode3 = CalculateMode3Vibration(normalizedDistance); // 두 번째 고유모드
        
        // Combine modes based on frequency
        float combinedMode = 0f;
        
        if (inputFrequency < 200f)
        {
            // 저주파: 주로 피스톤 모드
            combinedMode = mode1 * physics.mode1Weight;
        }
        else if (inputFrequency < 2000f)
        {
            // 중주파: 혼합 모드
            combinedMode = mode1 * physics.mode1Weight * 0.7f + 
                          mode2 * physics.mode2Weight * 0.3f;
        }
        else
        {
            // 고주파: 복잡한 모드들
            combinedMode = mode1 * physics.mode1Weight * 0.3f + 
                          mode2 * physics.mode2Weight * 0.4f + 
                          mode3 * physics.mode3Weight * 0.3f;
        }
        
        // Apply time-based oscillation
        float timeOscillation = Mathf.Sin(Time.time * inputFrequency * 2 * Mathf.PI);
        
        return inputAmplitude * frequencyMultiplier * combinedMode * timeOscillation;
    }
    
    float CalculateMode1Vibration(float normalizedDistance)
    {
        // 피스톤 모드: 전체 막이 균일하게 움직임
        return 1.0f;
    }
    
    float CalculateMode2Vibration(float normalizedDistance)
    {
        // 첫 번째 고유모드: 중앙은 크게, 가장자리는 작게
        return 1.0f - normalizedDistance * 0.8f;
    }
    
    float CalculateMode3Vibration(float normalizedDistance)
    {
        // 두 번째 고유모드: 복잡한 패턴
        return Mathf.Sin(normalizedDistance * Mathf.PI * 2) * (1.0f - normalizedDistance);
    }
    
    void ApplyMeshDeformation()
    {
        // Create mesh once and reuse
        if (currentMesh == null)
        {
            currentMesh = new Mesh();
            currentMesh.name = "TympanicMembrane_Deformed";
            currentMesh.MarkDynamic(); // Important for performance!
            
            // Set initial data
            currentMesh.vertices = currentVertices;
            currentMesh.triangles = originalTriangles;
            
            if (originalUVs != null && originalUVs.Length > 0)
                currentMesh.uv = originalUVs;
                
            meshFilter.mesh = currentMesh;
        }
        
        // Only update vertices (fastest operation)
        currentMesh.vertices = currentVertices;
        
        // Only update colors if they exist and changed
        if (vertexColors != null && vertexColors.Length > 0)
        {
            currentMesh.colors = vertexColors;
        }
        
        // Recalculate only when necessary
        currentMesh.RecalculateBounds(); // Faster than RecalculateNormals
        
        // Update MeshCollider with deformed mesh (중요!)
        UpdateMeshCollider();
        
        // Update current vibration level for other systems
        currentVibration = CalculateCurrentVibrationLevel();
    }
    
    void UpdateMeshCollider()
    {
        // MeshCollider 업데이트는 성능상 부담이 크므로 필요할 때만
        MeshCollider meshCol = membraneCollider as MeshCollider;
        if (meshCol != null && currentMesh != null)
        {
            // Validate mesh before applying to collider
            if (ValidateMesh(currentMesh))
            {
                try
                {
                    meshCol.sharedMesh = null; // 먼저 클리어
                    meshCol.sharedMesh = currentMesh; // 새 메쉬 적용
                }
                catch (System.Exception e)
                {
                    if (!hasLoggedMeshError)
                    {
                        Debug.LogWarning($"TympanicMembrane: Failed to update MeshCollider: {e.Message}");
                        hasLoggedMeshError = true;
                    }
                    // Fallback to original mesh
                    meshCol.sharedMesh = originalMesh;
                }
            }
            else
            {
                // Use original mesh if deformed mesh is invalid
                meshCol.sharedMesh = originalMesh;
            }
        }
    }
    
    bool ValidateMesh(Mesh mesh)
    {
        if (mesh == null) return false;
        
        Vector3[] vertices = mesh.vertices;
        if (vertices == null || vertices.Length == 0) return false;
        
        // Check for NaN/Infinity in vertices
        for (int i = 0; i < vertices.Length; i++)
        {
            if (!IsValidVector3(vertices[i]))
            {
                return false;
            }
        }
        
        return true;
    }
    
    float CalculateCurrentVibrationLevel()
    {
        float totalDisplacement = 0f;
        
        for (int i = 0; i < currentVertices.Length; i++)
        {
            Vector3 displacement = currentVertices[i] - originalVertices[i];
            totalDisplacement += displacement.magnitude;
        }
        
        return totalDisplacement / currentVertices.Length;
    }
    
    float GetMaxDistanceFromCenter()
    {
        float maxDistance = 0f;
        for (int i = 0; i < originalVertices.Length; i++)
        {
            float distance = Vector3.Distance(originalVertices[i], centerPoint);
            if (distance > maxDistance)
                maxDistance = distance;
        }
        return maxDistance;
    }
    
    // Safety check functions for NaN/Infinity prevention
    bool IsValidVector3(Vector3 v)
    {
        return !float.IsNaN(v.x) && !float.IsNaN(v.y) && !float.IsNaN(v.z) &&
               !float.IsInfinity(v.x) && !float.IsInfinity(v.y) && !float.IsInfinity(v.z);
    }
    
    Vector3 ClampVector3(Vector3 v, float min, float max)
    {
        return new Vector3(
            Mathf.Clamp(v.x, min, max),
            Mathf.Clamp(v.y, min, max),
            Mathf.Clamp(v.z, min, max)
        );
    }
    
    // Public methods for external control
    public void ApplyVibration(float amplitude, float frequency)
    {
        inputAmplitude = amplitude;
        inputFrequency = frequency;
    }
    
    public float GetCurrentVibration()
    {
        return currentVibration;
    }
    
    public void SetPerforation(List<int> perforatedVertexIndices)
    {
        perforatedVertices = perforatedVertexIndices;
        hasPerforation = perforatedVertices.Count > 0;
        
        // Modify physics based on perforation
        if (hasPerforation)
        {
            float perforationRatio = (float)perforatedVertices.Count / originalVertices.Length;
            physics.tension *= (1.0f - perforationRatio * 0.5f); // 장력 감소
            physics.damping *= (1.0f + perforationRatio * 0.3f);  // 댐핑 증가
        }
    }
    
    public Vector3[] GetCurrentVertices()
    {
        return currentVertices;
    }
    
    public Vector3[] GetOriginalVertices()
    {
        return originalVertices;
    }
    
    void OnDrawGizmosSelected()
    {
        if (originalVertices != null)
        {
            // Draw center point
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.TransformPoint(centerPoint), 0.001f);
            
            // Draw vibration visualization
            if (Application.isPlaying)
            {
                Gizmos.color = Color.green;
                for (int i = 0; i < currentVertices.Length; i++)
                {
                    Vector3 worldPos = transform.TransformPoint(currentVertices[i]);
                    Vector3 displacement = currentVertices[i] - originalVertices[i];
                    
                    if (displacement.magnitude > 0.0001f)
                    {
                        Gizmos.DrawLine(worldPos, worldPos + displacement * 100f);
                    }
                }
            }
        }
    }
    
    /*
     * ====================================================================
     * 🔥 고막 천공 시뮬레이션 (Perforation Simulation)
     * ====================================================================
     */
    
    /// <summary>
    /// 🔥 천공 상태 실시간 업데이트
    /// </summary>
    void UpdatePerforationState()
    {
        if (enablePerforation != hasPerforation)
        {
            if (enablePerforation)
                CreatePerforation();
            else
                HealPerforation();
        }
        
        if (enablePerforation)
        {
            UpdatePerforationVisualization();
        }
    }
    
    /// <summary>
    /// 🕳️ 고막에 구멍 생성
    /// </summary>
    void CreatePerforation()
    {
        if (currentMesh == null || currentVertices == null) return;
        
        hasPerforation = true;
        perforatedVertices.Clear();
        
        // 천공 위치 계산 (중심점 기준 상대 위치)
        Vector3 perforationCenter = centerPoint + new Vector3(perforationX * 0.01f, perforationY * 0.01f, 0);
        float perforationRadius = perforationSize * 0.005f; // 천공 반지름
        
        // 천공 영역 내의 정점들 찾기
        for (int i = 0; i < currentVertices.Length; i++)
        {
            Vector3 localVertex = transform.TransformPoint(currentVertices[i]);
            float distance = Vector3.Distance(localVertex, perforationCenter);
            
            if (distance <= perforationRadius)
            {
                perforatedVertices.Add(i);
                // 구멍 부분 정점을 안쪽으로 밀어넣기
                currentVertices[i] = Vector3.Lerp(currentVertices[i], centerPoint + Vector3.back * 0.002f, 0.8f);
            }
        }
        
        // 색상 변경으로 천공 부위 표시
        UpdatePerforationColors();
        
        Debug.Log($"🔥 고막 천공 생성: {perforatedVertices.Count}개 정점 영향, 크기 {perforationSize:F1}, 예상 청력 손실 {perforationHearingLoss:F0}%");
    }
    
    /// <summary>
    /// 💊 천공 치료 (구멍 메우기)
    /// </summary>
    void HealPerforation()
    {
        if (!hasPerforation) return;
        
        hasPerforation = false;
        
        // 원래 정점 위치로 복원
        if (originalVertices != null && currentVertices != null)
        {
            foreach (int vertexIndex in perforatedVertices)
            {
                if (vertexIndex < currentVertices.Length && vertexIndex < originalVertices.Length)
                {
                    currentVertices[vertexIndex] = originalVertices[vertexIndex];
                }
            }
        }
        
        // 정상 색상으로 복원
        RestoreNormalColors();
        
        perforatedVertices.Clear();
        meshNeedsUpdate = true;
        
        Debug.Log("💊 고막 천공 치료 완료 - 정상 상태로 복원됨");
    }
    
    /// <summary>
    /// 🎨 천공 시각화 업데이트
    /// </summary>
    void UpdatePerforationVisualization()
    {
        if (!hasPerforation || vertexColors == null) return;
        
        // 천공 부위를 빨간색으로 표시
        for (int i = 0; i < vertexColors.Length; i++)
        {
            if (perforatedVertices.Contains(i))
            {
                // 천공된 부위: 진한 빨간색
                vertexColors[i] = Color.Lerp(Color.red, Color.black, 0.5f);
            }
            else
            {
                // 정상 부위: 연한 분홍색 (염증 표현)
                float distanceToNearestHole = GetDistanceToNearestPerforation(i);
                float inflammationLevel = Mathf.Clamp01(1f - distanceToNearestHole / 0.01f);
                vertexColors[i] = Color.Lerp(new Color(1f, 0.9f, 0.9f, 1f), Color.red, inflammationLevel * 0.3f);
            }
        }
        
        if (currentMesh != null)
        {
            currentMesh.colors = vertexColors;
        }
    }
    
    /// <summary>
    /// 🎨 정상 색상 복원
    /// </summary>
    void RestoreNormalColors()
    {
        if (vertexColors == null) return;
        
        // 모든 정점을 정상 색상(연한 회색)으로 복원
        for (int i = 0; i < vertexColors.Length; i++)
        {
            vertexColors[i] = new Color(0.9f, 0.9f, 0.9f, 1f);
        }
        
        if (currentMesh != null)
        {
            currentMesh.colors = vertexColors;
        }
    }
    
    /// <summary>
    /// 천공 색상 업데이트
    /// </summary>
    void UpdatePerforationColors()
    {
        if (currentMesh == null) return;
        
        if (vertexColors == null || vertexColors.Length != currentVertices.Length)
        {
            vertexColors = new Color[currentVertices.Length];
            // 기본 색상으로 초기화
            for (int i = 0; i < vertexColors.Length; i++)
            {
                vertexColors[i] = new Color(0.9f, 0.9f, 0.9f, 1f);
            }
        }
    }
    
    /// <summary>
    /// 가장 가까운 천공까지의 거리 계산
    /// </summary>
    float GetDistanceToNearestPerforation(int vertexIndex)
    {
        if (perforatedVertices.Count == 0 || vertexIndex >= currentVertices.Length)
            return float.MaxValue;
        
        float minDistance = float.MaxValue;
        Vector3 currentVertex = currentVertices[vertexIndex];
        
        foreach (int perforatedIndex in perforatedVertices)
        {
            if (perforatedIndex < currentVertices.Length)
            {
                float distance = Vector3.Distance(currentVertex, currentVertices[perforatedIndex]);
                minDistance = Mathf.Min(minDistance, distance);
            }
        }
        
        return minDistance;
    }
    
    /// <summary>
    /// 🎵 천공이 소리 전달에 미치는 영향 계산
    /// </summary>
    public float GetPerforationTransmissionLoss()
    {
        if (!hasPerforation) return 1.0f; // 손실 없음
        
        // 천공 크기에 따른 전달 손실
        float sizeLoss = perforationSize * (perforationHearingLoss / 100f);
        
        // 천공된 정점 수에 따른 추가 손실
        float vertexLoss = (perforatedVertices.Count / (float)currentVertices.Length) * 0.3f;
        
        // 총 전달 효율 (0~1)
        float totalLoss = Mathf.Clamp01(sizeLoss + vertexLoss);
        return 1.0f - totalLoss;
    }
    
    /// <summary>
    /// 🩺 천공 상태 정보 반환 (Public API)
    /// </summary>
    public bool HasPerforation()
    {
        return hasPerforation;
    }
    
    /// <summary>
    /// 📊 천공 크기 반환 (Public API)
    /// </summary>
    public float GetPerforationSeverity()
    {
        if (!hasPerforation) return 0f;
        return perforationSize;
    }
}