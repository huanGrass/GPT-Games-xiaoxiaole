using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public AudioSource audioSourcePrefab; // AudioSource Ԥ����
    public int poolSize = 10; // ��Ч�ش�С
    private Queue<AudioSource> audioSources = new Queue<AudioSource>();

    void Start()
    {
        // ��ʼ����Ч��
        for (int i = 0; i < poolSize; i++)
        {
            AudioSource source = Instantiate(audioSourcePrefab, transform);
            source.gameObject.SetActive(false);
            audioSources.Enqueue(source);
        }
    }

    public void PlaySound(AudioClip clip)
    {
        AudioSource source;
        if (audioSources.Count > 0)
        {
            source = audioSources.Dequeue();
        }
        else
        {
            // ��Ч�ؿ��ˣ���̬�����µ� AudioSource
            source = Instantiate(audioSourcePrefab, transform);
        }

        source.clip = clip;
        source.gameObject.SetActive(true);
        source.Play();
        StartCoroutine(ReturnToPool(source, clip.length));
    }

    private IEnumerator ReturnToPool(AudioSource source, float delay)
    {
        yield return new WaitForSeconds(delay);
        source.gameObject.SetActive(false);
        audioSources.Enqueue(source);
    }
}
