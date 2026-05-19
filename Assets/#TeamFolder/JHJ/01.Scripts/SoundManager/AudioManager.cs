using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks.Triggers;
using PYH.Util;
using UnityEngine;

[Serializable]
public struct SoundSettingData
{
    public float masterVolume;
    public float bgmVolume;
    public float sfxVolume;
}

public enum SoundType
{
    Master,
    BGM,
    SFX
}

[Serializable]
public struct AudioSetting
{
    public AudioSetting(AudioClip clip, string key, bool loop, float volume, float pitch, bool startSound, SoundType type)
    {
        this.soundType = type;
        this.clip = clip;
        this.name = key;
        this.loop = loop;
        this.volume = volume;
        this.pitch = pitch;
        this.startSound = startSound;
    }
    public SoundType soundType;
    public AudioClip clip;
    public string name;
    public bool loop;
    [Range(0f, 1f)] public float volume;
    [Range(-3f, 3f)] public float pitch;
    public bool startSound;
}

[DefaultExecutionOrder(-1)]
public class AudioManager : MonoSingleton<AudioManager>
{
    private Dictionary<string, (AudioClip clip, AudioSource source)> _audios =
        new Dictionary<string, (AudioClip, AudioSource)>();

    private Dictionary<SoundType, float> _volume = new Dictionary<SoundType, float>();

    [SerializeField] private SoundDataSO _audioDataSO;
    private List<AudioSetting> _audioSettings;
    public float MasterVolume { get => _volume[SoundType.Master]; set => _volume[SoundType.Master] = value; }
    public float BGMVolume { get => _volume[SoundType.BGM]; set => _volume[SoundType.BGM] = value; }
    public float SFXVolume { get => _volume[SoundType.SFX]; set => _volume[SoundType.SFX] = value; }

    protected override void Awake()
    {
        base.Awake();

        if (_audioDataSO != null)
            _audioSettings = _audioDataSO.audioSettings;

        if (_audioSettings != null && _audioSettings.Count != 0)
        {
            foreach (AudioSetting setting in _audioSettings)
                AddAudio(setting);
        }

        _volume.Add(SoundType.Master, 1);
        _volume.Add(SoundType.BGM, 1);
        _volume.Add(SoundType.SFX, 1);

        Debug.Assert(_audioDataSO != null, "<color=red>AudioDataSO is null</color>");
        Debug.Assert(_audioSettings != null && _audioSettings?.Count != 0, "<color=red>AudioSettings is null</color>");
    }


    public void SetVolume(SoundSettingData soundSettingData)
    {
        MasterVolume = soundSettingData.masterVolume;
        BGMVolume = soundSettingData.bgmVolume;
        SFXVolume = soundSettingData.sfxVolume;
    }

    private void Start()
    {
        if (_audioSettings != null)
        {
            foreach (var setting in _audioSettings)
            {
                if (setting.startSound)
                    PlaySound(setting.name);
            }
        }

        ApplyVolumeToAllSources();
    }

    protected void OnDestroy()
    {
        
    }


    private void AddAudio(AudioSetting setting)
    {
        var key = setting.name;
        var clip = setting.clip;
        var source = gameObject.AddComponent<AudioSource>();

        if (clip != null && source != null)
        {
            source.clip = setting.clip;
            source.volume = setting.volume;
            source.pitch = setting.pitch;
            source.loop = setting.loop;

            _audios.Add(key, (clip, source));
        }
        else
        {
            Debug.Log("<color=red>This audio setting is null</color>");
        }
    }

    public void PlaySound(string name)
    {
        var clip = _audios[name].clip;
        var source = _audios[name].source;

        if (clip != null && source != null)
            source.PlayOneShot(clip);
        else
            Debug.Log("<color=red>This audio is not initialized</color>");
    }

    public void PlaySound(string name, float volume = 1f, float pitch = 1f)
    {
        var clip = _audios[name].clip;
        var source = _audios[name].source;

        if (clip != null && source != null)
        {
            var setting = _audioSettings.Find(x => x.name == name);

            float finalVolume = setting.volume * volume * _volume[setting.soundType] * _volume[SoundType.Master];

            source.pitch = pitch;
            source.PlayOneShot(clip, finalVolume);
        }
        else
        {
            Debug.Log("<color=red>This audio is not initialized</color>");
        }
    }
    private void ApplyVolumeToAllSources()
    {
        foreach (var kvp in _audios)
        {
            string key = kvp.Key;
            AudioSource source = kvp.Value.source;

            var setting = _audioSettings.Find(x => x.name == key);

            float baseVolume = setting.volume;
            float typeVolume = _volume[setting.soundType];
            float masterVolume = _volume[SoundType.Master];

            float finalVolume = baseVolume * typeVolume * masterVolume;

            source.volume = finalVolume;
        }
    }

    public void VolumeChanger(float value, SoundType type)
    {
        value = Mathf.Clamp(value, 0f, 1f);

        switch (type)
        {
            case SoundType.Master:
                MasterVolume = value; break;
            case SoundType.BGM:
                BGMVolume = value; break;
            case SoundType.SFX:
                SFXVolume = value; break;
        }

        ApplyVolumeToAllSources();
    }
}