using UnityEngine;

/// <summary>
/// 이소골 진동 시뮬레이션 (과학적 모델 기반)
///
/// 참고 논문:
/// - PMC6640663: Middle Ear Mechanics
/// - PMC1868690: Ossicular Motion
///
/// 물리적 파라미터:
/// - 레버비율 (Lever Ratio): 1.3 (malleus:incus 길이비)
/// - 면적비 (Area Ratio): 17:1 (고막:등골족판)
/// - 전체 압력 증폭: ~22배 (레버비 × 면적비)
/// - 이소골 공명 주파수: 700-1200 Hz
/// - 등골 최대 변위: ~0.1-1 μm (고막의 1/10~1/3)
/// </summary>
public class OssicleVibration : MonoBehaviour
{
    [Header("=== 물리적 파라미터 (논문 기반) ===")]
    [Tooltip("레버비율 - 실제: 1.3 (malleus/incus 길이비)")]
    [Range(1.0f, 2.0f)]
    public float leverRatio = 1.3f;

    [Tooltip("면적비 (고막/등골족판) - 실제: 14-21, 평균 17")]
    [Range(14f, 21f)]
    public float areaRatio = 17f;

    [Tooltip("이소골 공명 주파수 (Hz) - 실제: 700-1200Hz")]
    [Range(700f, 1200f)]
    public float resonanceFrequency = 900f;

    [Tooltip("Q 팩터 (공명 날카로움)")]
    [Range(1f, 5f)]
    public float qFactor = 2.5f;

    [Tooltip("등골 최대 변위 (μm) - 실제: 0.1-1μm")]
    [Range(0.1f, 2f)]
    public float maxStapesDisplacementMicron = 1f;

    [Header("=== 감쇠 파라미터 ===")]
    [Tooltip("관절 감쇠 계수 (malleoincudal joint)")]
    [Range(0.1f, 0.5f)]
    public float jointDamping = 0.2f;

    [Tooltip("인대 강성 계수")]
    [Range(0.5f, 2.0f)]
    public float ligamentStiffness = 1.0f;

    [Header("=== 연결 ===")]
    [Tooltip("TympanicMembrane 컴포넌트")]
    public TympanicMembrane tympanicMembrane;

    [Header("=== 상태 (읽기 전용) ===")]
    [SerializeField] private float inputVibration;
    [SerializeField] private float frequencyResponse;
    [SerializeField] private float currentDisplacementMicron;
    [SerializeField] private float pressureGain;
    [SerializeField] private float transmittedToStapes;

    // 진동 상태
    private float velocity = 0f;
    private float displacement = 0f;
    private float phase = 0f;
    private float currentFrequency = 1000f;

    // 원래 위치
    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private bool isInitialized = false;

    // 최대 변위 (미터)
    private float maxDisplacementMeters;

    void Start()
    {
        Initialize();
    }

    void Initialize()
    {
        // 현재 위치를 원래 위치로 저장
        originalPosition = transform.localPosition;
        originalRotation = transform.localRotation;

        Debug.Log($"[OssicleVibration] 원래 위치 저장: {originalPosition}");

        // TympanicMembrane 자동 검색
        if (tympanicMembrane == null)
            tympanicMembrane = FindObjectOfType<TympanicMembrane>();

        // 최대 변위를 미터로 변환
        maxDisplacementMeters = maxStapesDisplacementMicron * 1e-6f;

        // 압력 증폭 계산 (레버비 × 면적비)
        pressureGain = leverRatio * areaRatio;

        isInitialized = true;

        Debug.Log($"[OssicleVibration] 초기화 완료 - 공명주파수: {resonanceFrequency}Hz, 압력증폭: {pressureGain:F1}x");
    }

    void Update()
    {
        if (!Application.isPlaying) return;

        maxDisplacementMeters = maxStapesDisplacementMicron * 1e-6f;

        // 고막에서 진동 입력 받기
        if (tympanicMembrane != null)
        {
            inputVibration = tympanicMembrane.GetTransmittedVibration();

            // 현재 주파수 추정 (고막의 inputFrequency 접근 필요)
            // 임시로 외부에서 설정받도록 함
        }
        else
        {
            inputVibration = 0f;
        }

        UpdateVibration();
        ApplyVisualDisplacement();
    }

    void UpdateVibration()
    {
        if (inputVibration <= 0.001f)
        {
            // 진동 없으면 원래 위치로 복귀
            displacement = Mathf.Lerp(displacement, 0f, Time.deltaTime * 10f);
            velocity *= 0.9f;
            currentDisplacementMicron = 0f;
            transmittedToStapes = 0f;
            return;
        }

        // 주파수 응답 계산 (2차 공명 시스템)
        frequencyResponse = CalculateFrequencyResponse(currentFrequency);

        // 위상 업데이트
        phase += currentFrequency * Time.deltaTime * 2f * Mathf.PI;
        if (phase > 2f * Mathf.PI) phase -= 2f * Mathf.PI;

        float deltaTime = Time.deltaTime;

        // 목표 변위 계산
        // 고막 진동 × 주파수응답 × 최대변위
        float targetDisplacement = inputVibration * frequencyResponse * maxDisplacementMeters;

        // 스프링-댐퍼 시스템 (관절 특성 반영)
        float springConstant = ligamentStiffness * 10000f;
        float dampingConstant = jointDamping * 1000f;

        // 목표 위치까지의 힘
        float springForce = springConstant * (targetDisplacement * Mathf.Sin(phase) - displacement);
        float dampingForce = -dampingConstant * velocity;
        float totalForce = springForce + dampingForce;

        // 이소골 등가 질량 (약 25-50mg)
        float mass = 0.00004f; // 40mg in kg
        float acceleration = totalForce / mass;

        // 속도 및 위치 업데이트
        velocity += acceleration * deltaTime;
        displacement += velocity * deltaTime;

        // 최대 변위 제한
        if (Mathf.Abs(displacement) > maxDisplacementMeters * 2f)
        {
            displacement = Mathf.Sign(displacement) * maxDisplacementMeters * 2f;
            velocity *= 0.5f;
        }

        // 현재 변위 (μm 단위로 표시)
        currentDisplacementMicron = displacement * 1e6f;

        // 등골로 전달되는 진동량 계산
        // 압력 증폭 효과 포함
        transmittedToStapes = Mathf.Abs(displacement / maxDisplacementMeters) * inputVibration * frequencyResponse;
    }

    /// <summary>
    /// 주파수 응답 계산 (2차 공명 시스템)
    /// 논문 기반: 700-1200Hz에서 공명
    /// </summary>
    float CalculateFrequencyResponse(float frequency)
    {
        float omega = frequency / resonanceFrequency;

        // 2차 시스템 전달함수
        float denominator = Mathf.Sqrt(
            Mathf.Pow(1f - omega * omega, 2) +
            Mathf.Pow(omega / qFactor, 2)
        );

        float response = 1f / Mathf.Max(denominator, 0.1f);

        // 고주파에서 추가 감쇠 (관절의 점성 특성)
        if (frequency > resonanceFrequency * 1.5f)
        {
            float highFreqAttenuation = Mathf.Exp(-(frequency - resonanceFrequency * 1.5f) / 2000f);
            response *= highFreqAttenuation;
        }

        return Mathf.Clamp(response, 0.1f, 2.0f);
    }

    [Header("=== 시각화 설정 ===")]
    [Tooltip("시각적 진동 활성화")]
    public bool enableVisualVibration = false;

    [Tooltip("시각화 배율 (실제 변위가 μm라 확대 필요)")]
    [Range(100f, 50000f)]
    public float visualScale = 5000f;

    /// <summary>
    /// 시각적 변위 적용 (확대해서 표시)
    /// </summary>
    void ApplyVisualDisplacement()
    {
        // 초기화 안됐으면 리턴
        if (!isInitialized)
        {
            return;
        }

        // 시각화 비활성화시 아무것도 안 함
        if (!enableVisualVibration)
        {
            return;
        }

        // NaN 체크
        if (float.IsNaN(displacement) || float.IsInfinity(displacement))
        {
            displacement = 0f;
            return;
        }

        // 변위 제한 (매우 작은 값으로)
        float maxDisp = maxDisplacementMeters * 2f;
        if (maxDisp <= 0) maxDisp = 0.000002f; // 2 μm
        float clampedDisplacement = Mathf.Clamp(displacement, -maxDisp, maxDisp);

        // 시각화 변위 계산
        Vector3 direction = transform.forward;
        if (direction == Vector3.zero) direction = Vector3.forward;

        Vector3 visualDisplacement = direction * clampedDisplacement * visualScale;

        // NaN 체크 및 크기 제한 후 적용
        if (!float.IsNaN(visualDisplacement.x) && !float.IsNaN(visualDisplacement.y) && !float.IsNaN(visualDisplacement.z) &&
            !float.IsInfinity(visualDisplacement.x) && !float.IsInfinity(visualDisplacement.y) && !float.IsInfinity(visualDisplacement.z))
        {
            // 최대 이동 거리 제한 (원래 위치에서 0.005m = 5mm 이내)
            if (visualDisplacement.magnitude < 0.005f)
            {
                transform.localPosition = originalPosition + visualDisplacement;
            }
            else
            {
                // 너무 크면 원래 위치 유지
                transform.localPosition = originalPosition;
            }
        }

        // 회전은 비활성화 (문제 발생 가능성 높음)
        // transform.localRotation = originalRotation;
    }

    // ==================== Public API ====================

    /// <summary>
    /// 주파수 설정 (EarSimulator에서 호출)
    /// </summary>
    public void SetFrequency(float frequency)
    {
        currentFrequency = Mathf.Clamp(frequency, 20f, 20000f);
    }

    /// <summary>
    /// 등골로 전달되는 진동량 반환 (정규화된 값, 0-1)
    /// </summary>
    public float GetTransmittedToStapes()
    {
        return Mathf.Clamp01(transmittedToStapes);
    }

    /// <summary>
    /// 현재 등골 변위 반환 (μm)
    /// </summary>
    public float GetStapesDisplacementMicron()
    {
        return currentDisplacementMicron;
    }

    /// <summary>
    /// 주파수 응답 반환
    /// </summary>
    public float GetFrequencyResponse()
    {
        return frequencyResponse;
    }

    /// <summary>
    /// 압력 증폭 비율 반환 (레버비 × 면적비)
    /// </summary>
    public float GetPressureGain()
    {
        return pressureGain;
    }

    /// <summary>
    /// 직접 진동 적용 (TympanicMembrane 없이 테스트용)
    /// </summary>
    public void ApplyDirectVibration(float amplitude, float frequency)
    {
        inputVibration = Mathf.Clamp01(amplitude);
        currentFrequency = Mathf.Clamp(frequency, 20f, 20000f);
    }

    // ==================== Gizmos ====================

    void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying) return;

        // 진동 방향 표시
        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(transform.position, transform.forward * 0.01f);

        // 현재 변위 시각화
        if (Mathf.Abs(displacement) > 1e-9f)
        {
            Gizmos.color = Color.yellow;
            float visualScale = 10000f;
            Gizmos.DrawLine(
                transform.position,
                transform.position + transform.forward * displacement * visualScale
            );
        }
    }
}
