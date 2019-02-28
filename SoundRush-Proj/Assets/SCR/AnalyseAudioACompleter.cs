using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class AnalyseAudioACompleter : MonoBehaviour {
	
	// COMPOSANT UNITY
	AudioSource audioSrc; 		
	ParticleSystem.Particle[] points;
    ParticleSystem partSystem;
    float[] trame;
    Camera thecamera;
    Color backgroundcolor_init;


    // VARIABLE D'ANALYSE
    public int NB_SAMPLES = 4096;       // QUANTIFICATION
    /*AudioSettings.outputSampleRate*/  // ECHANTILLONAGE

    // AFFICHAGE TEXTE
    public Text _textPitch;
    public Text _textPeriode;
    public Text _textVolume;

    // GESTION MOYENNE PAR X TRAMES
    Queue<float> QueueMoyenne;
    public int QueueMoyenne_Length;

    // VAR VALEUR DB & FREQ
    int f0_index = 0;
    float db_display = 0f;
    float db_value=0f;
    
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

    //AFFICHAGE PITCH
    public float coef_PitchDisplay = 0.1f;
    public float min_PitchDisplay = -10f;
    bool PitchTrouvee;

    //AFFICHAGE DB
    public float coef_DBDisplay = 1/8f;
    public float min_DBDisplay = 0;
    public GameObject CubeVolume;

    // ===============================================
    // =========== METHODES START ET UPDATE ==========
    // ===============================================

    // Use this for initialization
    // ----------------------------
    void Start () {

        QueueMoyenne = new Queue<float>();
        thecamera = Camera.main;
        backgroundcolor_init = thecamera.backgroundColor;
        trame = new float[NB_SAMPLES];
		audioSrc = GetComponent<AudioSource> (); 
		StartMicListener();
        partSystem = GetComponent<ParticleSystem>();
        //
        cubesFreq = new List<GameObject>();
        GameObject go;
        for (int i = 0; i < nbCubesFreq_todisplay; i++)
        {
            go = Instantiate(gop_cubeFreq, go_cubeFreq_container.transform);
            go.transform.localPosition = new Vector3(i * cubesFreqGap - nbCubesFreq_todisplay/2* cubesFreqGap, 0, 0);
            cubesFreq.Add(go);

        }

		// Initialise le système de particules en les plaçant sur l'axe des x entre 0 et 5
		points = new ParticleSystem.Particle[NB_SAMPLES];
		for (int i = 0; i < NB_SAMPLES; i++) {
			float x = 5f* (float) (i) / (float) (NB_SAMPLES);
			points[i].position = new Vector3(x, 0f, 0f);
			points[i].color = new Color(x, 0f, 0f);
			points[i].size = 0.1f;
		}
	}
	
	// Update is called once per frame
	// -------------------------------
	void Update () {
		// If the audio has stopped playing, this will restart the mic play the clip.
		if (!audioSrc.isPlaying) StartMicListener();
		
		votreFonction ();
        return ;
		plotTab (trame, 100);

	}

	// ===============================================
	// ============== AUTRES METHODES ================
	// ===============================================

	// Starts the Mic, and plays the audio back in (near) real-time.
	// --------------------------------------------------------------
	private void StartMicListener() {
		if (audioSrc.clip == null) {
			audioSrc.clip = Microphone.Start ("Built-in Microphone", true, 999, AudioSettings.outputSampleRate);
			// HACK - Forces the function to wait until the microphone has started, before moving onto the play function.
			while (!(Microphone.GetPosition("Built-in Microphone") > 0)) {
			} audioSrc.Play ();
		}
	}

    // Votre Fonction
    // -------------------------------
    private void votreFonction()
    {
        float newMoyenneDeTrame = 0f;
        float somme = 0f;
        float newRMS = 0f;
        float newMoyenneGlissante;
        audioSrc.GetOutputData(trame, 0);

        //GET MOY
        foreach (float sample in trame) somme += sample;
        newMoyenneDeTrame = somme / NB_SAMPLES;

        //RMS
        newRMS = Mathf.Sqrt(newMoyenneDeTrame * newMoyenneDeTrame);
        QueueMoyenne.Enqueue(newRMS);

        //AUTO_CORRELATION
        autoCorrelation(trame);

        //get Spectrum data
        audioSrc.GetSpectrumData(trame, 0, FFTWindow.Hamming);
        List<float> spectrum = trame.ToList();
        int newf0 = spectrum.IndexOf(Mathf.Max(spectrum.ToArray()));
        if (Mathf.Abs(newf0- f0_index)<300)//newf0 > 12 && newf0 < 500)
        {
            f0_index = newf0;
        }

        //VISION DE SPECTRE
        spectrumVision(spectrum);


        //GET MOYENNE GLISSANTE FILTRE par FILE 
        //SI QUEUE.count EXCEED = ON A SUFFISEMMENT ECHANTILLONEE POUR TRAITER l'AFFICHAGE DU SIGNAL
        if (QueueMoyenne.Count >= QueueMoyenne_Length)
        {
            //RecupMoyenne : RMS
            newMoyenneGlissante = 0f;
            foreach (float val in QueueMoyenne) newMoyenneGlissante += val;
            newMoyenneGlissante /= QueueMoyenne_Length;
            db_value = newMoyenneGlissante;
            QueueMoyenne.Dequeue();

            //GET VOLUME : DB
            float REFVALUE = 0.0045f;
            db_display = 20 * Mathf.Log(newMoyenneGlissante  / REFVALUE);

            //RAFFRACHIS L'AFFICHAGE DES CUBES
            UpdateAffichageCube();
        }

        // AFFICHAGE TEXTE
        UpdateAffichageTexte();
    }

    void UpdateAffichageTexte()
    {
        if (PitchTrouvee) _textPitch.text = (int)(GetFrequenceParIndexDeTrameEchantillonnee(f0_index, NB_SAMPLES, AudioSettings.outputSampleRate)) + "Hz"; //
        _textVolume.text = (int)(db_value * 10000) + "dB";
    }
    
    /// <summary>
    /// Affiche la hauteur des cubes :
    /// Rouge = Pitch
    /// Bleu = Volume
    /// </summary>
    void UpdateAffichageCube()
    {
        transform.position = new Vector3(transform.position.x, f0_index * coef_PitchDisplay + min_PitchDisplay, transform.position.z);
        if (!float.IsInfinity(db_display)) CubeVolume.transform.position = new Vector3(transform.position.x, db_display * coef_DBDisplay + min_DBDisplay, transform.position.z);
    }

    /// <summary>
    /// Recupère par l'index d'une trame, sa valeur en frequence en fonction du nombre d'échantillon et la fréquence d'échantillonnage.
    /// RELATION : fundamental_freq = (fundamental_index)/N * sample_freq; // convert from raw index to Hz
    /// </summary>
    /// <param name="IndexEchantillon"></param>
    /// <param name="NombreEchantillonDeTrame"></param>
    /// <param name="FrequenceEchantillonnageDeTrame"></param>
    /// <returns></returns>
    float GetFrequenceParIndexDeTrameEchantillonnee(int IndexEchantillon,int NombreEchantillonDeTrame,int FrequenceEchantillonnageDeTrame)
    {
        return IndexEchantillon / (NombreEchantillonDeTrame / 2f) * FrequenceEchantillonnageDeTrame;
    }

    // Plot un tableau de taille NB_SAMPLES avec le système de particules
    private void plotTab(float[] tab, int gain) {
		for (int i = 0; i < NB_SAMPLES; i++) {
			Vector3 p = points [i].position;
			p.y = tab [i]*gain + 1f;
			points [i].position = p;
			points [i].color = new Color (1f, 0f, 0f);
			points [i].size = 0.1f;
		}
		GetComponent<ParticleSystem> ().SetParticles (points, points.Length);
	}

    /// <summary>
    /// On applique l'autocorrelation Rxx(trame). On superpose la trame avec elle-même 
    /// et on applique un calcul avec un retard (tau) : 
    /// 
    ///     trameAutoCorrelee = SOMME de 0 à n-tau ( trame[indice(n)] * trame[indice(n)+tau] ).
    ///     
    /// On sait alors que la convolution avec elle-même donne une fonction périodique et que trameAutoCorrelee = Max(trameAutoCorrelee)
    /// et que le second pique de frequence donne la période.
    /// 
    /// La période est calculé si et seulement si la le second pic de la trame récupéré est égale à 55% du pic en 0.
    /// Si cette condition est rempli, on récupère la fréquence est on l'affiche en texte. Ceci permet aussi de
    /// révéler que la valeur pitch est calculable.
    /// </summary>
    /// <param name="trame"></param>
    void autoCorrelation(float[] trame)
    {
        float tmp;
        List<float> trameList = trame.ToList();
        List<float> tauTrameAutoCorrelation = new List<float>();
        for (int tau = 0; tau < trame.Length-1; tau++)
        {
            tmp = 0;
            for (int i = 0; i < trame.Length-tau; i++)
            {
                tmp += trameList[i] * trameList[(i + tau)];
            }
            tauTrameAutoCorrelation.Add(tmp);
        }


        bool PremierNegatif = true;
        float diff = -1;
        int indexTmp=0;
        int SecondPic_index = 0;

        while (indexTmp < tauTrameAutoCorrelation.Count - 1)
        {
            diff = tauTrameAutoCorrelation[indexTmp + 1] - tauTrameAutoCorrelation[indexTmp];
            if (diff > 0 && PremierNegatif) PremierNegatif = false;
            else if (diff < 0 && !PremierNegatif)
            {
                SecondPic_index = indexTmp;
                break;
            }
            indexTmp++;
        }

        //Si un second pic est trouvé et que sa valeur est égale à 55% du pic en 0
        if (SecondPic_index != 0 && !PremierNegatif && tauTrameAutoCorrelation[0]*0.55f <= tauTrameAutoCorrelation[SecondPic_index])
        {
            PitchTrouvee = true;
            _textPeriode.text = "Période = " + SecondPic_index + " tau";
        }
        else
        {
            PitchTrouvee = false;
        }

    }


    /// <summary>
    /// Dispositif d'affichage du spectre en (nbCubesFreq_todisplay) = nombre de pics du spectre.
    /// J'integre en plus un affichage moyenné avec une plage de fréquence configurable.
    /// </summary>
    /// <param name="spectrum"></param>
    void spectrumVision(List<float> spectrum)
    {
        int plageAMoyenner = nbCubesFreq_maxDisplay - nbCubesFreq_minDisplay;
        int nbTomoyenner = (int)System.Math.Round( plageAMoyenner / (double)nbCubesFreq_todisplay,0);
        float tmp_moyenne;
        for (int i = 0; i < nbCubesFreq_todisplay; i++)
        {
            tmp_moyenne = 0;
            for (int j = nbCubesFreq_minDisplay+(i) * nbTomoyenner; j < nbCubesFreq_minDisplay+((i) + 1) * nbTomoyenner; j++)
            {
                tmp_moyenne += spectrum[j];
            }
            tmp_moyenne /= nbCubesFreq_todisplay;
            cubesFreq[i].transform.localScale = new Vector3(cubesFreqSizeX, 0.1f, tmp_moyenne * cubesFreqSizeZ);
        }
    }
}
