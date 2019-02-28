using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class Sequencer : MonoBehaviour
{
    //STATES
    bool playing { get { return soundtrack.isPlaying; } }
    bool rewinding;
    bool starting;
    bool hasStarted;
    bool winner;

    bool inputActive;

    public AudioSource soundtrack;
    public AudioSource feedback_OK;
    public AudioSource feedback_NOK;
    public AudioSource feedback_HardcoreLaunch;

    public float actualspeed = 1;
    public float targetspeed = 1;
    public float lerpspeed = 0.75f;
    public Coroutine actualbeatcoroutine;
    public int actualsequence_index;

    public int lastcheckpoint = 0;

    public Text dbg_speed;
    public Text dbg_targetspeed;

    public Text txt_startercount;

    public GameObject line;
    public GameObject player;
    public GameObject sample;
    public RectTransform samplerect;
    public RectTransform playerrect;
    public float targetsampleoffset = 0.0f;
    public float actualsampleoffset = 0.0f;
    public float lerpsample = 0.8f;

    public GameObject pnl_start;
    public GameObject pnl_darkbg;
    public GameObject pnl_perdre;
    public GameObject pnl_gagner;


    public float cursorSpeed = 5000;
    public float xMin = 202.5f;
    public float xMax = 992.5f;
    public float lineHalfSize;

    public GameObject BG_SPHERE;

    public float timestartplay;

    public float rewindtime = 5.0f;
    public float rewindspeed = 1f;
    public float rewindtargettime;

    public bgcolor bgmanager;

    void Start()
    {
        StartCoroutine(BackgroundCoroutine());
        //█████████████████
        trame = new float[NB_SAMPLES];
        audioSrc = GetComponent<AudioSource>();
        //
        cubesFreq = new List<GameObject>();
        GameObject go;
        for (int i = 0; i < nbCubesFreq_todisplay; i++)
        {
            go = Instantiate(gop_cubeFreq, go_cubeFreq_container.transform);
            go.transform.localPosition = new Vector3(i * cubesFreqGap - nbCubesFreq_todisplay / 2 * cubesFreqGap, 0, 0);
            cubesFreq.Add(go);

        }

        //█████████████████

        lineHalfSize = (Screen.width - 2.0f * line.GetComponent<RectTransform>().offsetMin.x)/2.0f;
        xMin = -lineHalfSize;
        xMax = lineHalfSize;
        samplerect = sample.GetComponent<RectTransform>();
        playerrect = player.GetComponent<RectTransform>();
        samplerect.localPosition = new Vector3(0.0f, samplerect.localPosition.y, samplerect.localPosition.z);
        playerrect.localPosition = new Vector3(0.0f, playerrect.localPosition.y, playerrect.localPosition.z);

        pnl_darkbg.SetActive(true);
        pnl_start.SetActive(true);
        print("READY");
    }

    public void BTN_Start()
    {
        print("STARTING");
        winner = false;
        hasStarted = false;
        inputActive = false;
        pnl_start.SetActive(false);
        pnl_gagner.SetActive(false);
        sample.SetActive(true);
        timestartplay = Time.timeSinceLevelLoad;
        StartCoroutine(StartSoundtrack());
    }

    public void BTN_Start_hardcore()
    {
        print("STARTING HARDCORE");
        winner = false;
        hasStarted = false;
        inputActive = false;
        pnl_start.SetActive(false);
        pnl_gagner.SetActive(false);
        sample.SetActive(false);
        timestartplay = Time.timeSinceLevelLoad;
        feedback_HardcoreLaunch.Play();
        StartCoroutine(StartSoundtrack());
    }

    private void Rewind()
    {
        hasStarted = false;
        inputActive = false;
        // FAIRE FAST REWARD
        if (soundtrack.time > rewindtime)
        {
            //rewind
            print("REWINDING");
            rewindtargettime = soundtrack.time - rewindtime;
            rewinding = true;
            soundtrack.pitch = -0.2f;
        }
        else
        {
            soundtrack.Stop();
            //on rejoue depuis le debut
            print("RESTART DIRECTLY");
            StartCoroutine(StartSoundtrack());
        }
    }

    private IEnumerator StartSoundtrack()
    {
        pnl_perdre.SetActive(false);
        samplerect.localPosition = new Vector3(0.0f, samplerect.localPosition.y, samplerect.localPosition.z);
        playerrect.localPosition = new Vector3(0.0f, playerrect.localPosition.y, playerrect.localPosition.z);
        ResetSoundtrackSetting();
        int c = 5;
        while (c > 0)
        {
            txt_startercount.text = c + "";
            print(c + "...");
            yield return new WaitForSeconds(1f);
            c--;
        }
        txt_startercount.text = "";
        pnl_darkbg.SetActive(false);
        soundtrack.Play();
        actualbeatcoroutine = StartCoroutine(BeatCoroutine());
        inputActive = true;
        hasStarted = true;
    }


    private IEnumerator RestartSoundtrack()
    {
        pnl_perdre.SetActive(false);
        playerrect.localPosition = new Vector3(getXSliderOffset(0.0f), playerrect.localPosition.y, playerrect.localPosition.z);
        targetsampleoffset = 0.0f;
        actualsampleoffset = 0.0f;
        samplerect.localPosition = new Vector3(getXSliderOffset(0.0f), samplerect.localPosition.y, samplerect.localPosition.z);

        ResetSoundtrackSetting();
        int c = 3;
        while (c > 0)
        {
            txt_startercount.text = c + "";
            print(c + "...");
            yield return new WaitForSeconds(1f);
            c--;
        }
        txt_startercount.text = "";
        pnl_darkbg.SetActive(false);
        actualbeatcoroutine = StartCoroutine(BeatCoroutine());
        inputActive = true;
        hasStarted = true;
    }

    // Update is called once per frame
    void Update()
    {
        if (playing)
        {
            moveCursor();

            //lerp music speed to target
            actualspeed = Mathf.Lerp(actualspeed, targetspeed, lerpspeed * Time.deltaTime);
            dbg_speed.text = "SPEED : " + limitString(actualspeed.ToString(), 5);
            dbg_targetspeed.text = "TARGET : " + limitString(targetspeed.ToString(), 5);
            soundtrack.pitch = actualspeed;

            // LERP BALANCE AUDIO
            actualsampleoffset = Mathf.Lerp(actualsampleoffset, targetsampleoffset, lerpsample * Time.deltaTime);
            samplerect.localPosition = new Vector3(getXSliderOffset(actualsampleoffset), samplerect.localPosition.y, samplerect.localPosition.z);
            soundtrack.panStereo = actualsampleoffset;

            BG_SPHERE.transform.Rotate(Vector3.right, Mathf.Pow(actualspeed,15.0f) * 0.1f);

            if (rewinding)
            {
                if (soundtrack.time < rewindtargettime)
                {
                    print("REWIND FINISHED");
                    rewinding = false;
                    timestartplay += rewindtime*3;
                    StartCoroutine(RestartSoundtrack());
                    targetspeed = 1f;
                    actualspeed = 1f;
                    soundtrack.pitch = 1f;
                }
                else
                {

                    targetspeed -= rewindspeed * Time.deltaTime;
                }
                //TODO
            }
            if (soundtrack.time > soundtrack.clip.length * 0.98f)
            {
                winner = true;
            }
            //
            SpectrumExtract();
        }
    }


    public float getXSliderOffset(float x) // x: [-1.0f:1.0f]
    {
        return lineHalfSize * x;
    }

    string limitString(string str, int nbchar)
    {
        return str != null && str.Trim().Length >= 5 ? str.Trim().Substring(0, 5) : str;
    }



    void setPerdu()
    {
        print("PERDU");
        ResetSoundtrackSetting();
        pnl_darkbg.SetActive(true);
        pnl_perdre.SetActive(true);
        StopCoroutine(BeatCoroutine());
        Rewind();
    }
    void setGagnee()
    {
        print("GAGNEE");
        soundtrack.Stop();
        ResetSoundtrackSetting();
        pnl_darkbg.SetActive(true);
        pnl_gagner.SetActive(true);
    }

    void ResetSoundtrackSetting()
    {
        soundtrack.pitch = 1.0f;
        soundtrack.panStereo = 0.0f;
        //soundtrack.volume = 1.0f;
        targetspeed = 1f;
        actualspeed = 1f;
        soundtrack.pitch = 1f;
    }

    void moveCursor()
    {
        if (Input.GetKey(KeyCode.RightArrow))
        {
            playerrect.localPosition = new Vector3(
                (xMax - 1),
                playerrect.localPosition.y, playerrect.localPosition.z);
        }
        if (Input.GetKey(KeyCode.LeftArrow))
        {
            playerrect.localPosition = new Vector3(
                ( xMin + 1),
                playerrect.localPosition.y, playerrect.localPosition.z);
        }
    }

    private IEnumerator BeatCoroutine()
    {
        print("COROUTINE START = BeatCoroutine");
        while (playing && !rewinding)
        {
            if (Mathf.Abs(playerrect.localPosition.x - samplerect.localPosition.x) < 100)
            {
                //NOTE CORRECTE
                targetspeed += 0.05f;
                targetspeed = Mathf.Min(1f+ (Time.timeSinceLevelLoad - timestartplay) * 0.001f, targetspeed);
                feedback_OK.Play();
            }
            else
            {
                //NOTE INCORRECTE
                targetspeed -= 0.1f;
                feedback_NOK.Play();
                if (targetspeed < 0.8f && hasStarted && !rewinding)
                {
                    //PERDU !
                    targetspeed = 0.2f;
                    setPerdu();
                }
            }
            //player.transform.position = new Vector3(getXSliderOffset(0.0f), player.transform.position.y);
            yield return new WaitForSeconds(Mathf.Abs(2.0f / (actualspeed * ((Time.timeSinceLevelLoad - timestartplay) * 0.010f + 0.9f))));
            targetsampleoffset = Random.Range(-1.0f, 1.0f) > 0.0f ? 1.0f : -1.0f;
        }

        if(winner)
        {
            setGagnee();
        }
    }




    // COMPOSANT UNITY
    AudioSource audioSrc;
    float[] trame;

    public int NB_SAMPLES = 4096;       // QUANTIFICATION

    // VAR VALEUR DB & FREQ
    int f0_index = 0;
    float db_display = 0f;
    float db_value = 0f;

    //AFFICHAGE SPECTRE - CONFIG VISUELLE
    public int nbCubesFreq_todisplay = 32; //puissance de 2
    public float cubesFreqGap = 1.05f;
    public float cubesFreqSizeX = 1f;
    public float cubesFreqSizeZ = 200f;
    public int nbCubesFreq_minDisplay = 0;
    public int nbCubesFreq_maxDisplay = 256;
    // IDM
    public GameObject gop_cubeFreq;
    public GameObject go_cubeFreq_container;
    List<GameObject> cubesFreq;

    // Votre Fonction
    // -------------------------------
    private void SpectrumExtract()
    {
        int channel = Mathf.FloorToInt((targetsampleoffset + 1.0f) / 2.0f + 0.5f);
        //print("channel: " + channel);
        audioSrc.GetOutputData(trame, 0);

        //get Spectrum data
        audioSrc.GetSpectrumData(trame, channel, FFTWindow.Hamming);

        spectrumVision(trame);
    }

    /// <summary>
    /// Dispositif d'affichage du spectre en (nbCubesFreq_todisplay) = nombre de pics du spectre.
    /// J'integre en plus un affichage moyenné avec une plage de fréquence configurable.
    /// </summary>
    /// <param name="spectrum"></param>
    void spectrumVision(float[] spectrum)
    {
        int plageAMoyenner = nbCubesFreq_maxDisplay - nbCubesFreq_minDisplay;
        int nbTomoyenner = (int)System.Math.Round(plageAMoyenner / (double)nbCubesFreq_todisplay, 0);
        float tmp_moyenne;
        for (int i = 0; i < nbCubesFreq_todisplay; i++)
        {
            tmp_moyenne = 0;
            for (int j = nbCubesFreq_minDisplay + (i) * nbTomoyenner; j < nbCubesFreq_minDisplay + ((i) + 1) * nbTomoyenner; j++)
            {
                tmp_moyenne += spectrum[j];
            }
            tmp_moyenne /= nbTomoyenner;
            cubesFreq[i].transform.localScale = new Vector3(cubesFreqSizeX, 0.1f, tmp_moyenne * cubesFreqSizeZ);
        }
    }



    private IEnumerator BackgroundCoroutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(1f);
            bgmanager.SetTargetColors(Random.ColorHSV(0f, 1f, 0.2f, 0.6f, 0.7f, 0.8f), Random.ColorHSV(0f, 1f, 0.2f, 0.6f, 0.7f, 0.8f));
        }
    }

}

