using UnityEngine;

public class Effecter : MonoBehaviour
{
    [SerializeField] private ParticleSystem[] particles;

    private int count;
    
    private void Awake()
    {
        for (int i = 0; i < particles.Length; i++)
        {
            var main = particles[i].main;
            main.stopAction = ParticleSystemStopAction.Callback;
        }
    }

    public void ParticleTrigger()
    {
        foreach (var particle in particles)
        {
            particle.Play();
        }
    }
    private void OnParticleSystemStopped()
    {
        count++;

        if (count >= particles.Length)
        {
            count = 0;
            gameObject.SetActive(false);
        }
    }
}
