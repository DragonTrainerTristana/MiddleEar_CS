using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[System.Serializable]
public class CochlearResponse
{
    [Header("주파수 응답")]
    public float[] frequencyBands;          // 주파수 대역들 (Hz)
    public float[] sensitivityLevels;       // 각 대역별 감도
    public float[] currentActivation;       // 현재 활성화 레벨
    
    [Header("임계값")]
    public float hearingThreshold = 20f;    // 청각 임계값 (dB SPL)
    public float painThreshold = 120f;      // 고통 임계값 (dB SPL)
    public float damageThreshold = 90f;     // 손상 임계값 (dB SPL)
    
    [Header("적응 반응")]
    public float adaptationRate = 0.1f;     // 적응 속도
    public float recoveryRate = 0.05f;      // 회복 속도
    public float currentAdaptation = 0f;    // 현재 적응 레벨
}

[System.Serializable]
public class InnerEarData
{
    [Header("실시간 측정값")]
    [ReadOnly] public float currentSPL;              // 현재 음압 레벨 (dB SPL)
    [ReadOnly] public float peakSPL;                 // 피크 음압 레벨
    [ReadOnly] public float averageSPL;              // 평균 음압 레벨
    [ReadOnly] public float frequencyAnalysis;       // 주파수 분석 결과
    
    [Header("누적 데이터")]
    [ReadOnly] public float totalExposureTime;       // 총 노출 시간 (초)
    [ReadOnly] public float cumulativeExposure;      // 누적 노출량
    [ReadOnly] public float hearingDamageRisk;       // 청력 손상 위험도 (0-1)
    
    [Header("상태")]
    [ReadOnly] public bool isReceivingSound;         // 소리 수신 중인가
    [ReadOnly] public bool isOverThreshold;          // 임계값 초과인가
    [ReadOnly] public string currentHearingStatus;   // 현재 청력 상태
}

public class InnerEarReceiver : MonoBehaviour
{
    [Header("Cochlear Response")]
    public CochlearResponse cochlearResponse;
    
    [Header("Measurement Data")]
    public InnerEarData measurementData;
    
    [Header("Settings")]
    [Range(0.1f, 5.0f)] public float measurementInterval = 0.1f;  // 측정 간격 (초)
    [Range(1f, 60f)] public float averagingWindow = 5f;           // 평균 계산 윈도우 (초)
    public bool enableRealTimeAnalysis = true;
    public bool logMeasurements = false;
    
    [Header("Visualization")]
    public GameObject cochlearVisualizationPrefab;
    public ParticleSystem soundVisualization;
    public LineRenderer frequencyResponse;
    public Color normalColor = Color.green;
    public Color warningColor = Color.yellow;
    public Color dangerColor = Color.red;
    
    // Private variables
    private Queue<float> splHistory;
    private Queue<float> timeHistory;
    private float lastMeasurementTime;
    private float inputVibration = 0f;
    private float inputFrequency = 440f;
    
    // Cochlear modeling
    private const int FREQUENCY_BANDS = 24;  // 사람 귀의 임계 주파수 대역 수
    private float[] basalFrequencies;        // 기저부 주파수들 (고주파)
    private float[] apicalFrequencies;       // 첨부 주파수들 (저주파)
    
    // Reference values
    private const float REFERENCE_PRESSURE = 20e-6f;  // 20 μPa (0 dB SPL 기준)
    private const float STAPES_FOOTPLATE_AREA = 3.2e-6f; // m² (등자뼈 발판 면적)
    
    void Start()
    {
        InitializeInnerEar();
        SetupCochlearModel();
        InitializeVisualization();
    }
    
    void InitializeInnerEar()
    {
        // Initialize data structures
        splHistory = new Queue<float>();
        timeHistory = new Queue<float>();
        
        // Initialize measurement data
        measurementData = new InnerEarData
        {
            currentHearingStatus = "Normal"
        };
        
        lastMeasurementTime = Time.time;
        
        Debug.Log("InnerEarReceiver initialized");
    }
    
    void SetupCochlearModel()
    {
        // Initialize cochlear response
        cochlearResponse.frequencyBands = new float[FREQUENCY_BANDS];
        cochlearResponse.sensitivityLevels = new float[FREQUENCY_BANDS];
        cochlearResponse.currentActivation = new float[FREQUENCY_BANDS];
        
        // Create frequency bands (logarithmic distribution from 20Hz to 20kHz)
        for (int i = 0; i < FREQUENCY_BANDS; i++)
        {
            float normalizedPos = (float)i / (FREQUENCY_BANDS - 1);
            cochlearResponse.frequencyBands[i] = Mathf.Lerp(20f, 20000f, Mathf.Pow(normalizedPos, 2));
            
            // Sensitivity curve (human hearing sensitivity)
            cochlearResponse.sensitivityLevels[i] = CalculateHumanSensitivity(cochlearResponse.frequencyBands[i]);
        }
        
        // Initialize arrays for frequency modeling
        basalFrequencies = new float[FREQUENCY_BANDS / 2];
        apicalFrequencies = new float[FREQUENCY_BANDS / 2];
        
        for (int i = 0; i < FREQUENCY_BANDS / 2; i++)
        {
            basalFrequencies[i] = cochlearResponse.frequencyBands[FREQUENCY_BANDS / 2 + i];
            apicalFrequencies[i] = cochlearResponse.frequencyBands[i];
        }
    }
    
    float CalculateHumanSensitivity(float frequency)
    {
        // ISO 226:2003 Equal-loudness contours를 기반한 인간 청각 감도 곡선
        if (frequency < 100f)
            return 0.3f;
        else if (frequency < 1000f)
            return Mathf.Lerp(0.3f, 1.0f, (frequency - 100f) / 900f);
        else if (frequency <= 4000f)
            return 1.0f; // 최대 감도
        else if (frequency < 8000f)
            return Mathf.Lerp(1.0f, 0.8f, (frequency - 4000f) / 4000f);
        else
            return Mathf.Lerp(0.8f, 0.4f, (frequency - 8000f) / 12000f);
    }
    
    void InitializeVisualization()
    {
        // Setup particle system for sound visualization
        if (soundVisualization != null)
        {
            var main = soundVisualization.main;
            main.startColor = normalColor;
            main.maxParticles = 100;
            
            var emission = soundVisualization.emission;
            emission.rateOverTime = 0;
        }
        
        // Setup frequency response line renderer
        if (frequencyResponse != null)
        {
            frequencyResponse.positionCount = FREQUENCY_BANDS;
            frequencyResponse.startColor = normalColor;
            frequencyResponse.endColor = normalColor;
            frequencyResponse.startWidth = 0.001f;
            frequencyResponse.endWidth = 0.001f;
        }
    }
    
    void Update()
    {
        if (enableRealTimeAnalysis)
        {
            // Periodic measurements
            if (Time.time - lastMeasurementTime >= measurementInterval)
            {
                PerformMeasurement();
                lastMeasurementTime = Time.time;
            }
            
            // Update cochlear response
            UpdateCochlearResponse();
            
            // Update visualization
            UpdateVisualization();
            
            // Update status
            UpdateHearingStatus();
        }
    }
    
    void PerformMeasurement()
    {
        if (inputVibration <= 0f)
        {
            measurementData.isReceivingSound = false;
            return;
        }
        
        measurementData.isReceivingSound = true;
        
        // Convert vibration amplitude to sound pressure level
        float soundPressure = ConvertVibrationToSPL(inputVibration, inputFrequency);
        measurementData.currentSPL = soundPressure;
        
        // Update peak
        if (soundPressure > measurementData.peakSPL)
        {
            measurementData.peakSPL = soundPressure;
        }
        
        // Add to history
        splHistory.Enqueue(soundPressure);
        timeHistory.Enqueue(Time.time);
        
        // Remove old data outside averaging window
        while (timeHistory.Count > 0 && Time.time - timeHistory.Peek() > averagingWindow)
        {
            splHistory.Dequeue();
            timeHistory.Dequeue();
        }
        
        // Calculate average
        if (splHistory.Count > 0)
        {
            measurementData.averageSPL = splHistory.Average();
        }
        
        // Update exposure data
        UpdateExposureData();
        
        // Check thresholds
        CheckThresholds();
        
        if (logMeasurements)
        {
            Debug.Log($"Inner Ear: SPL={soundPressure:F1}dB, Freq={inputFrequency:F0}Hz, " +
                     $"Avg={measurementData.averageSPL:F1}dB");
        }
    }
    
    float ConvertVibrationToSPL(float vibrationAmplitude, float frequency)
    {
        // Convert mechanical vibration to sound pressure
        // Based on middle ear transfer function and stapes motion
        
        // Assume vibration amplitude is in meters (displacement of stapes footplate)
        float velocity = vibrationAmplitude * 2 * Mathf.PI * frequency;
        
        // Convert to volume velocity (velocity * area)
        float volumeVelocity = velocity * STAPES_FOOTPLATE_AREA;
        
        // Convert to sound pressure using cochlear impedance
        float cochlearImpedance = 1.5e9f; // Pa·s/m³ (approximate)
        float soundPressure = volumeVelocity * cochlearImpedance;
        
        // Convert to dB SPL
        float splValue = 20f * Mathf.Log10(soundPressure / REFERENCE_PRESSURE);
        
        // Apply frequency-dependent corrections
        splValue *= GetFrequencyCorrection(frequency);
        
        // Clamp to reasonable values
        return Mathf.Clamp(splValue, 0f, 140f);
    }
    
    float GetFrequencyCorrection(float frequency)
    {
        // Middle ear transfer function frequency response
        if (frequency < 100f)
            return 0.1f;
        else if (frequency < 500f)
            return Mathf.Lerp(0.1f, 1.0f, (frequency - 100f) / 400f);
        else if (frequency <= 4000f)
            return 1.0f;
        else if (frequency < 8000f)
            return Mathf.Lerp(1.0f, 0.7f, (frequency - 4000f) / 4000f);
        else
            return 0.7f;
    }
    
    void UpdateCochlearResponse()
    {
        for (int i = 0; i < FREQUENCY_BANDS; i++)
        {
            float bandFrequency = cochlearResponse.frequencyBands[i];
            float sensitivity = cochlearResponse.sensitivityLevels[i];
            
            // Calculate activation based on input frequency and current SPL
            float activation = CalculateBandActivation(bandFrequency, sensitivity);
            
            // Apply adaptation
            float targetActivation = activation * (1.0f - cochlearResponse.currentAdaptation);
            cochlearResponse.currentActivation[i] = Mathf.Lerp(
                cochlearResponse.currentActivation[i], 
                targetActivation, 
                Time.deltaTime / cochlearResponse.adaptationRate
            );
        }
        
        // Update adaptation level
        float maxActivation = cochlearResponse.currentActivation.Max();
        if (maxActivation > 0.8f)
        {
            // Increase adaptation for loud sounds
            cochlearResponse.currentAdaptation = Mathf.Lerp(
                cochlearResponse.currentAdaptation, 
                0.5f, 
                Time.deltaTime / cochlearResponse.adaptationRate
            );
        }
        else
        {
            // Recovery during quiet periods
            cochlearResponse.currentAdaptation = Mathf.Lerp(
                cochlearResponse.currentAdaptation, 
                0f, 
                Time.deltaTime / cochlearResponse.recoveryRate
            );
        }
    }
    
    float CalculateBandActivation(float bandFrequency, float sensitivity)
    {
        if (!measurementData.isReceivingSound) return 0f;
        
        // Frequency selectivity (how much this band responds to input frequency)
        float frequencyDistance = Mathf.Abs(Mathf.Log10(inputFrequency) - Mathf.Log10(bandFrequency));
        float selectivity = Mathf.Exp(-frequencyDistance * 3f); // Sharp tuning curve
        
        // Intensity-dependent response
        float thresholdExcess = Mathf.Max(0f, measurementData.currentSPL - cochlearResponse.hearingThreshold);
        float intensityResponse = 1.0f - Mathf.Exp(-thresholdExcess / 20f);
        
        return selectivity * intensityResponse * sensitivity;
    }
    
    void UpdateExposureData()
    {
        if (!measurementData.isReceivingSound) return;
        
        measurementData.totalExposureTime += measurementInterval;
        
        // Calculate cumulative exposure (energy-based)
        if (measurementData.currentSPL > cochlearResponse.hearingThreshold)
        {
            float excessSPL = measurementData.currentSPL - cochlearResponse.hearingThreshold;
            float exposureContribution = Mathf.Pow(10f, excessSPL / 10f) * measurementInterval;
            measurementData.cumulativeExposure += exposureContribution;
        }
        
        // Calculate hearing damage risk (simplified model)
        float riskFromLevel = Mathf.Clamp01((measurementData.averageSPL - 80f) / 40f);
        float riskFromTime = Mathf.Clamp01(measurementData.totalExposureTime / 28800f); // 8 hours
        measurementData.hearingDamageRisk = Mathf.Max(riskFromLevel, riskFromTime);
    }
    
    void CheckThresholds()
    {
        measurementData.isOverThreshold = measurementData.currentSPL > cochlearResponse.damageThreshold;
        
        if (measurementData.currentSPL > cochlearResponse.painThreshold)
        {
            Debug.LogWarning($"Inner Ear: Pain threshold exceeded! SPL = {measurementData.currentSPL:F1} dB");
        }
        else if (measurementData.currentSPL > cochlearResponse.damageThreshold)
        {
            Debug.LogWarning($"Inner Ear: Damage threshold exceeded! SPL = {measurementData.currentSPL:F1} dB");
        }
    }
    
    void UpdateHearingStatus()
    {
        if (measurementData.hearingDamageRisk < 0.1f)
            measurementData.currentHearingStatus = "Normal";
        else if (measurementData.hearingDamageRisk < 0.3f)
            measurementData.currentHearingStatus = "Caution";
        else if (measurementData.hearingDamageRisk < 0.7f)
            measurementData.currentHearingStatus = "Warning";
        else
            measurementData.currentHearingStatus = "Danger";
    }
    
    void UpdateVisualization()
    {
        // Update particle system
        if (soundVisualization != null)
        {
            var emission = soundVisualization.emission;
            if (measurementData.isReceivingSound)
            {
                emission.rateOverTime = measurementData.currentSPL * 2f;
                
                var main = soundVisualization.main;
                if (measurementData.currentSPL > cochlearResponse.damageThreshold)
                    main.startColor = dangerColor;
                else if (measurementData.currentSPL > cochlearResponse.hearingThreshold + 40f)
                    main.startColor = warningColor;
                else
                    main.startColor = normalColor;
            }
            else
            {
                emission.rateOverTime = 0f;
            }
        }
        
        // Update frequency response visualization
        if (frequencyResponse != null && cochlearResponse.currentActivation != null)
        {
            UpdateFrequencyResponseVisualization();
        }
    }
    
    void UpdateFrequencyResponseVisualization()
    {
        Vector3[] positions = new Vector3[FREQUENCY_BANDS];
        
        for (int i = 0; i < FREQUENCY_BANDS; i++)
        {
            float x = (float)i / (FREQUENCY_BANDS - 1) * 0.01f; // 1cm wide
            float y = cochlearResponse.currentActivation[i] * 0.005f; // 5mm tall
            
            positions[i] = transform.position + new Vector3(x, y, 0);
        }
        
        frequencyResponse.SetPositions(positions);
        
        // Color based on overall activation
        float maxActivation = cochlearResponse.currentActivation.Max();
        Color responseColor;
        if (maxActivation > 0.8f)
            responseColor = dangerColor;
        else if (maxActivation > 0.5f)
            responseColor = warningColor;
        else
            responseColor = normalColor;
        frequencyResponse.startColor = responseColor;
        frequencyResponse.endColor = responseColor;
    }
    
    // Public API methods
    public void ReceiveVibration(float vibrationAmplitude, float frequency)
    {
        inputVibration = vibrationAmplitude;
        inputFrequency = frequency;
    }
    
    public float GetCurrentLevel()
    {
        return measurementData.currentSPL;
    }
    
    public float GetAverageLevel()
    {
        return measurementData.averageSPL;
    }
    
    public string GetHearingStatus()
    {
        return measurementData.currentHearingStatus;
    }
    
    public float GetHearingDamageRisk()
    {
        return measurementData.hearingDamageRisk;
    }
    
    public CochlearResponse GetCochlearResponse()
    {
        return cochlearResponse;
    }
    
    public void ResetMeasurements()
    {
        splHistory.Clear();
        timeHistory.Clear();
        measurementData.peakSPL = 0f;
        measurementData.totalExposureTime = 0f;
        measurementData.cumulativeExposure = 0f;
        measurementData.hearingDamageRisk = 0f;
        cochlearResponse.currentAdaptation = 0f;
        
        for (int i = 0; i < cochlearResponse.currentActivation.Length; i++)
        {
            cochlearResponse.currentActivation[i] = 0f;
        }
        
        Debug.Log("Inner ear measurements reset");
    }
    
    void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying) return;
        
        // Draw current SPL as a sphere
        Gizmos.color = measurementData.isReceivingSound ? Color.green : Color.gray;
        float sphereSize = Mathf.Lerp(0.001f, 0.01f, measurementData.currentSPL / 100f);
        Gizmos.DrawWireSphere(transform.position, sphereSize);
        
        // Draw frequency bands
        if (cochlearResponse.currentActivation != null)
        {
            for (int i = 0; i < cochlearResponse.currentActivation.Length; i++)
            {
                float activation = cochlearResponse.currentActivation[i];
                if (activation > 0.1f)
                {
                    Vector3 position = transform.position + new Vector3(i * 0.0005f, activation * 0.002f, 0);
                    Gizmos.color = Color.Lerp(Color.blue, Color.red, activation);
                    Gizmos.DrawCube(position, Vector3.one * 0.0002f);
                }
            }
        }
    }
}