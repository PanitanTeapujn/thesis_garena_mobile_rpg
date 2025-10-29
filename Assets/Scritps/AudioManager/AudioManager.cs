using UnityEngine;
using UnityEngine.Audio;
public class AudioManager : MonoBehaviour
{
    [SerializeField]
    private AudioSource[] bgm;
    public AudioSource[] Bgm { get { return bgm; } }

    [SerializeField]
    private AudioSource[] sfx;
    public AudioSource[] Sfx { get { return sfx; } }

    [SerializeField]
    private AudioMixer audioMixer;

    public static AudioManager instance;
    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        DontDestroyOnLoad(gameObject);

        instance = this;
    }
    void Start()
    {
        PlayBGM(0);
    }

    // Update is called once per frame
    void Update()
    {

    }
    private void StopAllBGM()
    {
        for (int i = 0; i < bgm.Length; i++)
            bgm[i].Stop();
    }

    public void PlayBGM(int i)
    {
        if (!Bgm[i].isPlaying)
        {
            StopAllBGM();
            if (i < Bgm.Length)
                Bgm[i].PlayDelayed(2f);

        }
    }

    public void PlaySFX(int i)
    {
        if (i < sfx.Length && !sfx[i].isPlaying)
            sfx[i].Play();
    }
    public void SetMasterVolume(float volume)
    {
        audioMixer.SetFloat("MasterVolume", Mathf.Log10(volume) * 20); // Volume: 0.0001 - 1.0
    }

    public void SetBGMVolume(float volume)
    {


        audioMixer.SetFloat("BGMVolume", Mathf.Log10(volume) * 20);
    }


    public void SetSFXVolume(float volume)
    {
        audioMixer.SetFloat("SFXVolume", Mathf.Log10(volume) * 20);
    }

}