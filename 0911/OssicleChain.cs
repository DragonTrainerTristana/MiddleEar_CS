using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class OssicleData
{
    [Header("물리 속성")]
    public float mass;              // 질량 (mg)
    public float stiffness;         // 강성
    public float damping;           // 댐핑
    public Vector3 rotationAxis;    // 회전 축
    public float maxAngle;          // 최대 회전각 (degrees)
    
    [Header("실시간 상태")]
    [ReadOnly] public float currentAngle;
    [ReadOnly] public float angularVelocity;
    [ReadOnly] public Vector3 currentPosition;
}

[System.Serializable]
public class OssicleConnection
{
    public GameObject fromOssicle;
    public GameObject toOssicle;
    public Vector3 connectionPoint;
    public float transmissionRatio;     // 전달 비율
    public float stiffness;
    public SpringJoint springJoint;
}

public class OssicleChain : MonoBehaviour
{
    [Header("Ossicle GameObjects")]
    public GameObject malleus;      // 망치뼈 (추골)
    public GameObject incus;        // 모루뼈 (침골)
    public GameObject stapes;       // 등자뼈 (등골)
    
    [Header("Ossicle Data")]
    public OssicleData malleusData;
    public OssicleData incusData;
    public OssicleData stapesData;
    
    [Header("Connections")]
    public List<OssicleConnection> connections = new List<OssicleConnection>();
    
    [Header("Physics Settings")]
    [Range(0.1f, 2.0f)] public float overallStiffness = 1.0f;
    [Range(0.1f, 2.0f)] public float overallDamping = 1.0f;
    public bool enableRealtimePhysics = true;
    
    // Private variables
    private Rigidbody malleusRb, incusRb, stapesRb;
    private float inputVibration = 0f;
    private float outputVibration = 0f;
    private float currentFrequency = 440f;
    
    // Anatomical constants (실제 귀 데이터 기반)
    private const float MALLEUS_MASS = 23.0f;    // mg
    private const float INCUS_MASS = 27.0f;      // mg  
    private const float STAPES_MASS = 2.8f;      // mg
    
    private const float MALLEUS_LEVER_RATIO = 1.3f;
    private const float INCUS_LEVER_RATIO = 1.3f;
    private const float STAPES_AREA_RATIO = 17.0f; // 고막 vs 등자뼈 발판
    
    void Start()
    {
        InitializeOssicleData();
        SetupPhysics();
        CreateConnections();
    }
    
    public void InitializeChain(GameObject malleusObj, GameObject incusObj, GameObject stapesObj)
    {
        malleus = malleusObj;
        incus = incusObj;
        stapes = stapesObj;
        
        if (Application.isPlaying)
        {
            InitializeOssicleData();
            SetupPhysics();
            CreateConnections();
        }
    }
    
    void InitializeOssicleData()
    {
        // Malleus (망치뼈) 설정
        malleusData = new OssicleData
        {
            mass = MALLEUS_MASS / 1000f,    // kg으로 변환
            stiffness = 100f,
            damping = 10f,
            rotationAxis = Vector3.up,
            maxAngle = 20f
        };
        
        // Incus (모루뼈) 설정
        incusData = new OssicleData
        {
            mass = INCUS_MASS / 1000f,
            stiffness = 120f,
            damping = 12f,
            rotationAxis = Vector3.up,
            maxAngle = 15f
        };
        
        // Stapes (등자뼈) 설정
        stapesData = new OssicleData
        {
            mass = STAPES_MASS / 1000f,
            stiffness = 150f,
            damping = 15f,
            rotationAxis = Vector3.forward,
            maxAngle = 10f
        };
    }
    
    void SetupPhysics()
    {
        // Malleus Rigidbody 설정
        if (malleus != null)
        {
            malleusRb = malleus.GetComponent<Rigidbody>();
            if (malleusRb == null) malleusRb = malleus.AddComponent<Rigidbody>();
            
            malleusRb.mass = malleusData.mass;
            malleusRb.linearDamping = malleusData.damping * overallDamping;
            malleusRb.angularDamping = malleusData.damping * overallDamping;
            malleusRb.useGravity = false;
            
            // 특정 축만 회전하도록 제한
            malleusRb.constraints = RigidbodyConstraints.FreezePositionX | 
                                   RigidbodyConstraints.FreezePositionY |
                                   RigidbodyConstraints.FreezePositionZ |
                                   RigidbodyConstraints.FreezeRotationX |
                                   RigidbodyConstraints.FreezeRotationZ;
        }
        
        // Incus Rigidbody 설정
        if (incus != null)
        {
            incusRb = incus.GetComponent<Rigidbody>();
            if (incusRb == null) incusRb = incus.AddComponent<Rigidbody>();
            
            incusRb.mass = incusData.mass;
            incusRb.linearDamping = incusData.damping * overallDamping;
            incusRb.angularDamping = incusData.damping * overallDamping;
            incusRb.useGravity = false;
            
            incusRb.constraints = RigidbodyConstraints.FreezePositionX | 
                                 RigidbodyConstraints.FreezePositionY |
                                 RigidbodyConstraints.FreezePositionZ |
                                 RigidbodyConstraints.FreezeRotationX |
                                 RigidbodyConstraints.FreezeRotationZ;
        }
        
        // Stapes Rigidbody 설정  
        if (stapes != null)
        {
            stapesRb = stapes.GetComponent<Rigidbody>();
            if (stapesRb == null) stapesRb = stapes.AddComponent<Rigidbody>();
            
            stapesRb.mass = stapesData.mass;
            stapesRb.linearDamping = stapesData.damping * overallDamping;
            stapesRb.angularDamping = stapesData.damping * overallDamping;
            stapesRb.useGravity = false;
            
            stapesRb.constraints = RigidbodyConstraints.FreezePositionX | 
                                  RigidbodyConstraints.FreezePositionY |
                                  RigidbodyConstraints.FreezeRotationX |
                                  RigidbodyConstraints.FreezeRotationY |
                                  RigidbodyConstraints.FreezeRotationZ;
        }
    }
    
    void CreateConnections()
    {
        connections.Clear();
        
        // Malleus-Incus 연결
        if (malleus != null && incus != null)
        {
            OssicleConnection malleusIncus = new OssicleConnection
            {
                fromOssicle = malleus,
                toOssicle = incus,
                transmissionRatio = MALLEUS_LEVER_RATIO,
                stiffness = 200f * overallStiffness
            };
            
            // Spring Joint 추가
            malleusIncus.springJoint = malleus.AddComponent<SpringJoint>();
            malleusIncus.springJoint.connectedBody = incusRb;
            malleusIncus.springJoint.spring = malleusIncus.stiffness;
            malleusIncus.springJoint.damper = malleusData.damping;
            
            connections.Add(malleusIncus);
        }
        
        // Incus-Stapes 연결
        if (incus != null && stapes != null)
        {
            OssicleConnection incusStapes = new OssicleConnection
            {
                fromOssicle = incus,
                toOssicle = stapes,
                transmissionRatio = INCUS_LEVER_RATIO,
                stiffness = 250f * overallStiffness
            };
            
            incusStapes.springJoint = incus.AddComponent<SpringJoint>();
            incusStapes.springJoint.connectedBody = stapesRb;
            incusStapes.springJoint.spring = incusStapes.stiffness;
            incusStapes.springJoint.damper = incusData.damping;
            
            connections.Add(incusStapes);
        }
    }
    
    void Update()
    {
        if (!enableRealtimePhysics || !Application.isPlaying) return;
        
        // Limit physics updates to improve performance
        if (Time.frameCount % 2 == 0) // Every 2nd frame
        {
            UpdateOssiclePhysics();
            CalculateOutputVibration();
        }
    }
    
    void UpdateOssiclePhysics()
    {
        // Update current states with safety checks
        if (malleusRb != null && malleus != null)
        {
            Vector3 eulerAngles = malleus.transform.eulerAngles;
            malleusData.currentAngle = float.IsFinite(eulerAngles.y) ? eulerAngles.y : 0f;
            
            Vector3 angVel = malleusRb.angularVelocity;
            malleusData.angularVelocity = float.IsFinite(angVel.y) ? 
                Mathf.Clamp(angVel.y, -100f, 100f) : 0f;
            
            Vector3 pos = malleus.transform.position;
            malleusData.currentPosition = IsValidVector3(pos) ? pos : Vector3.zero;
        }
        
        if (incusRb != null && incus != null)
        {
            Vector3 eulerAngles = incus.transform.eulerAngles;
            incusData.currentAngle = float.IsFinite(eulerAngles.y) ? eulerAngles.y : 0f;
            
            Vector3 angVel = incusRb.angularVelocity;
            incusData.angularVelocity = float.IsFinite(angVel.y) ? 
                Mathf.Clamp(angVel.y, -100f, 100f) : 0f;
                
            Vector3 pos = incus.transform.position;
            incusData.currentPosition = IsValidVector3(pos) ? pos : Vector3.zero;
        }
        
        if (stapesRb != null && stapes != null)
        {
            Vector3 eulerAngles = stapes.transform.eulerAngles;
            stapesData.currentAngle = float.IsFinite(eulerAngles.z) ? eulerAngles.z : 0f;
            
            Vector3 angVel = stapesRb.angularVelocity;
            stapesData.angularVelocity = float.IsFinite(angVel.z) ? 
                Mathf.Clamp(angVel.z, -100f, 100f) : 0f;
                
            Vector3 pos = stapes.transform.position;
            stapesData.currentPosition = IsValidVector3(pos) ? pos : Vector3.zero;
        }
        
        // Apply angle limits
        ApplyAngleLimits();
    }
    
    // Safety check function for Vector3 validity
    bool IsValidVector3(Vector3 v)
    {
        return float.IsFinite(v.x) && float.IsFinite(v.y) && float.IsFinite(v.z);
    }
    
    void ApplyAngleLimits()
    {
        // Malleus angle limit
        if (malleus != null)
        {
            Vector3 eulerAngles = malleus.transform.eulerAngles;
            eulerAngles.y = Mathf.Clamp(eulerAngles.y, -malleusData.maxAngle, malleusData.maxAngle);
            malleus.transform.eulerAngles = eulerAngles;
        }
        
        // Incus angle limit
        if (incus != null)
        {
            Vector3 eulerAngles = incus.transform.eulerAngles;
            eulerAngles.y = Mathf.Clamp(eulerAngles.y, -incusData.maxAngle, incusData.maxAngle);
            incus.transform.eulerAngles = eulerAngles;
        }
        
        // Stapes angle limit
        if (stapes != null)
        {
            Vector3 eulerAngles = stapes.transform.eulerAngles;
            eulerAngles.z = Mathf.Clamp(eulerAngles.z, -stapesData.maxAngle, stapesData.maxAngle);
            stapes.transform.eulerAngles = eulerAngles;
        }
    }
    
    void CalculateOutputVibration()
    {
        // Safety checks for input
        if (!float.IsFinite(inputVibration) || inputVibration <= 0f || 
            !float.IsFinite(currentFrequency) || currentFrequency <= 0f)
        {
            outputVibration = 0f;
            return;
        }
        
        // 각 단계별 전달 계산 with safety checks
        float frequencyResponse = GetFrequencyResponse(currentFrequency);
        if (!float.IsFinite(frequencyResponse))
        {
            frequencyResponse = 1.0f;
        }
        
        float malleusOutput = inputVibration * frequencyResponse;
        if (!float.IsFinite(malleusOutput)) malleusOutput = 0f;
        
        float incusOutput = malleusOutput * MALLEUS_LEVER_RATIO;
        if (!float.IsFinite(incusOutput)) incusOutput = 0f;
        
        float stapesOutput = incusOutput * INCUS_LEVER_RATIO;
        if (!float.IsFinite(stapesOutput)) stapesOutput = 0f;
        
        // 면적비에 의한 압력 증폭 (17배)
        float finalOutput = stapesOutput * STAPES_AREA_RATIO;
        if (!float.IsFinite(finalOutput)) finalOutput = 0f;
        
        // 주파수별 감쇠 적용
        float frequencyAttenuation = CalculateFrequencyAttenuation(currentFrequency);
        if (!float.IsFinite(frequencyAttenuation))
        {
            frequencyAttenuation = 1.0f;
        }
        
        outputVibration = finalOutput * frequencyAttenuation;
        
        // Final safety check and physical limits
        if (!float.IsFinite(outputVibration))
        {
            outputVibration = 0f;
        }
        else
        {
            outputVibration = Mathf.Clamp(outputVibration, 0f, 10f);
        }
    }
    
    float GetFrequencyResponse(float frequency)
    {
        // Safety check for valid frequency
        if (!float.IsFinite(frequency) || frequency <= 0f) return 0f;
        
        // 중이의 주파수 응답 곡선 (500-4000Hz가 최적)
        if (frequency < 100f)
            return 0.1f;
        else if (frequency < 500f)
        {
            float t = Mathf.Clamp01((frequency - 100f) / 400f);
            return Mathf.Lerp(0.1f, 1.0f, t);
        }
        else if (frequency <= 4000f)
            return 1.0f;
        else if (frequency < 8000f)
        {
            float t = Mathf.Clamp01((frequency - 4000f) / 4000f);
            return Mathf.Lerp(1.0f, 0.5f, t);
        }
        else
            return 0.5f;
    }
    
    float CalculateFrequencyAttenuation(float frequency)
    {
        // Safety check for valid frequency
        if (!float.IsFinite(frequency) || frequency <= 0f) return 0f;
        
        // 실제 중이 전달 함수 근사
        float resonanceFreq = 1000f; // 공명 주파수
        float q = Mathf.Max(2.0f, 0.1f); // Q factor with minimum value
        
        float omega = 2 * Mathf.PI * frequency;
        float omega0 = 2 * Mathf.PI * resonanceFreq;
        
        // Safety check for division by zero
        if (omega0 <= 0f) return 1.0f;
        
        float omegaRatio = omega / omega0;
        
        // Safety check for valid ratio
        if (!float.IsFinite(omegaRatio)) return 1.0f;
        
        // 2차 시스템 응답 with safe calculations
        float term1 = 1 - Mathf.Pow(omegaRatio, 2);
        float term2 = omegaRatio / q;
        
        float denominator = Mathf.Pow(term1, 2) + Mathf.Pow(term2, 2);
        
        // Prevent sqrt of negative or zero values
        denominator = Mathf.Max(denominator, 0.0001f);
        
        float magnitude = 1.0f / Mathf.Sqrt(denominator);
        
        // Ensure result is finite and reasonable
        if (!float.IsFinite(magnitude))
            magnitude = 1.0f;
        else
            magnitude = Mathf.Clamp(magnitude, 0.001f, 100f);
        
        return magnitude;
    }
    
    // Public methods
    public void ReceiveVibration(float vibrationAmplitude, float frequency)
    {
        inputVibration = vibrationAmplitude;
        currentFrequency = frequency;
        
        // Apply vibration to malleus
        if (malleusRb != null && enableRealtimePhysics)
        {
            Vector3 force = malleusData.rotationAxis * vibrationAmplitude * malleusData.stiffness;
            malleusRb.AddTorque(force);
        }
    }
    
    public float GetOutputVibration()
    {
        return outputVibration;
    }
    
    public void SetPhysicsParameters(float stiffness, float damping)
    {
        overallStiffness = stiffness;
        overallDamping = damping;
        
        // Update all spring joints
        foreach (var connection in connections)
        {
            if (connection.springJoint != null)
            {
                connection.springJoint.spring = connection.stiffness * overallStiffness;
                connection.springJoint.damper = connection.springJoint.damper * overallDamping;
            }
        }
    }
    
    public OssicleData GetMalleusData() => malleusData;
    public OssicleData GetIncusData() => incusData;
    public OssicleData GetStapesData() => stapesData;
    
    void OnDrawGizmosSelected()
    {
        // Draw connections
        Gizmos.color = Color.yellow;
        foreach (var connection in connections)
        {
            if (connection.fromOssicle != null && connection.toOssicle != null)
            {
                Gizmos.DrawLine(
                    connection.fromOssicle.transform.position,
                    connection.toOssicle.transform.position
                );
                
                // Draw transmission ratio
                Vector3 midPoint = Vector3.Lerp(
                    connection.fromOssicle.transform.position,
                    connection.toOssicle.transform.position,
                    0.5f
                );
                
                Gizmos.DrawWireSphere(midPoint, 0.001f * connection.transmissionRatio);
            }
        }
        
        // Draw vibration visualization
        if (Application.isPlaying)
        {
            Gizmos.color = Color.green;
            if (malleus != null)
            {
                Vector3 vibDir = malleusData.rotationAxis * inputVibration * 0.01f;
                Gizmos.DrawLine(malleus.transform.position, malleus.transform.position + vibDir);
            }
        }
    }
}

// ReadOnly attribute for inspector
public class ReadOnlyAttribute : PropertyAttribute { }