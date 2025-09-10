using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[System.Serializable]
public class AudioSettings
{
    [Header("Input Settings")]
    public bool useMicrophoneInput = true;
    public bool useGeneratedTone = false;
    public string selectedMicrophone = "";
    
    [Header("Generated Tone Settings")]
    [Range(20f, 20000f)] public float targetFrequency = 440f;
    [Range(0f, 1f)] public float toneAmplitude = 0.5f;
    public AnimationCurve envelopeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    [Header("Processing Settings")]
    public int sampleRate = 44100;
    public int bufferSize = 1024;
    [Range(0.1f, 5.0f)] public float analysisWindow = 1.0f; // seconds
    
    [Header("Frequency Analysis")]
    public bool enableFFT = true;
    public int fftSize = 1024;
    public FFTWindow fftWindow = FFTWindow.BlackmanHarris;
}

[System.Serializable]
public class AudioAnalysis
{
    [ReadOnly] public float currentAmplitude;
    [ReadOnly] public float dominantFrequency;
    [ReadOnly] public float[] spectrumData;
    [ReadOnly] public float rmsValue;
    [ReadOnly] public float peakValue;
    
    [Header("Frequency Bands (Hz)")]
    [ReadOnly] public float lowFreq;     // 20-250Hz
    [ReadOnly] public float midFreq;     // 250-4000Hz  
    [ReadOnly] public float highFreq;    // 4000-20000Hz
}

public class AudioProcessor : MonoBehaviour
{
    [Header("Settings")]
    public AudioSettings settings;
    
    [Header("Analysis Results")]
    public AudioAnalysis analysis;
    
    [Header("Debug")]
    public bool showDebugGUI = true;
    public bool logAudioData = false;
    
    // Private variables
    private AudioSource audioSource;
    private AudioClip microphoneClip;
    private bool isProcessing = false;
    
    // Audio data buffers
    private float[] audioBuffer;
    private float[] fftBuffer;
    private Queue<float> amplitudeHistory;
    
    // Generated tone variables
    private float tonePhase = 0f;
    private float toneSampleRate;
    
    // Microphone variables
    private int micPosition = 0;
    private int lastMicPosition = 0;
    
    void Start()
    {
        Initialize();
    }
    
    void Initialize()
    {
        // Get or create AudioSource
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        // Initialize settings
        toneSampleRate = AudioSettings.outputSampleRate;
        
        // Initialize buffers
        audioBuffer = new float[settings.bufferSize];
        fftBuffer = new float[settings.fftSize];
        amplitudeHistory = new Queue<float>();
        
        // Initialize analysis data
        analysis.spectrumData = new float[settings.fftSize / 2];
        
        // Setup microphone if available
        SetupMicrophone();
        
        Debug.Log($"AudioProcessor initialized - Sample Rate: {AudioSettings.outputSampleRate}Hz");
    }
    
    void SetupMicrophone()
    {
        if (Microphone.devices.Length > 0)
        {
            if (string.IsNullOrEmpty(settings.selectedMicrophone))
            {
                settings.selectedMicrophone = Microphone.devices[0];
            }
            
            Debug.Log($"Available microphones: {string.Join(", ", Microphone.devices)}");
            Debug.Log($"Selected microphone: {settings.selectedMicrophone}");
        }
        else
        {
            Debug.LogWarning("No microphones detected. Using generated tone only.");
            settings.useMicrophoneInput = false;
            settings.useGeneratedTone = true;
        }
    }
    
    public void StartProcessing()
    {
        if (isProcessing) return;
        
        isProcessing = true;
        
        if (settings.useMicrophoneInput && !string.IsNullOrEmpty(settings.selectedMicrophone))
        {
            StartMicrophoneRecording();
        }
        
        if (settings.useGeneratedTone)
        {
            StartGeneratedTone();
        }
        
        Debug.Log("Audio processing started");
    }
    
    public void StopProcessing()
    {
        if (!isProcessing) return;
        
        isProcessing = false;
        
        if (microphoneClip != null && Microphone.IsRecording(settings.selectedMicrophone))
        {
            Microphone.End(settings.selectedMicrophone);
        }
        
        if (audioSource.isPlaying)
        {
            audioSource.Stop();
        }
        
        Debug.Log("Audio processing stopped");
    }
    
    void StartMicrophoneRecording()
    {
        if (string.IsNullOrEmpty(settings.selectedMicrophone)) return;
        
        // Create microphone clip (1 second duration, looping)
        microphoneClip = Microphone.Start(settings.selectedMicrophone, true, 1, settings.sampleRate);
        
        if (microphoneClip != null)
        {
            Debug.Log("Microphone recording started");
        }
        else
        {
            Debug.LogError("Failed to start microphone recording");
        }
    }
    
    void StartGeneratedTone()
    {
        // Create audio clip for generated tone
        AudioClip toneClip = AudioClip.Create("GeneratedTone", settings.sampleRate, 1, settings.sampleRate, true, OnAudioRead);
        
        audioSource.clip = toneClip;
        audioSource.loop = true;
        audioSource.volume = settings.toneAmplitude;
        audioSource.Play();
        
        Debug.Log($"Generated tone started: {settings.targetFrequency}Hz");
    }
    
    void OnAudioRead(float[] data)
    {
        // Generate sine wave
        for (int i = 0; i < data.Length; i++)
        {
            float sample = Mathf.Sin(2 * Mathf.PI * settings.targetFrequency * tonePhase / toneSampleRate);
            sample *= settings.toneAmplitude;
            
            // Apply envelope curve
            float normalizedTime = (tonePhase / toneSampleRate) % 1.0f;
            float envelope = settings.envelopeCurve.Evaluate(normalizedTime);
            sample *= envelope;
            
            data[i] = sample;
            tonePhase++;
            
            if (tonePhase >= toneSampleRate)
            {
                tonePhase = 0f;
            }
        }
    }
    
    void Update()
    {
        if (isProcessing)
        {
            ProcessAudioData();
            PerformFrequencyAnalysis();
            UpdateAnalysisResults();
        }
    }
    
    void ProcessAudioData()
    {
        // Get audio data from microphone or generated source
        if (settings.useMicrophoneInput && microphoneClip != null)
        {
            ProcessMicrophoneData();
        }
        else if (settings.useGeneratedTone && audioSource.isPlaying)
        {
            ProcessGeneratedToneData();
        }
    }
    
    void ProcessMicrophoneData()
    {
        if (!Microphone.IsRecording(settings.selectedMicrophone)) return;
        
        int currentPosition = Microphone.GetPosition(settings.selectedMicrophone);
        
        if (currentPosition < 0 || currentPosition == lastMicPosition) return;
        
        // Get new audio data
        int samplesAvailable = currentPosition - lastMicPosition;
        if (samplesAvailable < 0)
        {
            samplesAvailable += microphoneClip.samples;
        }
        
        if (samplesAvailable >= settings.bufferSize)
        {
            float[] samples = new float[settings.bufferSize];
            microphoneClip.GetData(samples, lastMicPosition);
            
            System.Array.Copy(samples, audioBuffer, Mathf.Min(samples.Length, audioBuffer.Length));
            
            lastMicPosition = (lastMicPosition + settings.bufferSize) % microphoneClip.samples;
        }
    }
    
    void ProcessGeneratedToneData()
    {
        // For generated tone, we know the exact values
        float currentTime = Time.time;
        for (int i = 0; i < audioBuffer.Length; i++)
        {
            float sampleTime = currentTime + (float)i / settings.sampleRate;
            audioBuffer[i] = Mathf.Sin(2 * Mathf.PI * settings.targetFrequency * sampleTime) * settings.toneAmplitude;
        }
    }
    
    void PerformFrequencyAnalysis()
    {
        if (!settings.enableFFT || audioBuffer == null) return;
        
        // Copy audio buffer to FFT buffer
        System.Array.Copy(audioBuffer, fftBuffer, Mathf.Min(audioBuffer.Length, fftBuffer.Length));
        
        // Get spectrum data using Unity's built-in FFT
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.GetSpectrumData(analysis.spectrumData, 0, settings.fftWindow);
        }
        else if (microphoneClip != null)
        {
            // Alternative FFT for microphone input
            AudioListener.GetSpectrumData(analysis.spectrumData, 0, settings.fftWindow);
        }
        
        // Find dominant frequency
        analysis.dominantFrequency = FindDominantFrequency();
        
        // Calculate frequency bands
        CalculateFrequencyBands();
    }
    
    float FindDominantFrequency()
    {
        if (analysis.spectrumData == null || analysis.spectrumData.Length == 0) return 0f;
        
        int maxIndex = 0;
        float maxValue = 0f;
        
        for (int i = 1; i < analysis.spectrumData.Length; i++)
        {
            if (analysis.spectrumData[i] > maxValue)
            {
                maxValue = analysis.spectrumData[i];
                maxIndex = i;
            }
        }
        
        // Convert bin index to frequency
        float frequencyPerBin = (float)AudioSettings.outputSampleRate / 2f / analysis.spectrumData.Length;
        return maxIndex * frequencyPerBin;
    }
    
    void CalculateFrequencyBands()
    {
        if (analysis.spectrumData == null) return;
        
        int sampleRate = AudioSettings.outputSampleRate;
        int spectrumSize = analysis.spectrumData.Length;
        
        // Calculate frequency per bin
        float freqPerBin = (float)sampleRate / 2f / spectrumSize;
        
        // Define frequency ranges
        int lowFreqEnd = Mathf.FloorToInt(250f / freqPerBin);
        int midFreqEnd = Mathf.FloorToInt(4000f / freqPerBin);
        
        // Sum amplitudes in each band
        analysis.lowFreq = 0f;
        analysis.midFreq = 0f;
        analysis.highFreq = 0f;
        
        for (int i = 0; i < spectrumSize; i++)
        {
            if (i < lowFreqEnd)
                analysis.lowFreq += analysis.spectrumData[i];
            else if (i < midFreqEnd)
                analysis.midFreq += analysis.spectrumData[i];
            else
                analysis.highFreq += analysis.spectrumData[i];
        }
        
        // Normalize by number of bins
        analysis.lowFreq /= lowFreqEnd;
        analysis.midFreq /= (midFreqEnd - lowFreqEnd);
        analysis.highFreq /= (spectrumSize - midFreqEnd);
    }
    
    void UpdateAnalysisResults()
    {
        if (audioBuffer == null) return;
        
        // Calculate RMS and peak values
        float sum = 0f;
        float peak = 0f;
        
        for (int i = 0; i < audioBuffer.Length; i++)
        {
            float sample = Mathf.Abs(audioBuffer[i]);
            sum += sample * sample;
            if (sample > peak) peak = sample;
        }
        
        analysis.rmsValue = Mathf.Sqrt(sum / audioBuffer.Length);
        analysis.peakValue = peak;
        
        // Current amplitude (using RMS)
        analysis.currentAmplitude = analysis.rmsValue;
        
        // Store amplitude history
        amplitudeHistory.Enqueue(analysis.currentAmplitude);
        if (amplitudeHistory.Count > settings.sampleRate * settings.analysisWindow)
        {
            amplitudeHistory.Dequeue();
        }
        
        if (logAudioData)
        {
            Debug.Log($"Amplitude: {analysis.currentAmplitude:F3}, " +
                     $"Dominant Freq: {analysis.dominantFrequency:F1}Hz, " +
                     $"RMS: {analysis.rmsValue:F3}");
        }
    }
    
    // Public API methods
    public float GetCurrentAmplitude()
    {
        return analysis.currentAmplitude;
    }
    
    public float GetDominantFrequency()
    {
        return analysis.dominantFrequency;
    }
    
    public float[] GetSpectrumData()
    {
        return analysis.spectrumData;
    }
    
    public void SetTargetFrequency(float frequency)
    {
        settings.targetFrequency = Mathf.Clamp(frequency, 20f, 20000f);
    }
    
    public void SetAmplitude(float amplitude)
    {
        settings.toneAmplitude = Mathf.Clamp01(amplitude);
        if (audioSource != null)
        {
            audioSource.volume = settings.toneAmplitude;
        }
    }
    
    public float GetAverageAmplitude(float timeWindow = 1.0f)
    {
        if (amplitudeHistory.Count == 0) return 0f;
        
        int samplesToAverage = Mathf.Min(amplitudeHistory.Count, 
            Mathf.FloorToInt(settings.sampleRate * timeWindow));
        
        return amplitudeHistory.TakeLast(samplesToAverage).Average();
    }
    
    void OnGUI()
    {
        if (!showDebugGUI || !Application.isEditor) return;
        
        GUILayout.BeginArea(new Rect(Screen.width - 300, 10, 290, 400));
        GUILayout.BeginVertical("box");
        
        GUILayout.Label("Audio Processor Debug", EditorGUIUtility.GetBuiltinSkin(EditorSkin.Inspector).label);
        
        GUILayout.Space(10);
        GUILayout.Label($"Processing: {isProcessing}");
        GUILayout.Label($"Amplitude: {analysis.currentAmplitude:F3}");
        GUILayout.Label($"Dominant Freq: {analysis.dominantFrequency:F1} Hz");
        GUILayout.Label($"RMS: {analysis.rmsValue:F3}");
        GUILayout.Label($"Peak: {analysis.peakValue:F3}");
        
        GUILayout.Space(10);
        GUILayout.Label("Frequency Bands:");
        GUILayout.Label($"Low (20-250Hz): {analysis.lowFreq:F3}");
        GUILayout.Label($"Mid (250-4000Hz): {analysis.midFreq:F3}");
        GUILayout.Label($"High (4000-20000Hz): {analysis.highFreq:F3}");
        
        GUILayout.Space(10);
        if (settings.useGeneratedTone)
        {
            GUILayout.Label($"Target Freq: {settings.targetFrequency:F0} Hz");
            GUILayout.Label($"Tone Amplitude: {settings.toneAmplitude:F2}");
        }
        
        if (settings.useMicrophoneInput)
        {
            GUILayout.Label($"Microphone: {settings.selectedMicrophone}");
            GUILayout.Label($"Recording: {(microphoneClip != null && Microphone.IsRecording(settings.selectedMicrophone))}");
        }
        
        GUILayout.EndVertical();
        GUILayout.EndArea();
    }
    
    void OnDestroy()
    {
        StopProcessing();
    }
}