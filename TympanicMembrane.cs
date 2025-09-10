using UnityEngine;
using System.Collections.Generic;
using System.Linq;

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
    
    // Private variables
    private Mesh originalMesh;
    private Vector3[] originalVertices;
    private Vector3[] currentVertices;
    private Vector3[] velocities;
    private float[] vertexMasses;
    
    // Current state
    private float currentVibration = 0f;
    private float inputFrequency = 440f;
    private float inputAmplitude = 0f;
    private Vector3 centerPoint;
    
    // Perforation data
    private List<int> perforatedVertices = new List<int>();
    private bool hasPerforation = false;
    
    void Start()
    {
        InitializeMembrane();
        SetupSoftBodyPhysics();
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
            
        // Store original mesh data
        originalMesh = meshFilter.mesh;
        originalVertices = originalMesh.vertices;
        currentVertices = new Vector3[originalVertices.Length];
        velocities = new Vector3[originalVertices.Length];
        vertexMasses = new float[originalVertices.Length];
        
        // Copy original vertices
        System.Array.Copy(originalVertices, currentVertices, originalVertices.Length);
        
        // Initialize vertex masses and find center
        centerPoint = Vector3.zero;
        for (int i = 0; i < originalVertices.Length; i++)
        {
            vertexMasses[i] = physics.mass / originalVertices.Length;
            centerPoint += originalVertices[i];
        }
        centerPoint /= originalVertices.Length;
        
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
        rb.drag = physics.damping;
        rb.angularDrag = physics.damping;
        rb.useGravity = false;
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        
        // Setup collider for interaction
        if (membraneCollider != null)
        {
            membraneCollider.isTrigger = false;
        }
    }
    
    void Update()
    {
        if (Application.isPlaying)
        {
            UpdateSoftBodyPhysics();
            ApplyMeshDeformation();
        }
    }
    
    void UpdateSoftBodyPhysics()
    {
        float deltaTime = Time.deltaTime;
        
        for (int i = 0; i < currentVertices.Length; i++)
        {
            // Skip perforated vertices
            if (perforatedVertices.Contains(i))
                continue;
                
            Vector3 vertex = currentVertices[i];
            Vector3 originalPos = originalVertices[i];
            
            // Calculate distance from center for radial effects
            float distanceFromCenter = Vector3.Distance(originalPos, centerPoint);
            float normalizedDistance = distanceFromCenter / GetMaxDistanceFromCenter();
            
            // Calculate vibration based on current input
            float vibrationAmount = CalculateVibrationAtVertex(i, normalizedDistance);
            
            // Apply spring forces (restoring force)
            Vector3 springForce = -physics.stiffness * (vertex - originalPos);
            
            // Apply damping
            Vector3 dampingForce = -physics.damping * velocities[i];
            
            // Total force
            Vector3 totalForce = springForce + dampingForce;
            
            // Add vibration displacement
            Vector3 vibrationDisplacement = transform.forward * vibrationAmount;
            totalForce += vibrationDisplacement * physics.tension;
            
            // Apply Newton's second law: F = ma
            Vector3 acceleration = totalForce / vertexMasses[i];
            
            // Update velocity and position
            velocities[i] += acceleration * deltaTime;
            currentVertices[i] += velocities[i] * deltaTime;
            
            // Clamp maximum displacement
            Vector3 displacement = currentVertices[i] - originalPos;
            if (displacement.magnitude > physics.maxDisplacement)
            {
                displacement = displacement.normalized * physics.maxDisplacement;
                currentVertices[i] = originalPos + displacement;
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
        // Create new mesh with deformed vertices
        Mesh deformedMesh = Instantiate(originalMesh);
        deformedMesh.vertices = currentVertices;
        deformedMesh.RecalculateNormals();
        deformedMesh.RecalculateBounds();
        
        // Apply to mesh filter
        meshFilter.mesh = deformedMesh;
        
        // Update current vibration level for other systems
        currentVibration = CalculateCurrentVibrationLevel();
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
}