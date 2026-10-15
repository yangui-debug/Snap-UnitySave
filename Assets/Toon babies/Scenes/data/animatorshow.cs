using System.Collections;
using System.Collections.Generic;
using UnityEngine;



namespace ToonBabies
{

    public class animatorshow : MonoBehaviour
    {
        public GameObject[] Baby;
        string[] animations;
        GameObject RandomBaby;
        GameObject RandomBaby2;
        int animN;
        int set = 0;        
        bool rootON;
        public Texture[] texts;
        public GUIStyle newGUIStyle;
        public bool showUI;
        float Cturn;
        Vector3 Cam1transPos;
        Quaternion Cam1transRot;
        Vector3 Cam2transPos;
        Quaternion Cam2transRot;


        void Start()
        {
            if (transform.childCount > 0) Destroy(transform.GetChild(0).gameObject);
            rootON = false;
            animN = 0;
            set = 0;
            if (set == 0) animations = new string[33] { "idle1","idle2","idle3","idle4", "idlehappy" , "idlesad", "idleangry", "idleamazed", "idletired", "idlesuck","clap", "laugh", "cry",
                                                        "walk1","walk2","walkbackwards", "walkstrafeR", "walkstrafeL", "run1", "run2", "runR", "runL", "runstrafeR", "runstrafeL",
                                                         "jump", "runjumpIN", "freefall", "turnR45","turnR90", "turnL45", "turnL90", "fallforwardIN", "fallbackwardsIN" };

            RandomBaby = Instantiate(Baby[0]);
            RandomBaby.transform.position += transform.right * 0.2f;
            RandomBaby.transform.parent = transform;
            RandomBaby2 = Instantiate(Baby[1]);
            RandomBaby2.transform.position -= transform.right * 0.2f;
            RandomBaby2.transform.parent = transform;

            RandomBaby.GetComponent<TBabyPrefabMaker>().Getready();
            RandomBaby.GetComponent<TBabyPrefabMaker>().Randomize();
            RandomBaby2.GetComponent<TBabyPrefabMaker>().Getready();
            RandomBaby2.GetComponent<TBabyPrefabMaker>().Randomize();
            
            RandomBaby.GetComponent<Animator>().applyRootMotion = false;
            RandomBaby.GetComponent<Playanimation>().enabled = false;
            RandomBaby.GetComponent<Animator>().Play("TB_" + animations[0]);
            RandomBaby2.GetComponent<Animator>().applyRootMotion = false;
            RandomBaby2.GetComponent<Playanimation>().enabled = false;
            RandomBaby2.GetComponent<Animator>().Play("TB_" + animations[0]);

            Cam1transPos = Camera.main.transform.position;
            Cam1transRot = Camera.main.transform.rotation;
            Cam2transPos = new Vector3(0f, 1.525f, -0.31f);
            Cam2transRot = Quaternion.Euler(new Vector3(107.412f, 180f, 180f));
        }

        void Update()
        {
            if (Input.GetKeyDown("w")) changeset(1);
            if (Input.GetKeyDown("s")) changeset(-1);


            if (Input.GetKeyDown("d"))
            {
                changecharacter();
                animN++;
                changeanimation();
            }
            if (Input.GetKeyDown("a"))
            {
                changecharacter();
                animN--;
                changeanimation();
            }
            if (Input.GetKeyDown("space")) changecharacter();
            if (Input.GetKeyDown("r")) activeroot();
            if (Input.GetKeyDown("x")) showUI = !showUI;
            if (Input.GetKeyDown("left")) { Cturn += 90; turncharacter(); }
            if (Input.GetKeyDown("right")) { Cturn -= 90; turncharacter(); }
        }

        void changeanimation()
        {
            if (animN > animations.Length - 1) animN = 0;
            else if (animN < 0) animN = animations.Length -1 ;

            RandomBaby.GetComponent<Playanimation>().enabled = false;
            RandomBaby.GetComponent<Animator>().Play("TB_" + animations[animN]);
            RandomBaby2.GetComponent<Playanimation>().enabled = false;
            RandomBaby2.GetComponent<Animator>().Play("TB_" + animations[animN]);
        }

        void changeset(int nextset)
        {
            set += nextset;
            if (set > 4) set = 0;
            else if (set < 0) set = 4;

            if (set == 0) animations = new string[33] { "idle1","idle2","idle3","idle4", "idlehappy" , "idlesad", "idleangry", "idleamazed", "idletired", "idlesuck","clap", "laugh", "cry",
                                                        "walk1","walk2","walkbackwards", "walkstrafeR", "walkstrafeL", "run1", "run2", "runR", "runL", "runstrafeR", "runstrafeL",
                                                         "jump", "runjumpIN", "freefall", "turnR45","turnR90", "turnL45", "turnL90", "fallforwardIN", "fallbackwardsIN" };


            if (set == 1) animations = new string[15] {"sitdownidle1","sitdownidle2","sitdownidle3","sitdownidle4", "sitdownidlehappy","sitdownidlesad","sitdownidleangry",
                                                       "sitdownidleamazed","sitdownidletired","sitdownidlesuck","sitdownclap","sitdownlaugh","sitdowncry", "sitdownfallforward","sitdownfallbackwards" };


            if (set == 2) animations = new string[10] { "crawlidle1","crawlidle2", "crawl","crawlrun","crawlbackwards", "crawlturnR45", "crawlturnR90", "crawlturnL45", "crawlturnL90",
                                                        "crawlfall" };
                       
            if (set == 3) animations = new string[9] { "lieidle1", "lieidle2", "lieidle3", "lieidle4", "lieidletired", "lieidlesuck", "lieclap", "lielaugh", "liecry" };

            if (set == 4) animations = new string[8] { "standupcrawl", "crawlsitdown", "sitdowncrawl", "crawllie", "liecrawl", "crawlstandup", "standupsitdown", "sitdownstandup" };
            animN = 0;
            changecharacter();
            changeanimation();
            changecamera();
        }

        void changecharacter()
        {
            Destroy(RandomBaby);
            Destroy(RandomBaby2);

            RandomBaby = Instantiate(Baby[0]);
            RandomBaby.transform.position += transform.right * 0.2f;
            RandomBaby2 = Instantiate(Baby[1]);
            RandomBaby2.transform.position -= transform.right * 0.2f;

            RandomBaby.GetComponent<TBabyPrefabMaker>().Getready();
            RandomBaby.GetComponent<TBabyPrefabMaker>().Randomize();
            RandomBaby2.GetComponent<TBabyPrefabMaker>().Getready();
            RandomBaby2.GetComponent<TBabyPrefabMaker>().Randomize();

            RandomBaby.GetComponent<Animator>().applyRootMotion = rootON;
            RandomBaby.GetComponent<Playanimation>().enabled = false;
            RandomBaby.GetComponent<Animator>().Play("TB_" + animations[animN]);
            RandomBaby2.GetComponent<Animator>().applyRootMotion = rootON;
            RandomBaby2.GetComponent<Playanimation>().enabled = false;
            RandomBaby2.GetComponent<Animator>().Play("TB_" + animations[animN]);

            turncharacter();
        }

        void turncharacter()
        {
            RandomBaby.transform.rotation = Quaternion.Euler(new Vector3(0f, Cturn, 0f));
            RandomBaby2.transform.rotation = Quaternion.Euler(new Vector3(0f, Cturn, 0f));
        }

        void activeroot()
        {
            rootON = !rootON;
            RandomBaby.GetComponent<Animator>().applyRootMotion = rootON;
            RandomBaby2.GetComponent<Animator>().applyRootMotion = rootON;

        }

        void changecamera()
        {
            if (set != 3)
            {
                Camera.main.transform.position = Cam1transPos;
                Camera.main.transform.rotation = Cam1transRot;
            }
            else
            {
                Camera.main.transform.position = Cam2transPos;
                Camera.main.transform.rotation = Cam2transRot;
            }
        }
        void OnGUI()
        {
            if (showUI)
            {
                GUI.Label(new Rect(1300, 40, 300, 300), texts[0]);
                GUI.Label(new Rect(320, -100, 256, 256), animations[animN], newGUIStyle);

                if (set == 0) GUI.Label(new Rect(300, 40, 256, 128), texts[1]);
                if (set == 1) GUI.Label(new Rect(300, 40, 256, 128), texts[2]);
                if (set == 2) GUI.Label(new Rect(300, 40, 256, 128), texts[3]);
                if (set == 3) GUI.Label(new Rect(300, 40, 256, 128), texts[4]);
                if (set == 4) GUI.Label(new Rect(300, 40, 256, 128), texts[5]);
                if (set == 5) GUI.Label(new Rect(300, 40, 256, 128), texts[6]);

            }
        }
    }
}