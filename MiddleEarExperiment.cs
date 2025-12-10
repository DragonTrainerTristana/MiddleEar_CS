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
        // Setup sliders
        frequencySlider.value = settings.frequency / 20000f;
        amplitudeSlider.value = settings.amplitude;
        perforationSlider.value = settings.perforationSize;
        
        // Add listeners
        frequencySlider.onValueChanged.AddListener(OnFrequencyChanged);
        amplitudeSlider.onValueChanged.AddListener(OnAmplitudeChanged);
        perforationSlider.onValueChanged.AddListener(OnPerforationChanged);
        
        playButton.onClick.AddListener(StartExperiment);
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
        // Get current audio amplitude
        currentSoundLevel = audioProcessor.GetCurrentAmplitude();
        
        // Apply to tympanic membrane
        tympanicMembraneScript.ApplyVibration(currentSoundLevel, settings.frequency);
        
        // Get vibration from tympanic membrane and apply to ossicles
        float membraneVibration = tympanicMembraneScript.GetCurrentVibration();
        ossicleChainScript.ReceiveVibration(membraneVibration, settings.frequency);
        
        // Get final output and send to inner ear
        float ossicleOutput = ossicleChainScript.GetOutputVibration();
        innerEarReceiver.ReceiveVibration(ossicleOutput, settings.frequency);
    }
    
    void UpdateVisualization()
    {
        // Update particle system based on sound level
        if (components.soundWaveParticles != null)
        {
            var emission = components.soundWaveParticles.emission;
            emission.rateOverTime = currentSoundLevel * 100f;
            
            var velocityOverLifetime = components.soundWaveParticles.velocityOverLifetime;
            velocityOverLifetime.space = ParticleSystemSimulationSpace.Local;
        }
        
        // Update vibration path visualization
        if (components.vibrationPath != null)
        {
            UpdateVibrationPath();
        }
        
        // Update status text
        statusText.text = $"Frequency: {settings.frequency:F0}Hz | " +
                         $"Amplitude: {currentSoundLevel:F2} | " +
                         $"Inner Ear Level: {innerEarReceiver.GetCurrentLevel():F2}dB";
    }
    
    void UpdateVibrationPath()
    {
        Vector3[] positions = new Vector3[4];
        positions[0] = components.tympanicMembrane.transform.position;
        positions[1] = components.malleus.transform.position;
        positions[2] = components.incus.transform.position; 
        positions[3] = components.stapes.transform.position;
        
        components.vibrationPath.positionCount = 4;
        components.vibrationPath.SetPositions(positions);
        
        // Color based on vibration intensity
        Color lineColor = Color.Lerp(Color.blue, Color.red, currentSoundLevel);
        components.vibrationPath.startColor = lineColor;
        components.vibrationPath.endColor = lineColor;
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
        settings.frequency = value * 20000f;
        audioProcessor.SetTargetFrequency(settings.frequency);
    }
    
    public void OnAmplitudeChanged(float value)
    {
        settings.amplitude = value;
        components.audioSource.volume = value;
    }
    
    public void OnPerforationChanged(float value)
    {
        settings.perforationSize = value;
        meshPerforator.SetPerforationSize(value);
    }
    
    void OnGUI()
    {
        // Debug information
        if (Application.isEditor)
        {
            GUILayout.BeginArea(new Rect(10, 10, 300, 200));
            GUILayout.Label($"Current Frequency: {settings.frequency:F0} Hz");
            GUILayout.Label($"Sound Level: {currentSoundLevel:F3}");
            GUILayout.Label($"Perforation: {settings.perforationSize:F2}");
            
            if (innerEarReceiver != null)
            {
                GUILayout.Label($"Inner Ear Response: {innerEarReceiver.GetCurrentLevel():F1} dB");
            }
            GUILayout.EndArea();
        }
    }
}