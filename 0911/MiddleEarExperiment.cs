using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class MiddleEarComponents
{
    [Header("Ear Components")]
    public GameObject tympanicMembrane;      // 고막
    public GameObject malleus;               // 망치뼈 (추골)
    public GameObject incus;                 // 모루뼈 (침골)  
    public GameObject stapes;                // 등자뼈 (등골)
    public GameObject cochlea;               // 달팽이관
    
    [Header("Audio Input")]
    public AudioSource audioSource;
    public AudioClip testSound;
    
    [Header("Visualization")]
    public ParticleSystem soundWaveParticles;
    public LineRenderer vibrationPath;
}

[System.Serializable] 
public class ExperimentSettings
{
    [Header("Audio Settings")]
    [Range(20f, 20000f)] public float frequency = 440f;
    [Range(0f, 1f)] public float amplitude = 0.5f;
    
    [Header("Perforation Settings")]
    [Range(0f, 1f)] public float perforationSize = 0f;
    public Vector2 perforationPosition = Vector2.zero;
    
    [Header("Physics Settings")]
    public bool enableRealtimePhysics = true;
    public float dampingFactor = 0.8f;
    public float stiffnessFactor = 0.3f;
}

public class MiddleEarExperiment : MonoBehaviour
{
    [Header("Components")]
    public MiddleEarComponents components;
    
    [Header("Settings")]
    public ExperimentSettings settings;
    
    [Header("UI Controls")]
    public Slider frequencySlider;
    public Slider amplitudeSlider;
    public Slider perforationSlider;
    public Button playButton;
    public Button stopButton;
    public Text statusText;
    
    // Private components
    private TympanicMembrane tympanicMembraneScript;
    private OssicleChain ossicleChainScript;
    private AudioProcessor audioProcessor;
    private MeshPerforator meshPerforator;
    private InnerEarReceiver innerEarReceiver;
    
    // State
    private bool isPlaying = false;
    private float currentSoundLevel = 0f;
    
    void Start()
    {
        InitializeComponents();
        SetupUI();
        SetupAudioSystem();
    }
    
    void InitializeComponents()
    {
        // Get or add required scripts
        tympanicMembraneScript = components.tympanicMembrane.GetComponent<TympanicMembrane>();
        if (tympanicMembraneScript == null)
            tympanicMembraneScript = components.tympanicMembrane.AddComponent<TympanicMembrane>();
            
        ossicleChainScript = GetComponent<OssicleChain>();
        if (ossicleChainScript == null)
            ossicleChainScript = gameObject.AddComponent<OssicleChain>();
            
        audioProcessor = GetComponent<AudioProcessor>();
        if (audioProcessor == null)
            audioProcessor = gameObject.AddComponent<AudioProcessor>();
            
        meshPerforator = components.tympanicMembrane.GetComponent<MeshPerforator>();
        if (meshPerforator == null)
            meshPerforator = components.tympanicMembrane.AddComponent<MeshPerforator>();
            
        innerEarReceiver = components.cochlea.GetComponent<InnerEarReceiver>();
        if (innerEarReceiver == null)
            innerEarReceiver = components.cochlea.AddComponent<InnerEarReceiver>();
        
        // Initialize ossicle chain references
        ossicleChainScript.InitializeChain(components.malleus, components.incus, components.stapes);
        
        statusText.text = "Middle Ear Experiment Ready";
    }
    
    void SetupUI()
    {
        // Setup sliders with safety checks
        if (frequencySlider != null)
        {
            float normalizedFrequency = float.IsFinite(settings.frequency) ? settings.frequency / 20000f : 0.022f; // 440Hz default
            frequencySlider.value = Mathf.Clamp01(normalizedFrequency);
        }
        
        if (amplitudeSlider != null)
        {
            float safeAmplitude = float.IsFinite(settings.amplitude) ? Mathf.Clamp01(settings.amplitude) : 0.5f;
            amplitudeSlider.value = safeAmplitude;
        }
        
        if (perforationSlider != null)
        {
            float safePerforationSize = float.IsFinite(settings.perforationSize) ? Mathf.Clamp01(settings.perforationSize) : 0f;
            perforationSlider.value = safePerforationSize;
        }
        
        // Add listeners with null checks
        if (frequencySlider != null)
            frequencySlider.onValueChanged.AddListener(OnFrequencyChanged);
        if (amplitudeSlider != null)
            amplitudeSlider.onValueChanged.AddListener(OnAmplitudeChanged);
        if (perforationSlider != null)
            perforationSlider.onValueChanged.AddListener(OnPerforationChanged);
        
        if (playButton != null)
            playButton.onClick.AddListener(StartExperiment);
        if (stopButton != null)
            stopButton.onClick.AddListener(StopExperiment);
    }
    
    void SetupAudioSystem()
    {
        if (components.audioSource == null)
        {
            components.audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        components.audioSource.clip = components.testSound;
        components.audioSource.loop = true;
        components.audioSource.playOnAwake = false;
    }
    
    void Update()
    {
        if (isPlaying && settings.enableRealtimePhysics)
        {
            ProcessRealTimeAudio();
            UpdateVisualization();
        }
    }
    
    void ProcessRealTimeAudio()
    {
        // Get current audio amplitude with safety check
        if (audioProcessor != null)
        {
            float rawAmplitude = audioProcessor.GetCurrentAmplitude();
            currentSoundLevel = float.IsFinite(rawAmplitude) ? Mathf.Clamp01(rawAmplitude) : 0f;
        }
        else
        {
            currentSoundLevel = 0f;
        }
        
        // Ensure frequency is valid
        float safeFrequency = float.IsFinite(settings.frequency) && settings.frequency > 0f ? 
                             Mathf.Clamp(settings.frequency, 20f, 20000f) : 440f;
        
        // Apply to tympanic membrane with safety checks
        if (tympanicMembraneScript != null)
        {
            tympanicMembraneScript.ApplyVibration(currentSoundLevel, safeFrequency);
        }
        
        // Get vibration from tympanic membrane and apply to ossicles
        float membraneVibration = 0f;
        if (tympanicMembraneScript != null)
        {
            float rawVibration = tympanicMembraneScript.GetCurrentVibration();
            membraneVibration = float.IsFinite(rawVibration) ? Mathf.Clamp(rawVibration, -1f, 1f) : 0f;
        }
        
        if (ossicleChainScript != null)
        {
            ossicleChainScript.ReceiveVibration(membraneVibration, safeFrequency);
        }
        
        // Get final output and send to inner ear
        float ossicleOutput = 0f;
        if (ossicleChainScript != null)
        {
            float rawOutput = ossicleChainScript.GetOutputVibration();
            ossicleOutput = float.IsFinite(rawOutput) ? Mathf.Clamp(rawOutput, -1f, 1f) : 0f;
        }
        
        if (innerEarReceiver != null)
        {
            innerEarReceiver.ReceiveVibration(ossicleOutput, safeFrequency);
        }
    }
    
    void UpdateVisualization()
    {
        // Update particle system based on sound level with safety checks
        if (components.soundWaveParticles != null)
        {
            var emission = components.soundWaveParticles.emission;
            float safeEmissionRate = float.IsFinite(currentSoundLevel) ? currentSoundLevel * 100f : 0f;
            emission.rateOverTime = Mathf.Clamp(safeEmissionRate, 0f, 1000f);
            
            var velocityOverLifetime = components.soundWaveParticles.velocityOverLifetime;
            velocityOverLifetime.space = ParticleSystemSimulationSpace.Local;
        }
        
        // Update vibration path visualization
        if (components.vibrationPath != null)
        {
            UpdateVibrationPath();
        }
        
        // Update status text with safety checks
        if (statusText != null)
        {
            float safeFrequency = float.IsFinite(settings.frequency) ? settings.frequency : 440f;
            float safeAmplitude = float.IsFinite(currentSoundLevel) ? currentSoundLevel : 0f;
            
            float innerEarLevel = 0f;
            if (innerEarReceiver != null)
            {
                float rawLevel = innerEarReceiver.GetCurrentLevel();
                innerEarLevel = float.IsFinite(rawLevel) ? rawLevel : 0f;
            }
            
            statusText.text = $"Frequency: {safeFrequency:F0}Hz | " +
                             $"Amplitude: {safeAmplitude:F2} | " +
                             $"Inner Ear Level: {innerEarLevel:F2}dB";
        }
    }
    
    void UpdateVibrationPath()
    {
        // Safety checks for component references
        if (components.tympanicMembrane?.transform == null || 
            components.malleus?.transform == null || 
            components.incus?.transform == null || 
            components.stapes?.transform == null)
            return;
        
        Vector3[] positions = new Vector3[4];
        positions[0] = components.tympanicMembrane.transform.position;
        positions[1] = components.malleus.transform.position;
        positions[2] = components.incus.transform.position; 
        positions[3] = components.stapes.transform.position;
        
        // Safety check for position validity
        for (int i = 0; i < positions.Length; i++)
        {
            if (!IsValidVector3(positions[i]))
            {
                positions[i] = Vector3.zero; // Fallback position
            }
        }
        
        components.vibrationPath.positionCount = 4;
        components.vibrationPath.SetPositions(positions);
        
        // Color based on vibration intensity with safety check
        float safeSoundLevel = float.IsFinite(currentSoundLevel) ? Mathf.Clamp01(currentSoundLevel) : 0f;
        Color lineColor = Color.Lerp(Color.blue, Color.red, safeSoundLevel);
        components.vibrationPath.startColor = lineColor;
        components.vibrationPath.endColor = lineColor;
    }
    
    // Helper method to check Vector3 validity
    bool IsValidVector3(Vector3 v)
    {
        return float.IsFinite(v.x) && float.IsFinite(v.y) && float.IsFinite(v.z);
    }
    
    public void StartExperiment()
    {
        isPlaying = true;
        components.audioSource.Play();
        audioProcessor.StartProcessing();
        
        playButton.interactable = false;
        stopButton.interactable = true;
        
        statusText.text = "Experiment Running...";
    }
    
    public void StopExperiment()
    {
        isPlaying = false;
        components.audioSource.Stop();
        audioProcessor.StopProcessing();
        
        playButton.interactable = true;
        stopButton.interactable = false;
        
        statusText.text = "Experiment Stopped";
    }
    
    public void OnFrequencyChanged(float value)
    {
        // Safety check for input value
        if (!float.IsFinite(value))
            value = 0.022f; // Default to 440Hz (440/20000 = 0.022)
        
        value = Mathf.Clamp01(value); // Ensure valid slider range
        
        float newFrequency = value * 20000f;
        
        // Safety check for multiplication result
        if (!float.IsFinite(newFrequency))
            newFrequency = 440f; // Default frequency
        
        settings.frequency = Mathf.Clamp(newFrequency, 20f, 20000f);
        
        if (audioProcessor != null)
        {
            audioProcessor.SetTargetFrequency(settings.frequency);
        }
    }
    
    public void OnAmplitudeChanged(float value)
    {
        // Safety check for input value
        if (!float.IsFinite(value))
            value = 0.5f; // Default amplitude
        
        value = Mathf.Clamp01(value); // Ensure valid range
        
        settings.amplitude = value;
        
        if (components.audioSource != null)
        {
            components.audioSource.volume = value;
        }
    }
    
    public void OnPerforationChanged(float value)
    {
        // Safety check for input value
        if (!float.IsFinite(value))
            value = 0f; // Default no perforation
        
        value = Mathf.Clamp01(value); // Ensure valid range
        
        settings.perforationSize = value;
        
        if (meshPerforator != null)
        {
            meshPerforator.SetPerforationSize(value);
        }
    }
    
    void OnGUI()
    {
        // Debug information with safety checks
        if (Application.isEditor)
        {
            GUILayout.BeginArea(new Rect(10, 10, 300, 200));
            
            // Display frequency with safety check
            float safeFrequency = float.IsFinite(settings.frequency) ? settings.frequency : 440f;
            GUILayout.Label($"Current Frequency: {safeFrequency:F0} Hz");
            
            // Display sound level with safety check
            float safeSoundLevel = float.IsFinite(currentSoundLevel) ? currentSoundLevel : 0f;
            GUILayout.Label($"Sound Level: {safeSoundLevel:F3}");
            
            // Display perforation with safety check
            float safePerforationSize = float.IsFinite(settings.perforationSize) ? settings.perforationSize : 0f;
            GUILayout.Label($"Perforation: {safePerforationSize:F2}");
            
            // Display inner ear response with safety checks
            if (innerEarReceiver != null)
            {
                float rawLevel = innerEarReceiver.GetCurrentLevel();
                float safeInnerEarLevel = float.IsFinite(rawLevel) ? rawLevel : 0f;
                GUILayout.Label($"Inner Ear Response: {safeInnerEarLevel:F1} dB");
            }
            else
            {
                GUILayout.Label("Inner Ear Response: N/A");
            }
            
            GUILayout.EndArea();
        }
    }
}