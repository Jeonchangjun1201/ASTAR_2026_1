using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "JHJSoundDataSO", menuName = "JHJAdudioSO/AudioDataSO")]
public class SoundDataSO : ScriptableObject
{
    public List<AudioSetting> audioSettings;

    void OnValidate()
    {
        List<AudioSetting> modifiedSettings = new List<AudioSetting>(audioSettings);
        if (audioSettings != null)
        {
            foreach (var audioSetting in audioSettings)
            {
                int index = audioSettings.IndexOf(audioSetting);

                if (audioSetting.clip != null)
                {
                    AudioClip clip = audioSetting.clip;
                    string key = audioSetting.clip.name;
                    bool loop = audioSetting.loop;
                    float volume = audioSetting.volume;
                    float pitch = audioSetting.pitch;
                    bool startSound = audioSetting.startSound;
                    SoundType type = audioSetting.soundType;

                    AudioSetting modifiedSetting = new AudioSetting(clip, key, loop, volume, pitch, startSound, type);
                    modifiedSettings[index] = modifiedSetting;
                }
            }

            audioSettings = modifiedSettings;
        }
    }
}